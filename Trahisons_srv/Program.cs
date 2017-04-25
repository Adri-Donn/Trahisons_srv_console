using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net;
using Trahisons_srv.websockets;

namespace Trahisons_srv
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Lancement du serveur..");
            new GameManager();
        }
    }

    enum EnumActionProcessing
    {
        NONE,
        SOCIALHELP,
        AMBASSADORSELECTINGCARDS,
        CAPITAINSTEALINGMONEY,
        DUCHESSTAKINGINBANK,
        INQUISITORCONSULTING,
        INQUISITORCONSULTINGANDCHANGING,
        INQUISITORSELECTINGCARD,
        KILLERKILLING,
        WAITINGRETREIVINGCARDS,
        WAITINGIFCHALLENGE,
        MUSTKILLONECARD
    }

    enum enumTypesChallenge
    {
        NONE,
        AMBASSADOR,
        COMPTESS,
        CAPITAIN,
        DUCHESS,
        INQUISITOR,
        KILLER
    }

    public enum EnumTypeMSG
    {
        ACTION,
        ORDER,
        ANNONCE,
        PROPOSITION,
        SELECTION,
        CONSULT,
        GET,
        SET,
        ANSWER,
        NEXTPLAYER,
        RESET
    }

    public enum EnumTypeACTIONS
    {
        ROOM,
        REVENUE,
        NEWPLAYER,
        MONEY,
        MONEYERROR,
        SOCIALHELP,
        POWER,
        MURDER,
        CHALLENGE,
        SECONDCHALLENGE,
        AMBASSADORSELECTINGCARDS,
        RETRIEVINGCARDSAMBASSADOR,
        CAPITAINSTEALINGMONEY,
        DUCHESSTAKINGINBANK,
        INQUISITORCONSULTING,
        INQUISITORSELECTINGCARD,
        RETRIEVINGCARDINQUISITOR,
        KILLERKILLING,
        KILLONECARD,
        MUSTKILLONECARD,
        KILLCARD,
        KILLED,
        WAIT,
        SRVVERS,
        MINCLTVERS,
        NAME,
        NAMEOFAPLAYER,
        NUMBER_AMBASSADOR,
        NUMBER_COMPTESS,
        NUMBER_CAPITAIN,
        NUMBER_DUCHESS,
        NUMBER_INQUISITOR,
        NUMBER_KILLER,
        CARD,
        CARDS,
        NOCHALLENGE,
        WINNER,
        NUMBEROFPLAYERS,
        PLAYER,
        NEXTPLAYER,
        YOUR,
        LOOSEONECARD
    }
}