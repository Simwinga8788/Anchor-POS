-- Resets the 'admin' user password to 'sales123'
-- Run this script in SQL Server Management Studio against the SurfPOS database

UPDATE Users
SET PasswordHash = '$2a$11$N9qo8uLOickgx2ZMRZoMye7FRNpZeS8J3W9H3K6TlNIpAngLdAWKC'
WHERE Username = 'admin';
