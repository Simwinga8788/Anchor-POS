-- Check all users and their status
SELECT Id, Username, Role, IsActive, LastLogin, CreatedAt
FROM Users
ORDER BY Id;

-- If you want to manually set a password for a user:
-- Replace 'sales' with the username and run this:
-- Password will be: sales123

/*
UPDATE Users 
SET PasswordHash = '$2a$11$N9qo8uLOickgx2ZMRZoMye7FRNpZeS8J3W9H3K6TlNIpAngLdAWKC',
    IsActive = 1
WHERE Username = 'sales';
*/

-- This hash is for password: sales123
-- You can use this to quickly reset a user's password
