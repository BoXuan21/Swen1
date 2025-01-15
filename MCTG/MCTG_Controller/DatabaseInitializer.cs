using System;
using System.IO;
using Npgsql;

namespace MCTG
{
    public class DatabaseInitializer
    {
        private readonly string _connectionString;
        private readonly string _sqlFolderPath;

        public DatabaseInitializer(string connectionString, string sqlFolderPath)
        {
            _connectionString = connectionString;
            _sqlFolderPath = sqlFolderPath;
        }

        public void InitializeDatabase()
        {
            string dropSqlPath = Path.Combine(_sqlFolderPath, "drop.sql");
            string initSqlPath = Path.Combine(_sqlFolderPath, "init.sql");

            Console.WriteLine($"Looking for drop.sql at: {dropSqlPath}");
            Console.WriteLine($"Looking for init.sql at: {initSqlPath}");

            if (!File.Exists(dropSqlPath))
            {
                Console.WriteLine($"Error: drop.sql not found at {dropSqlPath}");
                return;
            }

            if (!File.Exists(initSqlPath))
            {
                Console.WriteLine($"Error: init.sql not found at {initSqlPath}");
                return;
            }

            try
            {
                string dropSql = File.ReadAllText(dropSqlPath);
                string initSql = File.ReadAllText(initSqlPath);

                using var connection = new NpgsqlConnection(_connectionString);
                connection.Open();

                using (var command = new NpgsqlCommand(dropSql, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Executed drop.sql script.");
                }

                using (var command = new NpgsqlCommand(initSql, connection))
                {
                    command.ExecuteNonQuery();
                    Console.WriteLine("Executed init.sql script.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing database: {ex.Message}");
            }
        }
    }
}
