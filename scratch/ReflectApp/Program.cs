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
                var selectCmd = conn.CreateCommand();
                selectCmd.CommandText = "SELECT TOP 3 Id, Status, CreatedAt, RuntimeOutput, CompileOutput FROM Submissions ORDER BY CreatedAt DESC";
                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine("-----------------------------");
                        Console.WriteLine($"Submission ID: {reader["Id"]}");
                        Console.WriteLine($"Status: {reader["Status"]}");
                        Console.WriteLine($"CreatedAt: {reader["CreatedAt"]}");
                        Console.WriteLine($"CompileOutput:\n{reader["CompileOutput"]}");
                        Console.WriteLine($"RuntimeOutput:\n{reader["RuntimeOutput"]}");
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
