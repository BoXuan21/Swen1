using System;
using Npgsql;

namespace MCTG
{
    class Program
    {
        public static void Main(string[] args)
        {
            string connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;";
            string jwtSecretKey = "your-secret-key-at-least-16-chars";
   
            try
            {
                // Initialize database
                var dbInitializer = new DatabaseInitializer(connectionString);
                dbInitializer.InitializeDatabase();
                Console.WriteLine("Database initialized successfully");

                // Create instances of required services
                IUserRepository userRepository = new UserRepository(connectionString);
                ICardRepository cardRepository = new CardRepository(connectionString);
                ITradeRepository tradeRepository = new TradeRepository(connectionString, cardRepository);
                JwtService jwtService = new JwtService(jwtSecretKey);

                // Start server
                var server = new TcpServer(
                    port: 10001,
                    userRepository: userRepository,
                    cardRepository: cardRepository,
                    tradeRepository: tradeRepository,
                    jwtService: jwtService
                );
                server.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}