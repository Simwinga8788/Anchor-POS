-- Add Salesperson User to Surf POS Database (SQLite)
-- Username: sales
-- Password: sales123
-- Role: Salesperson (1)

INSERT INTO Users (Username, PasswordHash, Role, IsActive, CreatedAt)
VALUES (
    'sales',
    '$2a$11$N9qo8uLOickgx2ZMRZoMye7FRNpZeS8J3W9H3K6TlNIpAngLdAWKC',
    1,
    1,
    datetime('now')
);

-- Role values: 0 = Admin, 1 = Salesperson
-- IsActive: 1 = Active, 0 = Inactive

-- HOW TO EXECUTE:
-- Option 1: Using DB Browser for SQLite (Recommended)
--   1. Download DB Browser for SQLite (free): https://sqlitebrowser.org/
--   2. Open surfpos.db file (in src\SurfPOS.Desktop\bin\Debug\net10.0-windows\)
--   3. Go to "Execute SQL" tab
--   4. Paste the INSERT statement above
--   5. Click "Execute" (F5)
--   6. Click "Write Changes"

-- Option 2: Using command line
--   1. Open PowerShell in the Surf POS folder
--   2. Run: sqlite3 "src\SurfPOS.Desktop\bin\Debug\net10.0-windows\surfpos.db" < add_sales_user.sql

-- After adding the user, restart the app and login with:
-- Username: sales
-- Password: sales123
