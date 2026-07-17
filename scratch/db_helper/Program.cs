using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CodeClash.Infrastructure.Persistence;
using CodeClash.Domain.Entities;

class Program
{
    static async Task Main(string[] args)
    {
        var connectionString = "Server=codeclash.cb4800ok48qs.eu-north-1.rds.amazonaws.com,1433;Database=codeclash;User ID=admin;Password=adminroot;Encrypt=True;TrustServerCertificate=True;";
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var db = new ApplicationDbContext(optionsBuilder.Options);

        Console.WriteLine("Connecting to DB...");

        // Ensure users exist
        var players = new[] { "player1", "player2", "player3", "player4" };
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Player@1234", workFactor: 12);

        foreach (var pName in players)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == pName);
            if (user == null)
            {
                user = User.Create(
                    fullName: pName.ToUpper(),
                    username: pName,
                    email: $"{pName}@codeclash.com",
                    passwordHash: passwordHash
                );
                await db.Users.AddAsync(user);
                Console.WriteLine($"Seeded user: {pName}");
            }
        }
        await db.SaveChangesAsync();

        Console.WriteLine("\nAll Users in DB:");
        var users = await db.Users.ToListAsync();
        foreach (var u in users)
        {
            Console.WriteLine($"- ID: {u.Id}, Username: {u.Username}, Role: {u.Role}, Points: {u.TotalPoints}, Rating: {u.Rating}");
        }
    }
}
