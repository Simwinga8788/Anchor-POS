using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SurfPOS.Core.Interfaces;
using SurfPOS.Data;

namespace SurfPOS.Services
{
    /// <summary>
    /// Background sync service.
    /// Every 60 seconds it finds all records where IsSynced = false,
    /// bundles them into a payload, and POSTs to the cloud API.
    /// If the API is unreachable, it silently retries on the next cycle.
    /// </summary>
    public class SyncService : ISyncService
    {
        // ── Configuration ─────────────────────────────────────────────────────
        private const int SyncIntervalSeconds = 60;
        private const string SyncLogFile = "SyncService_Error.txt";

        private readonly IDbContextFactory<SurfDbContext> _dbFactory;
        private readonly HttpClient _http;
        private string _apiBase = string.Empty;
        private string _storeId  = string.Empty;
        private string _apiKey   = string.Empty;
        private bool   _started  = false;

        public SyncService(IDbContextFactory<SurfDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void StartBackgroundSync()
        {
            if (_started) return;
            _started = true;
            Task.Run(RunLoopAsync);
        }

        public async Task<int> SyncNowAsync(CancellationToken ct = default)
        {
            await LoadSettingsAsync();

            if (string.IsNullOrWhiteSpace(_apiBase) ||
                string.IsNullOrWhiteSpace(_storeId)  ||
                string.IsNullOrWhiteSpace(_apiKey))
            {
                // Not configured yet — skip silently
                return 0;
            }

            int synced = 0;
            synced += await SyncTransactionsAsync(ct);
            synced += await SyncShiftsAsync(ct);
            synced += await SyncProductsAsync(ct);
            return synced;
        }

        // ── Background loop ───────────────────────────────────────────────────

        private async Task RunLoopAsync()
        {
            while (true)
            {
                try
                {
                    await SyncNowAsync();
                }
                catch (Exception ex)
                {
                    LogError(ex);
                }
                await Task.Delay(TimeSpan.FromSeconds(SyncIntervalSeconds));
            }
        }

        // ── Sync each table ───────────────────────────────────────────────────

        private async Task<int> SyncTransactionsAsync(CancellationToken ct)
        {
            using var db = await _dbFactory.CreateDbContextAsync(ct);

            var unsync = await db.Transactions
                .Include(t => t.Items)
                .Include(t => t.User)
                .Where(t => !t.IsSynced)
                .OrderBy(t => t.Date)
                .Take(100)          // max 100 per cycle to avoid large payloads
                .ToListAsync(ct);

            if (unsync.Count == 0) return 0;

            var payload = unsync.Select(t => new
            {
                storeId          = _storeId,
                localId          = t.Id,
                transactionRef   = t.TransactionRef,
                date             = t.Date,
                totalAmount      = t.TotalAmount,
                paymentMethod    = t.PaymentMethod.ToString(),
                cashierName      = t.User?.Username ?? "unknown",
                shiftId          = t.ShiftId,
                items            = t.Items.Select(i => new
                {
                    productId    = i.ProductId,
                    quantity     = i.Quantity,
                    unitPrice    = i.UnitPrice,
                    lineTotal    = i.Quantity * i.UnitPrice
                })
            }).ToList();

            var ok = await PostAsync("api/sync/sales", payload, ct);
            if (!ok) return 0;

            // Mark all as synced
            var ids = unsync.Select(t => t.Id).ToHashSet();
            var toMark = await db.Transactions.Where(t => ids.Contains(t.Id)).ToListAsync(ct);
            foreach (var t in toMark) { t.IsSynced = true; t.SyncedAt = DateTime.UtcNow; }
            await db.SaveChangesAsync(ct);
            return toMark.Count;
        }

        private async Task<int> SyncShiftsAsync(CancellationToken ct)
        {
            using var db = await _dbFactory.CreateDbContextAsync(ct);

            var unsync = await db.Shifts
                .Include(s => s.User)
                .Where(s => !s.IsSynced && !s.IsActive)   // only completed shifts
                .OrderBy(s => s.StartTime)
                .Take(50)
                .ToListAsync(ct);

            if (unsync.Count == 0) return 0;

            var payload = unsync.Select(s => new
            {
                storeId          = _storeId,
                localId          = s.Id,
                cashierName      = s.User?.Username ?? "unknown",
                startTime        = s.StartTime,
                endTime          = s.EndTime,
                totalSales       = s.TotalSales,
                transactionCount = s.TransactionCount,
                cashStart        = s.CashStart,
                cashEnd          = s.CashEnd
            }).ToList();

            var ok = await PostAsync("api/sync/shifts", payload, ct);
            if (!ok) return 0;

            var ids = unsync.Select(s => s.Id).ToHashSet();
            var toMark = await db.Shifts.Where(s => ids.Contains(s.Id)).ToListAsync(ct);
            foreach (var s in toMark) { s.IsSynced = true; s.SyncedAt = DateTime.UtcNow; }
            await db.SaveChangesAsync(ct);
            return toMark.Count;
        }

        private async Task<int> SyncProductsAsync(CancellationToken ct)
        {
            using var db = await _dbFactory.CreateDbContextAsync(ct);

            var unsync = await db.Products
                .Where(p => !p.IsSynced)
                .OrderBy(p => p.UpdatedAt)
                .Take(200)
                .ToListAsync(ct);

            if (unsync.Count == 0) return 0;

            var payload = unsync.Select(p => new
            {
                storeId       = _storeId,
                localId       = p.Id,
                name          = p.Name,
                barcode       = p.Barcode,
                price         = p.Price,
                costPrice     = p.CostPrice,
                stockQuantity = p.StockQuantity,
                category      = p.Category,
                isActive      = p.IsActive
            }).ToList();

            var ok = await PostAsync("api/sync/products", payload, ct);
            if (!ok) return 0;

            var ids = unsync.Select(p => p.Id).ToHashSet();
            var toMark = await db.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);
            foreach (var p in toMark) { p.IsSynced = true; p.SyncedAt = DateTime.UtcNow; }
            await db.SaveChangesAsync(ct);
            return toMark.Count;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private async Task<bool> PostAsync<T>(string path, T payload, CancellationToken ct)
        {
            try
            {
                _http.DefaultRequestHeaders.Clear();
                _http.DefaultRequestHeaders.Add("X-Store-Id",  _storeId);
                _http.DefaultRequestHeaders.Add("X-Api-Key",   _apiKey);

                var url = $"{_apiBase.TrimEnd('/')}/{path}";
                var resp = await _http.PostAsJsonAsync(url, payload, ct);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                using var db = await _dbFactory.CreateDbContextAsync();
                var settings = await db.AppSettings.ToListAsync();

                _apiBase  = settings.FirstOrDefault(s => s.Key == "CloudApiUrl")?.Value  ?? string.Empty;
                _storeId  = settings.FirstOrDefault(s => s.Key == "StoreId")?.Value      ?? string.Empty;
                _apiKey   = settings.FirstOrDefault(s => s.Key == "ApiKey")?.Value       ?? string.Empty;
            }
            catch { /* DB might not be ready yet on first boot */ }
        }

        private static void LogError(Exception ex)
        {
            try
            {
                var log = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    SyncLogFile);
                File.AppendAllText(log,
                    $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.Message}\n{ex.StackTrace}\n");
            }
            catch { /* never let logging crash the app */ }
        }
    }
}
