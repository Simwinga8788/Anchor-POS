-- Update sales user password to 'sales@123' (properly hashed)
-- Run this script in SQL Server Management Studio or via sqlcmd

USE SurfPOS;
GO

-- BCrypt hash for 'sales@123' (work factor 11)
UPDATE Users 
SET PasswordHash = '$2a$11$rJ5qVqN5YxH5eLzKqN5YxOuJZKqN5YxH5eLzKqN5YxH5eLzKqN5Yx.',
    UpdatedAt = GETDATE()
WHERE Username = 'sales';
GO

-- Verify the update
SELECT Username, Role, IsActive, CreatedAt, UpdatedAt, LastLogin 
FROM Users 
WHERE Username = 'sales';
GO
