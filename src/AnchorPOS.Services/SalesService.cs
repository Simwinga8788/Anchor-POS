using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Entities;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    public class SalesService : ISalesService
    {
        private readonly SurfDbContext _context;
        private readonly IEmailService _emailService;

        public SalesService(SurfDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Transaction> ProcessSaleAsync(int userId, List<(int ProductId, int Quantity)> items, PaymentMethod paymentMethod, int? shiftId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var lowStockProducts = new List<Product>();

            try
            {
                // Create transaction
                var sale = new Transaction
                {
                    TransactionRef = GenerateTransactionRef(),
                    Date = DateTime.Now,
                    UserId = userId,
                    PaymentMethod = paymentMethod,
                    CreatedAt = DateTime.Now,
                    ShiftId = shiftId
                };

                decimal totalAmount = 0;

                // Process each item
                foreach (var item in items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                        throw new Exception($"Product with ID {item.ProductId} not found");

                    if (product.StockQuantity < item.Quantity)
                        throw new Exception($"Insufficient stock for {product.Name}. Available: {product.StockQuantity}");

                    // Check if stock crosses threshold (only alert if it was above or equal and now going below or equal)
                    // Or simplified: Alert if resulting stock is low.
                    // Let's use the 'Crossing' logic to avoid spam: Alert if Old > Threshold AND New <= Threshold
                    // But also alert if it hits 0.
                    bool wasAbove = product.StockQuantity > product.LowStockThreshold;
                    
                    // Create transaction item
                    var transactionItem = new TransactionItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price,
                        CreatedAt = DateTime.Now
                    };

                    sale.Items.Add(transactionItem);
                    totalAmount += product.Price * item.Quantity;

                    // Deduct stock
                    product.StockQuantity -= item.Quantity;
                    product.UpdatedAt = DateTime.Now;

                    bool isNowLow = product.StockQuantity <= product.LowStockThreshold;

                    // Add to alert list if stock is now at or below threshold
                    // This ensures we don't miss alerts even if it was already low
                    if (product.StockQuantity <= product.LowStockThreshold)
                    {
                        if (!lowStockProducts.Contains(product))
                        {
                            lowStockProducts.Add(product);
                        }
                    }

                    // Log stock change
                    var stockLog = new StockLog
                    {
                        ProductId = item.ProductId,
                        ChangeAmount = -item.Quantity,
                        NewQuantity = product.StockQuantity,
                        Reason = "Sale",
                        UserId = userId,
                        CreatedAt = DateTime.Now
                    };

                    _context.StockLogs.Add(stockLog);
                }

                sale.TotalAmount = totalAmount;
                _context.Transactions.Add(sale);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Send low stock emails (Fire and forget-ish, but awaited safely)
                if (lowStockProducts.Any())
                {
                    await SendLowStockAlertsAsync(lowStockProducts);
                }

                return sale;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task SendLowStockAlertsAsync(List<Product> products)
        {
            try
            {
                // Get Admin Email directly from settings
                var adminEmail = await _context.AppSettings
                    .Where(s => s.Key == "AdminEmail")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(adminEmail)) return;

                foreach (var product in products)
                {
                    string subject = $"Low Stock Alert: {product.Name}";
                    string body = $"Low Stock Warning!\n\n" +
                                 $"Product: {product.Name}\n" +
                                 $"Current Stock: {product.StockQuantity}\n" +
                                 $"Threshold: {product.LowStockThreshold}\n\n" +
                                 $"Please restock this item soon.\n" +
                                 $"Kenji's Beauty Space POS";

                    // We use the EmailService which handles Smtp retrieval internally
                    await _emailService.SendEmailAsync(adminEmail, subject, body);
                }
            }
            catch
            {
                // Suppress email errors to not affect the sale process
            }
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByDateAsync(DateTime date)
        {
            // Ensure we compare the whole day range to avoid SQLite .Date translation issues
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddSeconds(-1);

            return await _context.Transactions
                .Include(t => t.User)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Where(t => t.Date >= startOfDay)
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByUserAsync(int userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Transactions
                .Include(t => t.User)
                .Include(t => t.Items)
                    .ThenInclude(i => i.Product)
                .Where(t => t.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(t => t.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.Date <= endDate.Value);

            return await query
                .OrderByDescending(t => t.Date)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalSalesAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Transactions
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .SumAsync(t => t.TotalAmount);
        }

        private string GenerateTransactionRef()
        {
            // Format: TXN-YYYYMMDD-XXX
            var today = DateTime.Now;
            var todayCount = _context.Transactions
                .Count(t => t.Date.Date == today.Date);

            return $"TXN-{today:yyyyMMdd}-{(todayCount + 1):D3}";
        }
    }
}
