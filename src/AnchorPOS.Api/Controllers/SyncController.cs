using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AnchorPOS.Api.Data;
using AnchorPOS.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnchorPOS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SyncController : ControllerBase
    {
        private readonly CloudDbContext _db;

        public SyncController(CloudDbContext db)
        {
            _db = db;
        }

        // Validate API Key and StoreId
        private bool IsAuthorized(string requestStoreId)
        {
            var headerStoreId = Request.Headers["X-Store-Id"].ToString();
            var headerApiKey = Request.Headers["X-Api-Key"].ToString();

            // In production, validate these against a Tenants table
            // For now, simple match
            if (string.IsNullOrEmpty(headerStoreId) || headerStoreId != requestStoreId)
                return false;

            return true;
        }

        [HttpPost("sales")]
        public async Task<IActionResult> SyncSales([FromBody] List<TransactionDto> payloads)
        {
            if (payloads == null || payloads.Count == 0) return Ok();

            // Check auth on first item
            if (!IsAuthorized(payloads[0].StoreId)) return Unauthorized();

            foreach (var dto in payloads)
            {
                // Check if already synced
                var exists = await _db.Transactions
                    .AnyAsync(t => t.StoreId == dto.StoreId && t.LocalId == dto.LocalId);

                if (exists) continue;

                var cloudTx = new CloudTransaction
                {
                    StoreId = dto.StoreId,
                    LocalId = dto.LocalId,
                    TransactionRef = dto.TransactionRef ?? "",
                    Date = dto.Date,
                    TotalAmount = dto.TotalAmount,
                    PaymentMethod = dto.PaymentMethod ?? "",
                    CashierName = dto.CashierName ?? "",
                    LocalShiftId = dto.ShiftId,
                    Items = dto.Items.Select(i => new CloudTransactionItem
                    {
                        StoreId = dto.StoreId,
                        LocalId = 0, // Transaction items use DB auto gen
                        LocalProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.LineTotal
                    }).ToList()
                };

                _db.Transactions.Add(cloudTx);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("shifts")]
        public async Task<IActionResult> SyncShifts([FromBody] List<ShiftDto> payloads)
        {
            if (payloads == null || payloads.Count == 0) return Ok();
            if (!IsAuthorized(payloads[0].StoreId)) return Unauthorized();

            foreach (var dto in payloads)
            {
                var exists = await _db.Shifts
                    .AnyAsync(s => s.StoreId == dto.StoreId && s.LocalId == dto.LocalId);

                if (exists) continue;

                var cloudShift = new CloudShift
                {
                    StoreId = dto.StoreId,
                    LocalId = dto.LocalId,
                    CashierName = dto.CashierName ?? "",
                    StartTime = dto.StartTime,
                    EndTime = dto.EndTime,
                    TotalSales = dto.TotalSales,
                    TransactionCount = dto.TransactionCount,
                    CashStart = dto.CashStart,
                    CashEnd = dto.CashEnd
                };

                _db.Shifts.Add(cloudShift);
            }

            await _db.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("products")]
        public async Task<IActionResult> SyncProducts([FromBody] List<ProductDto> payloads)
        {
            if (payloads == null || payloads.Count == 0) return Ok();
            if (!IsAuthorized(payloads[0].StoreId)) return Unauthorized();

            foreach (var dto in payloads)
            {
                var existing = await _db.Products
                    .FirstOrDefaultAsync(p => p.StoreId == dto.StoreId && p.LocalId == dto.LocalId);

                if (existing != null)
                {
                    // Update existing
                    existing.Name = dto.Name;
                    existing.Barcode = dto.Barcode;
                    existing.Price = dto.Price;
                    existing.CostPrice = dto.CostPrice;
                    existing.StockQuantity = dto.StockQuantity;
                    existing.Category = dto.Category;
                    existing.IsActive = dto.IsActive;
                    _db.Products.Update(existing);
                }
                else
                {
                    // Insert new
                    var cloudProduct = new CloudProduct
                    {
                        StoreId = dto.StoreId,
                        LocalId = dto.LocalId,
                        Name = dto.Name,
                        Barcode = dto.Barcode,
                        Price = dto.Price,
                        CostPrice = dto.CostPrice,
                        StockQuantity = dto.StockQuantity,
                        Category = dto.Category,
                        IsActive = dto.IsActive
                    };
                    _db.Products.Add(cloudProduct);
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
    }

    // DTOs that match JSON payload structure from local POS SyncService
    public class TransactionDto
    {
        public string StoreId { get; set; }
        public int LocalId { get; set; }
        public string TransactionRef { get; set; }
        public DateTime Date { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string CashierName { get; set; }
        public int? ShiftId { get; set; }
        public List<TransactionItemDto> Items { get; set; } = new List<TransactionItemDto>();
    }

    public class TransactionItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class ShiftDto
    {
        public string StoreId { get; set; }
        public int LocalId { get; set; }
        public string CashierName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public decimal TotalSales { get; set; }
        public int TransactionCount { get; set; }
        public decimal CashStart { get; set; }
        public decimal CashEnd { get; set; }
    }

    public class ProductDto
    {
        public string StoreId { get; set; }
        public int LocalId { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public int StockQuantity { get; set; }
        public string Category { get; set; }
        public bool IsActive { get; set; }
    }
}
