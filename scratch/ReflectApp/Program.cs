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
                cmd.CommandText = "SELECT TOP 5 Id, ProblemId, Status, RuntimeOutput, CompileOutput, CreatedAt FROM Submissions ORDER BY CreatedAt DESC";
                
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("--------------------------------------------------");
                        Console.WriteLine($"Id: {reader["Id"]}");
                        Console.WriteLine($"Status: {reader["Status"]}");
                        Console.WriteLine($"RuntimeOutput: {reader["RuntimeOutput"]}");
                        Console.WriteLine($"CompileOutput: {reader["CompileOutput"]}");
                        Console.WriteLine($"CreatedAt: {reader["CreatedAt"]}");
                    }
                }
                var probCmd = conn.CreateCommand();
                probCmd.CommandText = "SELECT Id, Title, Slug FROM Problems WHERE Id = '0e27992c-0b0b-4a80-ac86-e7faa69c940f'";
                using (var probReader = probCmd.ExecuteReader())
                {
                    if (probReader.Read())
                    {
                        Console.WriteLine($"Problem ID: {probReader["Id"]}");
                        Console.WriteLine($"Title: {probReader["Title"]}");
                        Console.WriteLine($"Slug: {probReader["Slug"]}");
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
