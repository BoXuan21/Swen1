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
                IJwtService jwtService = new JwtService(jwtSecretKey);
                ICardRepository cardRepository = new CardRepository(connectionString);
                IPackageRepository packageRepository = new PackageRepository(connectionString, cardRepository);

                // Start server
                var server = new TcpServer(
                    port: 10001,
                    userRepository: userRepository,
                    jwtService: jwtService,
                    packageRepository: packageRepository,
                    cardRepository: cardRepository,
                    connectionString: connectionString
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