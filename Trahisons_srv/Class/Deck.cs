using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trahisons_srv.Class
{
    class Deck
    {
        public Deck ()
        {
            for (int i = 0; i <= 3; i++)
            {
                Jeu.Add(new Card(enumTypes.AMBASSADOR));
                Jeu.Add(new Card(enumTypes.CAPITAIN));
                Jeu.Add(new Card(enumTypes.COMPTESS));
                Jeu.Add(new Card(enumTypes.DUCHESS));
                Jeu.Add(new Card(enumTypes.INQUISITOR));
                Jeu.Add(new Card(enumTypes.KILLER));
            }
        }

        public List<Card> Jeu = new List<Card>();
        public List<Card> Cour = new List<Card>();
        public List<Card> Cimetiere = new List<Card>();

        public void reset()
        {
            Jeu.Clear();
            Cour.Clear();
            Cimetiere.Clear();

            for(int i = 0; i <= 3; i++)
            {
                Jeu.Add(new Card(enumTypes.AMBASSADOR));
                Jeu.Add(new Card(enumTypes.CAPITAIN));
                Jeu.Add(new Card(enumTypes.COMPTESS));
                Jeu.Add(new Card(enumTypes.DUCHESS));
                Jeu.Add(new Card(enumTypes.INQUISITOR));
                Jeu.Add(new Card(enumTypes.KILLER));
            }     
        }
    }
}
