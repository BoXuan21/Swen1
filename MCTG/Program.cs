using System;
using Npgsql;

namespace MCTG
{
    class Program
    {
        public static void Main(string[] args)
        {
            string connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
    
            try
            {
                // Initialize database
                var dbInitializer = new DatabaseInitializer(connectionString);
                dbInitializer.InitializeDatabase();
                Console.WriteLine("Database initialized successfully");

                // Start server
                IUserRepository userRepository = new UserRepository();
                var server = new TcpServer(10001, userRepository);
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}