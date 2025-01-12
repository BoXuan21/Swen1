using Npgsql;

namespace MCTG
{
    public class CardRepository : ICardRepository
    {
        private readonly string _connectionString;

        public CardRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public int AddCard(Card card, int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Check if card already exists for the user
                using var checkCmd = new NpgsqlCommand(
                    "SELECT id FROM cards WHERE name = @Name AND user_id = @UserId",
                    connection);
                checkCmd.Parameters.AddWithValue("@Name", card.Name);
                checkCmd.Parameters.AddWithValue("@UserId", userId);

                var existingId = checkCmd.ExecuteScalar();
                if (existingId != null)
                {
                    // Card already exists, return existing ID
                    transaction.Commit();
                    return (int)existingId;
                }

                // Insert new card
                using var insertCmd = new NpgsqlCommand(@"
                    INSERT INTO cards (name, damage, element_type, card_type, user_id) 
                    VALUES (@Name, @Damage, @ElementType, @CardType, @UserId) 
                    RETURNING id",
                    connection);

                insertCmd.Parameters.AddWithValue("@Name", card.Name);
                insertCmd.Parameters.AddWithValue("@Damage", card.Damage);
                insertCmd.Parameters.AddWithValue("@ElementType", (int)card.ElementType);
                insertCmd.Parameters.AddWithValue("@CardType", card.CardType);
                insertCmd.Parameters.AddWithValue("@UserId", userId);

                var id = (int)insertCmd.ExecuteScalar();
                Console.WriteLine($"Card added successfully with ID: {id}, CardType: {card.CardType}");
                
                transaction.Commit();
                return id;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error adding card: {ex.Message}");
                throw;
            }
        }

        public IEnumerable<Card> GetUserCards(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand(@"
        SELECT id, name, damage, element_type, card_type, user_id 
        FROM cards
        WHERE user_id = @UserId",
                connection);
    
            cmd.Parameters.AddWithValue("@UserId", userId);
    
            var cards = new List<Card>();
            using var reader = cmd.ExecuteReader();
    
            while (reader.Read())
            {
                cards.Add(new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = (ElementType)Enum.Parse(typeof(ElementType), reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                });
            }

            return cards;
        }

        public Card GetCard(int cardId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand(@"
        SELECT id, name, damage, element_type, card_type, user_id
        FROM cards 
        WHERE id = @CardId",
                connection);
    
            cmd.Parameters.AddWithValue("@CardId", cardId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var card = new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = (ElementType)Enum.Parse(typeof(ElementType), reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                };
                Console.WriteLine($"Retrieved card from DB: Id={card.Id}, Type={card.CardType}, Element={card.ElementType}");
                return card;
            }
            return null;
        }

        public IEnumerable<Card> GetUserDeck(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();

            using var cmd = new NpgsqlCommand(@"
        SELECT id, name, damage, element_type, card_type, user_id
        FROM cards 
        WHERE user_id = @UserId AND in_deck = true",
                connection);
    
            cmd.Parameters.AddWithValue("@UserId", userId);
    
            var cards = new List<Card>();
            using var reader = cmd.ExecuteReader();
    
            while (reader.Read())
            {
                cards.Add(new Card
                {
                    Id = reader.GetInt32(reader.GetOrdinal("id")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Damage = reader.GetInt32(reader.GetOrdinal("damage")),
                    ElementType = (ElementType)Enum.Parse(typeof(ElementType), reader.GetString(reader.GetOrdinal("element_type"))),
                    CardType = reader.GetString(reader.GetOrdinal("card_type")),
                    UserId = reader.GetInt32(reader.GetOrdinal("user_id"))
                });
            }

            return cards;
        }

        public void UpdateDeck(int userId, List<int> cardIds)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                Console.WriteLine($"Updating deck for user {userId} with cards: {string.Join(", ", cardIds)}");

                // First reset all cards to not in deck
                using var resetCmd = new NpgsqlCommand(@"
                    UPDATE cards 
                    SET in_deck = false 
                    WHERE user_id = @UserId",
                    connection);
                resetCmd.Parameters.AddWithValue("@UserId", userId);
                resetCmd.ExecuteNonQuery();

                // Then set selected cards to in deck
                if (cardIds.Any())
                {
                    using var updateCmd = new NpgsqlCommand(@"
                        UPDATE cards 
                        SET in_deck = true 
                        WHERE id = ANY(@CardIds) AND user_id = @UserId",
                        connection);
                    updateCmd.Parameters.AddWithValue("@CardIds", cardIds.ToArray());
                    updateCmd.Parameters.AddWithValue("@UserId", userId);
                    updateCmd.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error updating deck: {ex.Message}");
                throw;
            }
        }

        private Card GenerateRandomCard()
        {
            var random = new Random();
            var elements = new[] { ElementType.Fire, ElementType.Water, ElementType.Normal };
            string[] cardTypes = { "Monster", "Spell" };
            string[] monsterNames = { "Goblin", "Dragon", "Wizard", "Ork", "Knight", "Kraken", "FireElves" };
            string[] spellNames = { "FireSpell", "WaterSpell", "RegularSpell" };

            string cardType = cardTypes[random.Next(cardTypes.Length)];
            ElementType element = elements[random.Next(elements.Length)];
            string name;

            if (cardType == "Monster")
            {
                name = monsterNames[random.Next(monsterNames.Length)];
            }
            else
            {
                name = spellNames[random.Next(spellNames.Length)];
            }

            return new Card(name, random.Next(10, 100), element);
        }
    }
}