-- ============================================================
-- Apply Cloud Sync Fields migration to existing SurfPOS DB
-- Safe to run multiple times (uses IF NOT EXISTS checks)
-- ============================================================

USE SurfPOS;
GO

-- ── Add IsSynced / SyncedAt to every table ─────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsSynced')
    ALTER TABLE Users ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'SyncedAt')
    ALTER TABLE Users ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'IsSynced')
    ALTER TABLE Transactions ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Transactions') AND name = 'SyncedAt')
    ALTER TABLE Transactions ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TransactionItems') AND name = 'IsSynced')
    ALTER TABLE TransactionItems ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('TransactionItems') AND name = 'SyncedAt')
    ALTER TABLE TransactionItems ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockLogs') AND name = 'IsSynced')
    ALTER TABLE StockLogs ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('StockLogs') AND name = 'SyncedAt')
    ALTER TABLE StockLogs ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shifts') AND name = 'IsSynced')
    ALTER TABLE Shifts ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Shifts') AND name = 'SyncedAt')
    ALTER TABLE Shifts ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'IsSynced')
    ALTER TABLE Products ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Products') AND name = 'SyncedAt')
    ALTER TABLE Products ADD SyncedAt DATETIME2 NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AppSettings') AND name = 'IsSynced')
    ALTER TABLE AppSettings ADD IsSynced BIT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AppSettings') AND name = 'SyncedAt')
    ALTER TABLE AppSettings ADD SyncedAt DATETIME2 NULL;
GO

-- ── Register all three migrations as applied (idempotent) ───
IF OBJECT_ID('__EFMigrationsHistory') IS NULL
    CREATE TABLE __EFMigrationsHistory (MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY, ProductVersion NVARCHAR(32) NOT NULL);

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251225140058_InitialCreate')
    INSERT INTO __EFMigrationsHistory VALUES ('20251225140058_InitialCreate', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251228130751_AddShiftTracking')
    INSERT INTO __EFMigrationsHistory VALUES ('20251228130751_AddShiftTracking', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260101121616_AddAuditLogs')
    INSERT INTO __EFMigrationsHistory VALUES ('20260101121616_AddAuditLogs', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20260323113028_AddCloudSyncFields')
    INSERT INTO __EFMigrationsHistory VALUES ('20260323113028_AddCloudSyncFields', '8.0.0');
GO

-- ── Seed Cloud Sync app settings (idempotent) ───────────────
IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'StoreId')
    INSERT INTO AppSettings ([Key], [Value], CreatedAt, IsSynced)
    VALUES ('StoreId', CAST(NEWID() AS NVARCHAR(100)), GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'ApiKey')
    INSERT INTO AppSettings ([Key], [Value], CreatedAt, IsSynced)
    VALUES ('ApiKey', '', GETDATE(), 0);

IF NOT EXISTS (SELECT 1 FROM AppSettings WHERE [Key] = 'CloudApiUrl')
    INSERT INTO AppSettings ([Key], [Value], CreatedAt, IsSynced)
    VALUES ('CloudApiUrl', 'https://api.anchorpos.app', GETDATE(), 0);
GO

PRINT 'Migration applied successfully.';
