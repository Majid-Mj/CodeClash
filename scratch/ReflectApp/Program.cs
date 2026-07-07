using System;
using Microsoft.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connString = "Server=codeclash.cb4800ok48qs.eu-north-1.rds.amazonaws.com,1433;Database=codeclash;User ID=admin;Password=adminroot;Encrypt=True;TrustServerCertificate=True;";
        using (var conn = new SqlConnection(connString))
        {
            try
            {
                conn.Open();
                var userCmd = conn.CreateCommand();
                userCmd.CommandText = "SELECT TOP 1 Id FROM Users";
                var userId = userCmd.ExecuteScalar()?.ToString() ?? Guid.NewGuid().ToString();

                var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = @"
                    IF NOT EXISTS (SELECT 1 FROM Problems WHERE Slug = 'palindrome-number')
                    BEGIN
                        DECLARE @ProbId UNIQUEIDENTIFIER = NEWID();
                        INSERT INTO Problems (Id, Title, Slug, Difficulty, Category, StatementMarkdown, ConstraintsJson, AllowedLanguagesJson, TimeLimitMs, MemoryLimitMb, IsActive, CreatedByUserId, CreatedAt, UpdatedAt)
                        VALUES (@ProbId, 'Palindrome Number', 'palindrome-number', 'Easy', 'Algorithms', 'Given an integer x, return true if x is a palindrome, and false otherwise.', '[]', '[]', 2000, 256, 1, @UserId, GETUTCDATE(), GETUTCDATE());

                        INSERT INTO TestCases (Id, ProblemId, Input, ExpectedOutput, IsHidden, OrderIndex)
                        VALUES 
                        (NEWID(), @ProbId, '121', 'true', 0, 0),
                        (NEWID(), @ProbId, '-121', 'false', 0, 1),
                        (NEWID(), @ProbId, '10', 'false', 0, 2);
                        
                        SELECT @ProbId AS NewProbId;
                    END
                    ELSE
                    BEGIN
                        SELECT Id AS NewProbId FROM Problems WHERE Slug = 'palindrome-number';
                    END
                ";
                insertCmd.Parameters.AddWithValue("@UserId", userId);
                var newId = insertCmd.ExecuteScalar();
                Console.WriteLine($"Palindrome Problem ID: {newId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}
