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
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT p.Title, p.Slug, tc.Input, tc.ExpectedOutput FROM TestCases tc JOIN Problems p ON tc.ProblemId = p.Id";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"Problem: {reader["Title"]} ({reader["Slug"]}) | Input: {reader["Input"]} | Expected: {reader["ExpectedOutput"]}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}

