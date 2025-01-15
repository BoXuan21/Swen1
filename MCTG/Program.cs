using System;
using System.IO;
using Npgsql;

namespace MCTG
{
    class Program
    {
        public static void Main(string[] args)
        {
            string connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;Include Error Detail=true;";
            string jwtSecretKey = "your-secret-key-at-least-16-chars";

            try
            {
                // Resolve SQL folder path
                string projectRootPath = AppDomain.CurrentDomain.BaseDirectory;
                string sqlFolderPath = Path.Combine(projectRootPath, "../../../SQL"); // Navigate up to the project root
                Console.WriteLine($"SQL folder path resolved to: {sqlFolderPath}");

                // Initialize database
                var dbInitializer = new DatabaseInitializer(connectionString, sqlFolderPath);
                dbInitializer.InitializeDatabase();
                Console.WriteLine("Database initialized successfully");

                // Create instances of required services
                IUserRepository userRepository = new UserRepository(connectionString);
                IJwtService jwtService = new JwtService(jwtSecretKey);
                ICardRepository cardRepository = new CardRepository(connectionString);
                IPackageRepository packageRepository = new PackageRepository(connectionString, cardRepository);
                IBattleRepository battleRepository = new BattleRepository(connectionString);
                ITradeRepository tradeRepository = new TradeRepository(connectionString, cardRepository);
                IUserStatsRepository userStatsRepository = new UserStatsRepository(connectionString);

                // Start server
                var server = new TcpServer(
                    port: 10001,
                    userRepository: userRepository,
                    jwtService: jwtService,
                    packageRepository: packageRepository,
                    cardRepository: cardRepository,
                    connectionString: connectionString,
                    battleRepository: battleRepository,
                    tradeRepository: tradeRepository,
                    userStatsRepository: userStatsRepository
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
