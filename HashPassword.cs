// Quick Password Hash Generator
// Run this to get the BCrypt hash for any password

using System;

class Program
{
    static void Main()
    {
        string password = "sales@123";
        string hash = BCrypt.Net.BCrypt.HashPassword(password);
        
        Console.WriteLine("Password: " + password);
        Console.WriteLine("BCrypt Hash: " + hash);
        Console.WriteLine();
        Console.WriteLine("Run this SQL to update the sales user:");
        Console.WriteLine($"UPDATE Users SET PasswordHash = '{hash}', UpdatedAt = GETDATE() WHERE Username = 'sales';");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
