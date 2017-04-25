using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trahisons_srv.Class
{
    class Card
    {
        public Card(enumTypes type)
        {
            this.type = type;
        }

        public Card(int type)
        {
            this.type = (enumTypes)type;
        }

        public enumTypes type { get; set; }
    }

    enum enumTypes
    {
        AMBASSADOR,
        COMPTESS,
        CAPITAIN,
        DUCHESS,
        INQUISITOR,
        KILLER,
        CHALLENGE
    }
}
