﻿namespace MCTG;

public class BattleLogic
{
    public class BattleLog
    {
        public List<string> Rounds { get; set; } = new List<string>();
        public string Winner { get; set; }
        public int FinalScore1 { get; set; }
        public int FinalScore2 { get; set; }
        public int Player1EloChange { get; set; }
        public int Player2EloChange { get; set; }
        public List<Card> CardsExchanged { get; set; } = new List<Card>();
        public DateTime BattleTime { get; set; } = DateTime.UtcNow;
    }

    private readonly User _user1;
    private readonly User _user2;
    private readonly Deck _deck1;
    private readonly Deck _deck2;

    public BattleLogic(User user1, User user2, Deck deck1, Deck deck2)
    {
        _user1 = user1;
        _user2 = user2;
        _deck1 = deck1;
        _deck2 = deck2;
    }

    public BattleLog ExecuteBattle()
    {
        var battleLog = new BattleLog();
        int user1Wins = 0;
        int user2Wins = 0;
        int maxRounds = 100;
        int currentRound = 0;

        while (_deck1.GetCurrentCardCount() > 0 && _deck2.GetCurrentCardCount() > 0 && currentRound < maxRounds)
        {
            currentRound++;
            var roundLog = $"Round {currentRound}:";

            Card card1 = _deck1.GetTopCard();
            Card card2 = _deck2.GetTopCard();

            roundLog += $"\nUser 1 plays: {card1.Name} ({card1.CardType}, {card1.Element}, Damage: {card1.Damage})";
            roundLog += $"\nUser 2 plays: {card2.Name} ({card2.CardType}, {card2.Element}, Damage: {card2.Damage})";

            double damage1 = CalculateEffectiveDamage(card1, card2);
            double damage2 = CalculateEffectiveDamage(card2, card1);

            roundLog += $"\nEffective damage - User 1: {damage1}, User 2: {damage2}";

            if (damage1 > damage2)
            {
                roundLog += "\nUser 1 wins round!";
                _deck2.RemoveTopCard();
                _deck1.AddCard(card2);
                user1Wins++;
            }
            else if (damage2 > damage1)
            {
                roundLog += "\nUser 2 wins round!";
                _deck1.RemoveTopCard();
                _deck2.AddCard(card1);
                user2Wins++;
            }
            else
            {
                roundLog += "\nDraw - no cards exchanged";
            }

            battleLog.Rounds.Add(roundLog);
            Console.WriteLine(roundLog);
        }

        UpdateScoresAndElo(battleLog, user1Wins, user2Wins);
        return battleLog;
    }

    private void UpdateScoresAndElo(BattleLog battleLog, int user1Wins, int user2Wins)
    {
        if (_deck1.GetCurrentCardCount() == 0)
        {
            _user1.Elo -= 5;  // Lost
            _user2.Elo += 3;  // Won
            battleLog.Winner = "User 2";
        }
        else if (_deck2.GetCurrentCardCount() == 0)
        {
            _user1.Elo += 3;  // Won
            _user2.Elo -= 5;  // Lost
            battleLog.Winner = "User 1";
        }

        battleLog.FinalScore1 = user1Wins;
        battleLog.FinalScore2 = user2Wins;
    }

    private double CalculateEffectiveDamage(Card attackingCard, Card defendingCard)
    {
        // Special rules
        if (attackingCard.Name == "Goblin" && defendingCard.Name == "Dragon")
            return 0; // Goblins are too afraid of Dragons

        if (attackingCard.Name == "Ork" && defendingCard.Name == "Wizard")
            return 0; // Wizards control Orks

        if (defendingCard.Name == "Knight" && attackingCard.Element == ElementType.Water)
            return double.MaxValue; // Knights drown instantly against WaterSpells

        if (defendingCard.Name == "Kraken" && attackingCard.CardType == "Spell")
            return 0; // Kraken is immune against spells

        if (attackingCard.Name == "Dragon" && defendingCard.Name == "FireElves")
            return 0; // FireElves evade Dragon attacks

        double baseDamage = attackingCard.Damage;

        // Element effectiveness (only apply if at least one spell card is involved)
        if (attackingCard.CardType == "Spell" || defendingCard.CardType == "Spell")
        {
            if (attackingCard.Element == ElementType.Water && defendingCard.Element == ElementType.Fire)
                baseDamage *= 2.0;
            else if (attackingCard.Element == ElementType.Fire && defendingCard.Element == ElementType.Normal)
                baseDamage *= 2.0;
            else if (attackingCard.Element == ElementType.Normal && defendingCard.Element == ElementType.Water)
                baseDamage *= 2.0;
            else if (attackingCard.Element == ElementType.Fire && defendingCard.Element == ElementType.Water)
                baseDamage *= 0.5;
            else if (attackingCard.Element == ElementType.Normal && defendingCard.Element == ElementType.Fire)
                baseDamage *= 0.5;
            else if (attackingCard.Element == ElementType.Water && defendingCard.Element == ElementType.Normal)
                baseDamage *= 0.5;
        }

        return baseDamage;
    }
}