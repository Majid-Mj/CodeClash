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
                selectCmd.CommandText = "SELECT Id, ProblemId, Language, SourceCode, Status, RuntimeOutput, CompileOutput FROM Submissions WHERE Id = '580d964e-da41-42a7-a05b-62d96029093c'";
                using (var reader = selectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        Console.WriteLine("-----------------------------");
                        Console.WriteLine($"Submission ID: {reader["Id"]}");
                        Console.WriteLine($"Language: {reader["Language"]}");
                        Console.WriteLine($"Status: {reader["Status"]}");
                        Console.WriteLine($"SourceCode:\n{reader["SourceCode"]}");
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
