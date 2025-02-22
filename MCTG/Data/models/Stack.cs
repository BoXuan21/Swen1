﻿namespace MCTG
{
    public class Stack
    {
        public List<Card> Cards { get; set; }

        public Stack()
        {
            Cards = new List<Card>();
        }

        public void AddRandomCards(int count)
        {
            Random rand = new Random();
            ElementType[] elements = { ElementType.Fire, ElementType.Water, ElementType.Normal };
            string[] monsterTypes = { "Goblin", "Wizard", "Dragons", "Orcs", "Kraken", "Knights" };
            string[] spellTypes = { "Fireball", "Heal", "Freeze", "Lightning" };

            for (int i = 0; i < count; i++)
            {
                ElementType elementType = elements[rand.Next(elements.Length)];
                int damage = rand.Next(10, 50);

                if (rand.Next(2) == 0)
                {
                    string monsterName = "Monster" + rand.Next(1, 15);
                    string monsterType = monsterTypes[rand.Next(monsterTypes.Length)];
                    Cards.Add(new MonsterCard(monsterName, damage, elementType, monsterType));
                }
                else
                {
                    string spellName = "Spell" + rand.Next(1, 15);
                    string spellType = spellTypes[rand.Next(spellTypes.Length)];
                    Cards.Add(new SpellCard(spellName, damage, elementType, spellType));
                }
            }
        }
    }
}