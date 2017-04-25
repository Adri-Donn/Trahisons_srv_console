using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trahisons_srv.Class
{
    class User
    {
        public string Name { get; set; } = "UnamedPlayer#:?:#" + DateTime.UtcNow;
        public string Ip { get; set; }
        public string CurrentRoom { get; set; } = "WaitingRoom";
        public int Money { get; set; } = 2;
        public bool Online { get; set; } = true;

        public List<Card> Hand = new List<Card>();
        
        public bool isAlive()
        {
            return (Hand.Count() > 0);
        }

        public void SetName(string name)
        {
            Name = name + "#:?:#" + DateTime.UtcNow;
        }

        public List<Card> GetRandomCards(int numberOfCards)
        {
            int random = 0;
            List<Card> tirage = new List<Card>();

            for (int i = 1; i <= numberOfCards; i++)
            {
                random = new Random().Next(0, Hand.Count - 1);
                tirage.Add(Hand.ElementAt(random));
            }

            return tirage;
        }

        public Card KillOneCardAndGet(enumTypes cardType)
        {
            Card card = Hand.FirstOrDefault(h => h.type == cardType);

            Hand.Remove(card);

            return card;
        }

        public void Kill()
        {
            Hand.Clear();
        }

        public void reset()
        {
            Money = 2;

            Hand.Clear();
        }

        public int StealMoney(int amount)
        {
            int moneyStoled = 0;
            if(Money >= amount)
            {
                moneyStoled = amount;
                Money = Money - amount;
            }
            else
            {
                moneyStoled = amount - Money;
                Money = 0;
            }

            return moneyStoled;
        }

        public bool HasCard(enumTypes typeCard)
        {
            bool hasCard = false;

            foreach(Card card in Hand)
            {
                if(card.type == typeCard)
                {
                    hasCard = true;
                }
            }

            return hasCard;
        }
    }
}
