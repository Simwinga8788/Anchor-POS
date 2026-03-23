using System.Threading;
using System.Threading.Tasks;

namespace SurfPOS.Core.Interfaces
{
    public interface ISyncService
    {
        /// <summary>
        /// Runs one full sync cycle: finds all unsynced local records
        /// and pushes them to the cloud API.
        /// Returns the number of records successfully synced.
        /// </summary>
        Task<int> SyncNowAsync(CancellationToken ct = default);

        /// <summary>
        /// Starts the background loop that calls SyncNowAsync every 60 seconds.
        /// Fire-and-forget — safe to call from App.xaml.cs startup.
        /// </summary>
        void StartBackgroundSync();
    }
}
