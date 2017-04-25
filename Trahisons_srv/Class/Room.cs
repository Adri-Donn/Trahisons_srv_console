using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Trahisons_srv.Extension;
using System.Threading;

namespace Trahisons_srv.Class
{
    class Room
    {
        public Room (string id)
        {
            this.Id = id;
        }
        public string Id { get; set; }

        public List<User> Players = new List<User>();
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public Deck deck = new Deck();

        public string IpActualPlayer { get; set; }
        public int OrderNumberOfActualPlayer { get; set; } = 0;

        public Timer timerAction { get; set; }
        public string IpChallenger { get; set; }
        public string MustAnswer { get; set; }
        public bool CanInterrupt { get; set; } = false;
        public bool IsInterrogating { get; set; } = false;
        public int OrderNumberOfActualPlayerInterrogating { get; set; } = 0;

        public enumTypes MustAnswerPlayer { get; set; }

        public EnumActionProcessing ActionProcessing { get; set; } = EnumActionProcessing.NONE;
        public enumTypesChallenge TypeChallenge { get; set; } = enumTypesChallenge.NONE;
        
        public int DuchessAlive { get; set; } = 4;
        public int CapitainAlive { get; set; } = 4;
        public int ComptessAlive { get; set; } = 4;
        public int KillerAlive { get; set; } = 4;
        public int AmbassadorAlive { get; set; } = 4;
        public int InquisitorsAlive { get; set; } = 4;

        public string NextPlayer()
        {
            int numberOfPlayers = Players.Count();

            if(OrderNumberOfActualPlayer + 1 >= numberOfPlayers)
            {
                OrderNumberOfActualPlayer += 1;
                IpActualPlayer = Players.ElementAt(OrderNumberOfActualPlayer).Ip;

                if (Players[OrderNumberOfActualPlayer].Online)
                {
                    IpActualPlayer = Players.ElementAt(OrderNumberOfActualPlayer).Ip;
                }
                else
                {
                    return NextPlayer();
                }
            }
            else
            {
                OrderNumberOfActualPlayer = 0;

                if (Players[OrderNumberOfActualPlayer].Online)
                {
                    IpActualPlayer = Players.ElementAt(OrderNumberOfActualPlayer).Ip;
                }
                else
                {
                    return NextPlayer();
                }
            }

            return IpActualPlayer;
        }

        public int NumberOrder(string ipPlayer)
        {
            int i = 0;
            foreach(User user in Players)
            {
                if(user.Ip == ipPlayer)
                {
                    break;
                }
                i += 1;
            }
            return i;
        }

        public void Distribute()
        {
            deck.reset();

            deck.Jeu.Shuffle();
            deck.Jeu.Shuffle();
            deck.Jeu.Shuffle();

            int i = 0;
            do
            {
                Players[i].Hand.Add(deck.Jeu.ElementAt(0));
                Players[i].Hand.Add(deck.Jeu.ElementAt(1));
                
                deck.Jeu.RemoveAt(1);
                deck.Jeu.RemoveAt(0);
            } while (i >= Players.Count);

            deck.Cour = deck.Jeu;
            deck.Jeu.Clear();
        }

        public void CheckCardsAlive()
        {
            DuchessAlive = 0;
            CapitainAlive = 0;
            ComptessAlive = 0;
            KillerAlive = 0;
            AmbassadorAlive = 0;
            InquisitorsAlive = 0;

            foreach(User user in Players)
            {
                foreach (Card card in user.Hand)
                {
                    switch (card.type)
                    {
                        case enumTypes.AMBASSADOR:
                            AmbassadorAlive += 1;
                            break;

                        case enumTypes.CAPITAIN:
                            CapitainAlive += 1;
                            break;

                        case enumTypes.COMPTESS:
                            ComptessAlive += 1;
                            break;

                        case enumTypes.DUCHESS:
                            DuchessAlive += 1;
                            break;

                        case enumTypes.INQUISITOR:
                            InquisitorsAlive += 1;
                            break;

                        case enumTypes.KILLER:
                            KillerAlive += 1;
                            break;
                    }
                }
            }
            
            foreach (Card card in deck.Cour)
            {
                switch (card.type)
                {
                    case enumTypes.AMBASSADOR:
                        AmbassadorAlive += 1;
                        break;

                    case enumTypes.CAPITAIN:
                        CapitainAlive += 1;
                        break;

                    case enumTypes.COMPTESS:
                        ComptessAlive += 1;
                        break;

                    case enumTypes.DUCHESS:
                        DuchessAlive += 1;
                        break;

                    case enumTypes.INQUISITOR:
                        InquisitorsAlive += 1;
                        break;

                    case enumTypes.KILLER:
                        KillerAlive += 1;
                        break;
                }
            }
        }
        
        public List<Card> GetRandomCards(int numberOfCards)
        {
            int random = 0;
            List<Card> tirage = new List<Card>();

            for(int i = 1; i <= numberOfCards; i++)
            {
                random = new Random().Next(0, deck.Cour.Count - 1);
                tirage.Add(deck.Cour.ElementAt(random));
                deck.Cour.RemoveAt(random);
            }
            
            return tirage;
        }

        public int NumberPlayersAlive()
        {
            int numberPlayerAlive = 0; 

            foreach(User user in Players)
            {
                if(user.Online)
                {
                    if(user.Hand.Count > 0)
                    {
                        numberPlayerAlive += 1;
                    }
                }
            }

            return numberPlayerAlive;
        }

        public void Restart()
        {
            OrderNumberOfActualPlayer = 0;
            IpActualPlayer = Players.ElementAt(OrderNumberOfActualPlayer).Ip;
            DuchessAlive = 4;
            CapitainAlive = 4;
            ComptessAlive = 4;
            KillerAlive = 4;
            AmbassadorAlive = 4;
            InquisitorsAlive = 4;
            MustAnswer = "";
            CanInterrupt = false;

            foreach(User user in Players)
            {
                user.reset();
            }

            Distribute();
        }
    }

}
