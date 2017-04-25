using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trahisons_srv.Class
{
    public class Message
    {
        public Message() { }
        public Message (EnumTypeMSG typeMSG, EnumTypeACTIONS enumTypeActions, string a, string b, string c, string d)
        {
            this.typeMSG = typeMSG;
            this.typeAction = enumTypeActions;
            this.a = a;
            this.b = b;
            this.c = c;
            this.d = d;
        }
        public EnumTypeMSG typeMSG { get; set; }
        public EnumTypeACTIONS typeAction { get; set; }
        public string a { get; set; }
        public string b { get; set; }
        public string c { get; set; }
        public string d { get; set; }
    }
}
