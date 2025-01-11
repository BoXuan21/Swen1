namespace MCTG;
using Npgsql;
using Dapper;

public class DatabaseInitializer
{
    private readonly string _connectionString;

    public DatabaseInitializer(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void InitializeDatabase()
    {
        using var connection = new NpgsqlConnection(_connectionString);

        // Drop existing tables
        connection.Execute(@"
            DROP TABLE IF EXISTS trades CASCADE;
            DROP TABLE IF EXISTS battle_history CASCADE;
            DROP TABLE IF EXISTS user_stats CASCADE;
            DROP TABLE IF EXISTS package_cards CASCADE;
            DROP TABLE IF EXISTS cards CASCADE;
            DROP TABLE IF EXISTS packages CASCADE;
            DROP TABLE IF EXISTS users CASCADE;
        ");

        // Create users table
        var createUsersTable = @"
            CREATE TABLE IF NOT EXISTS users (
                id SERIAL PRIMARY KEY,
                username VARCHAR(255) UNIQUE NOT NULL,
                password VARCHAR(255) NOT NULL,
                coins INTEGER NOT NULL DEFAULT 20,
                elo INTEGER NOT NULL DEFAULT 100
            )";

        // Create cards table
        var createCardsTable = @"
    CREATE TABLE IF NOT EXISTS cards (
        id SERIAL PRIMARY KEY,
        name VARCHAR(100) NOT NULL,
        damage INTEGER NOT NULL,
        element_type VARCHAR(50) NOT NULL,
        card_type VARCHAR(50) NOT NULL,
        user_id INTEGER,
        in_deck BOOLEAN DEFAULT false,
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
    )";
        
        // Create battle_history table
        var createBattleHistoryTable = @"
            CREATE TABLE IF NOT EXISTS battle_history (
                id SERIAL PRIMARY KEY,
                player1_id INTEGER REFERENCES users(id),
                player2_id INTEGER REFERENCES users(id),
                winner_id INTEGER REFERENCES users(id),
                battle_log TEXT NOT NULL,
                player1_elo_change INTEGER NOT NULL,
                player2_elo_change INTEGER NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )";
        
        // Create user_stats table
        var createUserStatsTable = @"
            CREATE TABLE IF NOT EXISTS user_stats (
                id SERIAL PRIMARY KEY,
                user_id INTEGER REFERENCES users(id),
                games_played INTEGER DEFAULT 0,
                wins INTEGER DEFAULT 0,
                losses INTEGER DEFAULT 0,
                draws INTEGER DEFAULT 0,
                elo INTEGER DEFAULT 100
            )";
        
        // Create user_profiles table
        var createUserProfilesTable = @"
    CREATE TABLE IF NOT EXISTS user_profiles (
        user_id INTEGER PRIMARY KEY REFERENCES users(id),
        name VARCHAR(255),
        bio TEXT,
        image VARCHAR(255)
    )";

        // Create trades table
        var createTradesTable = @"
            CREATE TABLE IF NOT EXISTS trades (
                id SERIAL PRIMARY KEY,
                card_id INTEGER,
                user_id INTEGER,
                required_type VARCHAR(50) NOT NULL,
                minimum_damage INTEGER NOT NULL,
                FOREIGN KEY (card_id) REFERENCES cards(id) ON DELETE CASCADE,
                FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
            )";
        
        // Create packages table
        var createPackagesTable = @"
            CREATE TABLE IF NOT EXISTS packages (
                id SERIAL PRIMARY KEY,
                is_sold BOOLEAN DEFAULT false,
                bought_by_user_id INTEGER REFERENCES users(id),
                purchase_date TIMESTAMP
            )";

        // Create package_cards table
        var createPackageCardsTable = @"
            CREATE TABLE IF NOT EXISTS package_cards (
                package_id INTEGER REFERENCES packages(id) ON DELETE CASCADE,
                card_id INTEGER REFERENCES cards(id) ON DELETE CASCADE,
                PRIMARY KEY (package_id, card_id)
            )";

        // Execute all CREATE statements in correct order
        connection.Execute(createUsersTable);
        connection.Execute(createPackagesTable);
        connection.Execute(createCardsTable);
        connection.Execute(createPackageCardsTable);
        connection.Execute(createBattleHistoryTable);
        connection.Execute(createUserStatsTable);
        connection.Execute(createTradesTable);
        connection.Execute(createUserProfilesTable);

        // Verify the foreign key constraints
        var constraints = connection.Query(@"
            SELECT
                tc.table_name, 
                kcu.column_name,
                ccu.table_name AS foreign_table_name,
                ccu.column_name AS foreign_column_name
            FROM
                information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                  ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage AS ccu
                  ON ccu.constraint_name = tc.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY'");

        Console.WriteLine("\nForeign Key Constraints:");
        foreach (var constraint in constraints)
        {
            Console.WriteLine($"Table {constraint.table_name}: Column {constraint.column_name} references {constraint.foreign_table_name}({constraint.foreign_column_name})");
        }

        Console.WriteLine("Database initialized successfully");
    }
}