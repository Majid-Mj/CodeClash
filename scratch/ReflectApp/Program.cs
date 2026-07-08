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
                var updateCmd = conn.CreateCommand();
                updateCmd.CommandText = "UPDATE TestCases SET ExpectedOutput = '[0,1]' WHERE Id = 'd38d275e-6019-4c24-9900-68f8c0599d65'";
                int rows = updateCmd.ExecuteNonQuery();
                Console.WriteLine($"Updated {rows} row(s) in TestCases table.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}
