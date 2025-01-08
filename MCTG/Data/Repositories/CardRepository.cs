using Npgsql;
using Dapper;
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
            Console.WriteLine($"Adding card: Name={card.Name}, UserId={userId}, CardType={card.CardType}, Damage={card.Damage}");
    
            var sql = @"
        INSERT INTO cards (name, damage, element_type, card_type, user_id) 
        VALUES (@Name, @Damage, @Element, @CardType, @userId) 
        RETURNING id";
    
            try 
            {
                var parameters = new
                {
                    card.Name,
                    card.Damage,
                    Element = card.ElementType.ToString(),
                    card.CardType,
                    userId
                };
        
                var id = connection.QuerySingle<int>(sql, parameters);
                Console.WriteLine($"Card added successfully with ID: {id}, CardType: {card.CardType}");
                return id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding card: {ex.Message}");
                throw;
            }
        }

        
        
        public IEnumerable<Card> GetUserCards(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return connection.Query<Card>(@"
                SELECT * FROM cards 
                WHERE user_id = @userId",
                new { userId });
        }

        public void AddPackage(int userId)
        {
            // Generate 5 random cards
            var random = new Random();
            for (int i = 0; i < 5; i++)
            {
                var card = GenerateRandomCard();
                AddCard(card, userId);
            }
        }

        public Card GetCard(int cardId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            var sql = @"
        SELECT 
            id Id,
            name Name,
            damage Damage,
            element_type Element,
            card_type CardType,
            user_id UserId
        FROM cards 
        WHERE id = @cardId";

            var card = connection.QuerySingleOrDefault<Card>(sql, new { cardId });
            Console.WriteLine($"Retrieved card from DB: Id={card?.Id}, Type={card?.CardType}, Element={card?.ElementType}");
            return card;
        }
        

        public IEnumerable<Card> GetUserDeck(int userId)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            return connection.Query<Card>(@"
                SELECT * FROM cards 
                WHERE user_id = @userId AND in_deck = true",
                new { userId });
        }

        public void UpdateDeck(int userId, List<int> cardIds)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            // First reset all cards to not in deck
            connection.Execute(@"
                UPDATE cards 
                SET in_deck = false 
                WHERE user_id = @userId",
                new { userId });

            // Then set selected cards to in deck
            if (cardIds.Any())
            {
                connection.Execute(@"
                    UPDATE cards 
                    SET in_deck = true 
                    WHERE id = ANY(@cardIds) AND user_id = @userId",
                    new { cardIds, userId });
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