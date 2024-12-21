using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MCTG
{
    public class DatabaseController
    {
        private readonly string _connectionString;

        public DatabaseController()
        {
            _connectionString = "Host=localhost;Port=5432;Database=MCTG;Username=admin;Password=1";
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        // Database initialization
        public async Task InitializeDatabase()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var createTablesSQL = @"
                -- Users table
                CREATE TABLE IF NOT EXISTS users (
                    id SERIAL PRIMARY KEY,
                    username VARCHAR(50) UNIQUE NOT NULL,
                    password VARCHAR(255) NOT NULL,
                    coins INTEGER DEFAULT 20,
                    elo VARCHAR(20) DEFAULT 'Iron'
                );

                -- Cards table
                CREATE TABLE IF NOT EXISTS cards (
                    id SERIAL PRIMARY KEY,
                    name VARCHAR(100) NOT NULL,
                    damage INTEGER NOT NULL,
                    element_type VARCHAR(20) NOT NULL,
                    card_type VARCHAR(20) NOT NULL,
                    specific_type VARCHAR(50) NOT NULL
                );

                -- User_Cards table
                CREATE TABLE IF NOT EXISTS user_cards (
                    id SERIAL PRIMARY KEY,
                    user_id INTEGER REFERENCES users(id),
                    card_id INTEGER REFERENCES cards(id),
                    in_deck BOOLEAN DEFAULT false,
                    UNIQUE(user_id, card_id)
                );

                -- Trades table
                CREATE TABLE IF NOT EXISTS trades (
                    id SERIAL PRIMARY KEY,
                    user_id INTEGER REFERENCES users(id),
                    card_id INTEGER REFERENCES cards(id),
                    desired_card_type VARCHAR(50),
                    minimum_damage INTEGER,
                    status VARCHAR(20) DEFAULT 'ACTIVE',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                -- Battles table
                CREATE TABLE IF NOT EXISTS battles (
                    id SERIAL PRIMARY KEY,
                    user1_id INTEGER REFERENCES users(id),
                    user2_id INTEGER REFERENCES users(id),
                    winner_id INTEGER REFERENCES users(id),
                    played_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";

            using var cmd = new NpgsqlCommand(createTablesSQL, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // User Operations
        public async Task<bool> CreateUser(string username, string password)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            try
            {
                var sql = "INSERT INTO users (username, password) VALUES (@username, @password)";
                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("password", password);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (PostgresException)
            {
                return false;
            }
        }

        public async Task<User> GetUser(string username, string password)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = "SELECT id, username, coins, elo FROM users WHERE username = @username AND password = @password";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("username", username);
            cmd.Parameters.AddWithValue("password", password);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Username = reader.GetString(1),
                    Coin = reader.GetInt32(2),
                    Elo = reader.GetString(3)
                };
            }
            return null;
        }

        // Card Operations
        public async Task<bool> AddCardToUser(int userId, Card card)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // First insert the card
                var cardSql = @"INSERT INTO cards (name, damage, element_type, card_type, specific_type) 
                               VALUES (@name, @damage, @element, @cardType, @specificType) 
                               RETURNING id";
                using var cardCmd = new NpgsqlCommand(cardSql, conn);
                cardCmd.Parameters.AddWithValue("name", card.Name);
                cardCmd.Parameters.AddWithValue("damage", card.Damage);
                cardCmd.Parameters.AddWithValue("element", card.Element.ToString());
                cardCmd.Parameters.AddWithValue("cardType", card is MonsterCard ? "Monster" : "Spell");
                cardCmd.Parameters.AddWithValue("specificType", 
                    card is MonsterCard ? ((MonsterCard)card).MonsterType : ((SpellCard)card).SpellType);

                var cardId = (int)await cardCmd.ExecuteScalarAsync();

                // Then link it to the user
                var userCardSql = "INSERT INTO user_cards (user_id, card_id) VALUES (@userId, @cardId)";
                using var userCardCmd = new NpgsqlCommand(userCardSql, conn);
                userCardCmd.Parameters.AddWithValue("userId", userId);
                userCardCmd.Parameters.AddWithValue("cardId", cardId);
                await userCardCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<List<Card>> GetUserCards(int userId, bool inDeck = false)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT c.* FROM cards c
                       JOIN user_cards uc ON c.id = uc.card_id
                       WHERE uc.user_id = @userId AND uc.in_deck = @inDeck";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("inDeck", inDeck);

            var cards = new List<Card>();
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                Card card;
                var cardType = reader.GetString(4);
                if (cardType == "Monster")
                {
                    card = new MonsterCard(
                        reader.GetString(1),
                        reader.GetInt32(2),
                        Enum.Parse<Card.ElementType>(reader.GetString(3)),
                        reader.GetString(5)
                    );
                }
                else
                {
                    card = new SpellCard(
                        reader.GetString(1),
                        reader.GetInt32(2),
                        Enum.Parse<Card.ElementType>(reader.GetString(3)),
                        reader.GetString(5)
                    );
                }
                cards.Add(card);
            }

            return cards;
        }

        // Coin Operations
        public async Task UpdateUserCoins(int userId, int coins)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE users SET coins = @coins WHERE id = @userId";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("coins", coins);
            cmd.Parameters.AddWithValue("userId", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Deck Operations
        public async Task MoveCardToDeck(int userId, int cardId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE user_cards SET in_deck = true WHERE user_id = @userId AND card_id = @cardId";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("userId", userId);
            cmd.Parameters.AddWithValue("cardId", cardId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Battle History Operations
        public async Task RecordBattle(int user1Id, int user2Id, int winnerId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();

            var sql = @"INSERT INTO battles (user1_id, user2_id, winner_id) 
                       VALUES (@user1Id, @user2Id, @winnerId)";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("user1Id", user1Id);
            cmd.Parameters.AddWithValue("user2Id", user2Id);
            cmd.Parameters.AddWithValue("winnerId", winnerId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}