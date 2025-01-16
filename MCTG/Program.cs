using System;
using System.IO;
using System.Threading.Tasks;

namespace MCTG
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            string connectionString = "Host=localhost;Database=MCTG;Username=postgres;Password=postgres;Include Error Detail=true;";
            string jwtSecretKey = "your-secret-key-at-least-16-chars";

            try
            {
                // Resolve SQL folder path relative to executable
                string projectRootPath = AppDomain.CurrentDomain.BaseDirectory;
                string sqlFolderPath = Path.GetFullPath(Path.Combine(projectRootPath, "SQL"));
                
                Console.WriteLine($"Looking for SQL scripts in: {sqlFolderPath}");
                
                if (!Directory.Exists(sqlFolderPath))
                {
                    Console.WriteLine("SQL directory not found, trying alternative path...");
                    sqlFolderPath = Path.GetFullPath(Path.Combine(projectRootPath, "../../../SQL"));
                    
                    if (!Directory.Exists(sqlFolderPath))
                    {
                        throw new DirectoryNotFoundException($"Could not find SQL scripts directory at: {sqlFolderPath}");
                    }
                }

                Console.WriteLine($"SQL folder path resolved to: {sqlFolderPath}");

                // Initialize database
                try
                {
                    var dbInitializer = new DatabaseInitializer(connectionString, sqlFolderPath);
                    dbInitializer.InitializeDatabase();
                    Console.WriteLine("Database initialized successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization failed: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    throw;
                }

                // Create repositories
                var userRepository = new UserRepository(connectionString);
                var cardRepository = new CardRepository(connectionString);
                var packageRepository = new PackageRepository(connectionString, cardRepository);
                var battleRepository = new BattleRepository(connectionString);
                var tradeRepository = new TradeRepository(connectionString, cardRepository);
                var userStatsRepository = new UserStatsRepository(connectionString);
                var jwtService = new JwtService(jwtSecretKey);

                Console.WriteLine("All repositories initialized successfully");

                // Test database connection
                try
                {
                    await using var connection = new Npgsql.NpgsqlConnection(connectionString);
                    await connection.OpenAsync();
                    Console.WriteLine("Database connection test successful");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database connection test failed: {ex.Message}");
                    throw;
                }

                // Start server
                Console.WriteLine("Starting server...");
                var server = new TcpServer(
                    port: 10001,
                    userRepository: userRepository,
                    cardRepository: cardRepository,
                    jwtService: jwtService,
                    packageRepository: packageRepository,
                    battleRepository: battleRepository,
                    tradeRepository: tradeRepository,
                    userStatsRepository: userStatsRepository,
                    connectionString: connectionString
                );

                Console.WriteLine("Server initialized, starting to listen...");
                server.Start();

                // Keep the application running
                Console.WriteLine("Press Ctrl+C to stop the server");
                var tcs = new TaskCompletionSource<bool>();
                Console.CancelKeyPress += (s, e) => {
                    e.Cancel = true;
                    tcs.SetResult(true);
                };
                await tcs.Task;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }

                // Give user time to read error messages
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                Environment.Exit(1);
            }
        }
    }
}