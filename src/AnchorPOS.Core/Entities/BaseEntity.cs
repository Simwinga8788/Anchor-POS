using System;
using System.ComponentModel.DataAnnotations;

namespace SurfPOS.Core.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // ── Cloud Sync tracking ───────────────────────────────────────────────
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
    }
}
