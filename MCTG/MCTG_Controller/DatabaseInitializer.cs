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
           DROP TABLE IF EXISTS trades;
           DROP TABLE IF EXISTS cards;
           DROP TABLE IF EXISTS users;
       ");
       
       // Create users table
       var createUsersTable = @"
           CREATE TABLE IF NOT EXISTS users (
               id SERIAL PRIMARY KEY,
               username VARCHAR(50) UNIQUE NOT NULL,
               password VARCHAR(50) NOT NULL,
               coins INTEGER DEFAULT 20,
               elo VARCHAR(20) DEFAULT 'Iron'
           )";

       // Create cards table
       var createCardsTable = @"
           CREATE TABLE IF NOT EXISTS cards (
               id SERIAL PRIMARY KEY,
               name VARCHAR(100) NOT NULL,
               damage INTEGER NOT NULL,
               element_type VARCHAR(50) NOT NULL,
               card_type VARCHAR(50) NOT NULL,
               user_id INTEGER REFERENCES users(id),
               in_deck BOOLEAN DEFAULT false
           )";

       // Create trades table
       var createTradesTable = @"
           CREATE TABLE IF NOT EXISTS trades (
               id SERIAL PRIMARY KEY,
               card_id INTEGER REFERENCES cards(id),
               user_id INTEGER REFERENCES users(id),
               required_type VARCHAR(50) NOT NULL,
               minimum_damage INTEGER NOT NULL
           )";

       connection.Execute(createUsersTable);
       connection.Execute(createCardsTable);
       connection.Execute(createTradesTable);

       // Print table structure to verify
       var tableInfo = connection.Query("SELECT column_name, data_type FROM information_schema.columns WHERE table_name = 'users'");
       Console.WriteLine("Users table structure:");
       foreach (var column in tableInfo)
       {
           Console.WriteLine($"Column: {column.column_name}, Type: {column.data_type}");
       }
   }
}