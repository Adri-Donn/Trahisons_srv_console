using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Trahisons_srv.Class;
using Trahisons_srv.Extension;

namespace Trahisons_srv.websockets
{
    class GameManager
    {
        /****************************************************************************************************
         * 
         * **************************************************************************************************/
        public const string CURRENTSERVERVERSION = "0.0.1";
        public const string MINCLIENTVERSION = "0.0.1";
        public const int MINPLAYERBYROOM = 1;
        public const int SERVERPORT = 8888;

        /****************************************************************************************************
         * 
         * **************************************************************************************************/
        public static List<Room> roomIcollection = new List<Room>();
        public static Room waitingRoom = new Room("waitingRoom");
        public static Timer timer;
        public static string waitingRoomName = "waitingRoom";
        public static bool firstRun = true;

        private static List<TcpClient> Players = new List<TcpClient>();
        private TcpListener serverSocket = new TcpListener(IPAddress.Any, SERVERPORT);
        private TcpClient clientSocket = default(TcpClient);
        
        /****************************************************************************************************
         * 
         * **************************************************************************************************/
        public const int STARTINGMONEY = 2;
        public const int AMOUTAMONEYCAPITAINCANSTOLE = 2;
        public const int AMOUTMONEYSOCIALHELP = 2;
        public const int AMOUNTMONEYDUCHESSCANTAKEINBANK = 3;

        /****************************************************************************************************
         * 
         * **************************************************************************************************/
        public GameManager()
        {
            SocketStart();
        }

        public void SocketStart()
        {
            serverSocket.Start();
            
            Console.WriteLine("Lancement du socket.. (PORT: " + SERVERPORT + ").");
            while (true)
            {
                clientSocket = this.serverSocket.AcceptTcpClient();
                var user = new User();
                user.Ip = clientSocket.Client.RemoteEndPoint.ToString();
                Players.Add(clientSocket);
                OnClientConnectToServer(user.Ip);
                var thread = new Thread(() => SocketMessage(user.Ip));
                thread.Start();
            }
        }

        private void SocketMessage(string userIp)
        {
            while (clientSocket.Connected)
            {
                try
                {
                    byte[] bytesFrom = new byte[clientSocket.ReceiveBufferSize];
                    NetworkStream networkStream = clientSocket.GetStream();
                    int n = networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                    if (n == 0)
                        break;
                    string dataFromClient = Encoding.UTF8.GetString(bytesFrom);
                    
                    var deserialize = new XmlSerializer(typeof(Message));
                    Message message = (Message)deserialize.Deserialize(new StringReader(cleanDataReceived(dataFromClient)));

                    Console.WriteLine("GET >> " + message.a);

                    MsgClientToServe(userIp, message.typeMSG, message.typeAction, message.a, message.b, message.c, message.d);
                    Console.WriteLine("****************************************************************************************************");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.Data);
                    Console.WriteLine(e.InnerException);
                    Console.WriteLine(e.HResult);
                }
                finally
                {
                    
                }
            }
        }

        public string cleanDataReceived(string data)
        {
            int fc = 0;
            int lc = 0;
            int pos = 0;

            foreach (char c in data)
            {
                if (c == '<' && fc > pos)
                {
                    fc = pos;
                }
                else if (c == '>' && lc < pos)
                {
                    lc = pos;
                }

                pos++;
            }
            
            return data.Substring(fc, lc - fc + 1);
        }

        private void Send(TcpClient client, Message message)
        {
            try
            {
                Byte[] broadcastBytes = null;
                
                var xmlSerializer = new XmlSerializer(typeof(Message));
                NetworkStream networkStream = client.GetStream();

                xmlSerializer.Serialize(networkStream, message);

                broadcastBytes = Encoding.UTF8.GetBytes((xmlSerializer.ToString()));

                networkStream.Write(broadcastBytes, 0, broadcastBytes.Length);
                networkStream.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void broadcast(Message message)
        {
            foreach (TcpClient client in Players)
            {
                this.Send(client, message);
            }
        }
        
        public TcpClient FindTCPCLient(string userIp)
        {
            TcpClient clientRetour = new TcpClient();
            foreach(TcpClient tcpcli in Players)
            {
                if(tcpcli.Client.RemoteEndPoint.ToString() == userIp)
                {
                    clientRetour = tcpcli;
                    break;
                }
            }
            return clientRetour;
        }

        /****************************************************************************************************
         * 
         * **************************************************************************************************/
        public void OnClientConnectToServer(string userIp)
        {
            Console.WriteLine("One new client connected! ID: " + userIp);
            User user = new User();
            user.Ip = userIp;

            waitingRoom.Players.Add(user);

            SendToOnePlayer(user.Ip, EnumTypeMSG.SET, EnumTypeACTIONS.ROOM, "waitingRoom", null, null, null);
            SendToARoom(waitingRoomName, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.NEWPLAYER, null, null, null, null);

            if (firstRun)
            {
                timer = new Timer(timer_Tick, null, 1000 * 5, Timeout.Infinite);
            }

            if (waitingRoom.Players.Count() <= 8)
            {
                timer.Change(1000 * 5, Timeout.Infinite);
            }
        }

        public void OnReceiveFromClient(string userIp, Message message)
        {
            MsgClientToServe(userIp, message.typeMSG, message.typeAction, message.a, message.b, message.c, message.d);
        }

        public void SendToOnePlayer(string userIp, EnumTypeMSG enumTypeMSG, EnumTypeACTIONS enumTypeACTIONS, string firstParam, string secondParam, string thirdParam, string fourthParam)
        {
            Thread.Sleep(500);
            Console.WriteLine("SENDING >> " + firstParam + ":" + secondParam);
            Send(FindTCPCLient(userIp), new Message(enumTypeMSG, enumTypeACTIONS, firstParam, secondParam, thirdParam, fourthParam));
        }

        public void OnClientDisconnectedToServer(string userIp)
        {
            Console.WriteLine("One client disconnected! ID: " + userIp);
            // Disconnect procedure.
        }

        /****************************************************************************************************
         *                                  
         ***************************************************************************************************/
        public void SendToARoom(string idRoom, EnumTypeMSG enumTypeMSG, EnumTypeACTIONS enumTypeACTIONS, string firtsParam, string secondParam, string thirdParam, string fourthParam)
        {
            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);
            if(idRoom == waitingRoomName)
            {
                room = waitingRoom;
            }
            else
            {
                room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);
            }
            
            foreach(User user in room.Players)
            {
                SendToOnePlayer(user.Ip, enumTypeMSG, enumTypeACTIONS, firtsParam, secondParam, thirdParam, fourthParam);
            }
        }

        /****************************************************************************************************
          * 
          ***************************************************************************************************/
        public void MsgClientToServe(string userIp, EnumTypeMSG type, EnumTypeACTIONS enumTypeACTIONS, string paramA, string paramB, string paramC, string paramD)
        {
            string ipClient = userIp;

            Room room = roomIcollection.FirstOrDefault(r => r.Players.Any(u => u.Ip == ipClient));
            if (room == null)
            {
                room = waitingRoom;
            }

            string roomId = room.Id;
            User user = room.Players.FirstOrDefault(u => u.Ip == ipClient);

            if (type == EnumTypeMSG.ACTION)
            {
                if ((room.IpActualPlayer == ipClient || room.CanInterrupt || room.IpChallenger == ipClient || room.MustAnswer == ipClient) && room.ActionProcessing != EnumActionProcessing.MUSTKILLONECARD && room.ActionProcessing != EnumActionProcessing.WAITINGRETREIVINGCARDS)
                {
                    switch (enumTypeACTIONS)
                    {
                        case EnumTypeACTIONS.REVENUE:
                            user.Money += 1;
                            SendToOnePlayer(ipClient, EnumTypeMSG.SET, EnumTypeACTIONS.MONEY, room.OrderNumberOfActualPlayer.ToString(), user.Money.ToString(), null, null);
                            roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players.FirstOrDefault(u => u.Ip == ipClient).Money += 1;
                            NextPlayer(room.Id);
                            break;

                        case EnumTypeACTIONS.SOCIALHELP:
                            roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.SOCIALHELP;
                            roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.DUCHESS;

                            SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.SOCIALHELP, room.OrderNumberOfActualPlayer.ToString(), null, null, null);

                            WaitForSomeoneChallenge(room.Id);
                            break;

                        case EnumTypeACTIONS.POWER:
                            switch (Convert.ToInt32(paramA))
                            {
                                case (int)enumTypes.AMBASSADOR:
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.AMBASSADORSELECTINGCARDS;
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.AMBASSADOR;

                                    SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.AMBASSADORSELECTINGCARDS, room.OrderNumberOfActualPlayer.ToString(), null, null, null);

                                    WaitForSomeoneChallenge(room.Id);
                                    break;

                                case (int)enumTypes.CAPITAIN:
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.CAPITAINSTEALINGMONEY;
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.CAPITAIN;

                                    SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.CAPITAINSTEALINGMONEY, room.OrderNumberOfActualPlayer.ToString(), null, null, null);

                                    // target player

                                    string capitainIpTarget = roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[Convert.ToInt32(paramB)].Ip;

                                    WaitForAnswer(room.Id, capitainIpTarget);
                                    break;

                                case (int)enumTypes.DUCHESS:
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.DUCHESSTAKINGINBANK;
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.DUCHESS;

                                    SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.DUCHESSTAKINGINBANK, room.OrderNumberOfActualPlayer.ToString(), null, null, null);

                                    WaitForSomeoneChallenge(room.Id);
                                    break;

                                case (int)enumTypes.INQUISITOR:
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.INQUISITOR;
                                    if (Convert.ToInt32(paramB) == 0)
                                    {
                                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.INQUISITORCONSULTING;

                                        string inquisitorIpTarget = roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[Convert.ToInt32(paramC)].Ip;

                                        SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORCONSULTING, room.OrderNumberOfActualPlayer.ToString(), null, null, null);
                                        //target player

                                        WaitForAnswer(room.Id, inquisitorIpTarget);
                                    }
                                    else
                                    {
                                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.INQUISITORSELECTINGCARD;

                                        SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORSELECTINGCARD, room.OrderNumberOfActualPlayer.ToString(), null, null, null);

                                        WaitForSomeoneChallenge(room.Id);
                                    }
                                    break;

                                case (int)enumTypes.KILLER:
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).ActionProcessing = EnumActionProcessing.KILLERKILLING;
                                    roomIcollection.FirstOrDefault(r => r.Id == room.Id).TypeChallenge = enumTypesChallenge.KILLER;

                                    SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.KILLERKILLING, room.OrderNumberOfActualPlayer.ToString(), null, null, null);
                                    //targer player

                                    string killerIpTarget = roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[Convert.ToInt32(paramB)].Ip;

                                    WaitForAnswer(room.Id, killerIpTarget);
                                    break;
                            }
                            break;

                        case EnumTypeACTIONS.MURDER:
                            if (user.Money >= 7)
                            {
                                user.Money -= 7;
                                roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players.FirstOrDefault(u => u.Ip == ipClient).Money = user.Money;
                                SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.MURDER, room.OrderNumberOfActualPlayer.ToString(), paramA, null, null);

                                MustKillOneCard(room.Id, Convert.ToInt32(paramA), room.Players[Convert.ToInt32(paramA)].Ip);
                            }
                            else
                            {
                                SendToOnePlayer(ipClient, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.MONEYERROR, null, null, null, null);
                                SendToOnePlayer(ipClient, EnumTypeMSG.SET, EnumTypeACTIONS.MONEY, room.NumberOrder(user.Ip).ToString(), user.Money.ToString(), null, null);
                            }
                            break;

                        case EnumTypeACTIONS.CHALLENGE:
                            StopTimer(room.Id);
                            SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.CHALLENGE, room.NumberOrder(ipClient).ToString(), paramB, null, null);
                            roomIcollection.FirstOrDefault(r => r.Id == room.Id).MustAnswerPlayer = (enumTypes)Convert.ToInt32(paramA);
                            WaitForSecondChallenge(room.Id, ipClient);
                            break;

                        case EnumTypeACTIONS.SECONDCHALLENGE:
                            StopTimer(room.Id);
                            int orderMustAnswer = room.NumberOrder(room.MustAnswer);

                            if (room.Players[orderMustAnswer].HasCard(room.MustAnswerPlayer))
                            {
                                SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.SECONDCHALLENGE, "FAIL", room.OrderNumberOfActualPlayer.ToString(), null, null);
                                MustKillOneCard(room.Id, room.OrderNumberOfActualPlayer, room.IpActualPlayer);
                            }
                            else
                            {
                                SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.SECONDCHALLENGE, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                                ActionSuccess(room.Id);
                                MustKillOneCard(room.Id, orderMustAnswer, room.MustAnswer);
                            }
                            break;
                    }
                }
                else if (room.ActionProcessing == EnumActionProcessing.WAITINGRETREIVINGCARDS && room.MustAnswer == ipClient)
                {
                    if (enumTypeACTIONS == EnumTypeACTIONS.RETRIEVINGCARDSAMBASSADOR)
                    {
                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cour.Add(new Card(Convert.ToInt32(paramA)));
                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cour.Add(new Card(Convert.ToInt32(paramB)));

                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cour.Shuffle();

                        NextPlayer(room.Id);
                    }
                    else if (enumTypeACTIONS == EnumTypeACTIONS.RETRIEVINGCARDINQUISITOR)
                    {
                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cour.Add(new Card(Convert.ToInt32(paramA)));

                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cour.Shuffle();

                        NextPlayer(room.Id);
                    }
                }
                else if (room.ActionProcessing == EnumActionProcessing.MUSTKILLONECARD && room.MustAnswer == ipClient)
                {
                    if (enumTypeACTIONS == EnumTypeACTIONS.KILLCARD)
                    {
                        if (roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[room.NumberOrder(ipClient)].Hand.Count - 1 > 0)
                        {
                            enumTypes cardType = (enumTypes)Convert.ToInt32(paramA);
                            if (roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[room.NumberOrder(ipClient)].HasCard(cardType))
                            {
                                Card card = roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[room.NumberOrder(ipClient)].KillOneCardAndGet(cardType);
                                roomIcollection.FirstOrDefault(r => r.Id == room.Id).deck.Cimetiere.Add(card);
                                SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.KILLONECARD, room.NumberOrder(ipClient).ToString(), null, null, null);
                                roomIcollection.FirstOrDefault(r => r.Id == room.Id).CheckCardsAlive();
                                SendCardsAlive(room.Id);

                                CheckGameEndAndNextPlayerIfOk(room.Id);
                            }
                            else
                            {
                                SendToOnePlayer(ipClient, EnumTypeMSG.ORDER, EnumTypeACTIONS.MUSTKILLONECARD, null, null, null, null);
                            }
                        }
                        else
                        {
                            roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players[room.NumberOrder(ipClient)].Kill();
                            SendToARoom(room.Id, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.KILLED, room.NumberOrder(ipClient).ToString(), null, null, null);
                            CheckGameEndAndNextPlayerIfOk(room.Id);
                        }
                    }
                }
                else
                {
                    SendToOnePlayer(ipClient, EnumTypeMSG.ORDER, EnumTypeACTIONS.WAIT, null, null, null, null);
                }
            }
            else if (type == EnumTypeMSG.GET)
            {
                switch (enumTypeACTIONS)
                {
                    case EnumTypeACTIONS.SRVVERS:
                        Console.WriteLine("Demande de la version du serveur : " + user.Ip);
                        SendToOnePlayer(ipClient, EnumTypeMSG.ANSWER, EnumTypeACTIONS.SRVVERS, CURRENTSERVERVERSION, null, null, null);
                        break;

                    case EnumTypeACTIONS.MINCLTVERS:
                        SendToOnePlayer(ipClient, EnumTypeMSG.ANSWER, EnumTypeACTIONS.MINCLTVERS, MINCLIENTVERSION, null, null, null);
                        break;
                }
            }
            else if (type == EnumTypeMSG.SET)
            {
                switch (enumTypeACTIONS)
                {
                    case EnumTypeACTIONS.NAME:
                        roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players.FirstOrDefault(p => p.Ip == user.Ip).SetName(paramB);
                        SendToARoom(room.Id, EnumTypeMSG.SET, EnumTypeACTIONS.NAMEOFAPLAYER, room.NumberOrder(user.Ip).ToString(), roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players.FirstOrDefault(p => p.Ip == user.Ip).Name, null, null);
                        break;
                }
            }
        }

        public void SendCardsAlive(string idRoom)
        {
            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);

            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_AMBASSADOR, room.AmbassadorAlive.ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_COMPTESS, room.ComptessAlive.ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_CAPITAIN, room.CapitainAlive.ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_DUCHESS, room.DuchessAlive.ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_INQUISITOR, room.InquisitorsAlive.ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBER_KILLER, room.KillerAlive.ToString(), null, null, null);
        }

        public void StopTimer(string idRoom)
        {
            Timer aWT = roomIcollection.FirstOrDefault(r => r.Id == idRoom).timerAction;
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).timerAction = null;
            aWT.Change(Timeout.Infinite, Timeout.Infinite);
            aWT.Dispose();
        }

        public void WaitForAnswer(string idRoom, string ipMustAnswer)
        {
            Timer timerWaitForAnswer = new Timer(WaitForAnswer_Tick, null, 1000 * 10, Timeout.Infinite);

            roomIcollection.FirstOrDefault(r => r.Id == idRoom).timerAction = timerWaitForAnswer;
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = ipMustAnswer;
        }

        public void WaitForAnswer_Tick(Object state)
        {
            string idRoom = ((Room)state).Id;

            StopTimer(idRoom);

            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);

            int orderNumberAcualPlayer = room.OrderNumberOfActualPlayer;
            int orderNumberChallenger = room.NumberOrder(room.MustAnswer);

            switch (room.ActionProcessing)
            {
                case EnumActionProcessing.CAPITAINSTEALINGMONEY:
                    int moneyStoled = roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[orderNumberChallenger].StealMoney(AMOUTAMONEYCAPITAINCANSTOLE);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[orderNumberAcualPlayer].Money += moneyStoled;
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.CAPITAINSTEALINGMONEY, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.INQUISITORCONSULTING:
                    List<Card> listeCarte = roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[room.OrderNumberOfActualPlayer].GetRandomCards(1);
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORCONSULTING, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                    SendToOnePlayer(idRoom, EnumTypeMSG.CONSULT, EnumTypeACTIONS.CARD, room.NumberOrder(room.MustAnswer).ToString(), listeCarte[0].type.ToString(), null, null);
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.KILLERKILLING:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.KILLERKILLING, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    MustKillOneCard(idRoom, orderNumberChallenger, room.MustAnswer);
                    break;
            }
        }

        public void WaitForSomeoneChallenge(string idRoom)
        {
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).CanInterrupt = true;
            Timer actionWaitForSomeoneChallenge = new Timer(WaitForSomeoneChallenge_Tick, null, 1000 * 10, Timeout.Infinite);
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).timerAction = actionWaitForSomeoneChallenge;
        }

        public void WaitForSomeoneChallenge_Tick(Object state)
        {
            string idRoom = ((Room)state).Id;

            StopTimer(idRoom);

            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);

            int orderNumberAcualPlayer = room.OrderNumberOfActualPlayer;

            SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.NOCHALLENGE, null, null, null, null);
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).CanInterrupt = false;

            switch (room.ActionProcessing)
            {
                case EnumActionProcessing.SOCIALHELP:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.SOCIALHELP, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[orderNumberAcualPlayer].Money += AMOUTMONEYSOCIALHELP;
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.AMBASSADORSELECTINGCARDS:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.AMBASSADORSELECTINGCARDS, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    List<Card> twoRandomCards = roomIcollection.FirstOrDefault(r => r.Id == idRoom).GetRandomCards(2);
                    SendToOnePlayer(room.IpActualPlayer, EnumTypeMSG.SELECTION, EnumTypeACTIONS.CARDS, twoRandomCards[0].type.ToString(), twoRandomCards[1].type.ToString(), null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = room.IpActualPlayer;
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).ActionProcessing = EnumActionProcessing.WAITINGRETREIVINGCARDS;
                    break;

                case EnumActionProcessing.DUCHESSTAKINGINBANK:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.DUCHESSTAKINGINBANK, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[orderNumberAcualPlayer].Money += AMOUNTMONEYDUCHESSCANTAKEINBANK;
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.INQUISITORSELECTINGCARD:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORSELECTINGCARD, "SUCCESS", orderNumberAcualPlayer.ToString(), null, null);
                    List<Card> oneRandomCard = roomIcollection.FirstOrDefault(r => r.Id == idRoom).GetRandomCards(1);
                    SendToOnePlayer(room.IpActualPlayer, EnumTypeMSG.SELECTION, EnumTypeACTIONS.CARD, oneRandomCard[0].type.ToString(), null, null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = room.IpActualPlayer;
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).ActionProcessing = EnumActionProcessing.WAITINGRETREIVINGCARDS;
                    break;
            }
        }

        public void WaitForSecondChallenge(string idRoom, string ipMustAnswer)
        {
            Timer timerWaitForSecondChallenge = new Timer(WaitForSecondChallenge_Tick, null, 1000 * 10, Timeout.Infinite);

            roomIcollection.FirstOrDefault(r => r.Id == idRoom).timerAction = timerWaitForSecondChallenge;
            roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = ipMustAnswer;
        }

        public void WaitForSecondChallenge_Tick(Object state)
        {
            string idRoom = ((Room)state).Id;

            StopTimer(idRoom);

            ActionSuccess(idRoom);
        }

        public void ActionSuccess(string idRoom)
        {
            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);

            switch (room.ActionProcessing)
            {
                case EnumActionProcessing.AMBASSADORSELECTINGCARDS:
                    List<Card> twoRandomCards = roomIcollection.FirstOrDefault(r => r.Id == idRoom).GetRandomCards(2);
                    SendToOnePlayer(room.IpActualPlayer, EnumTypeMSG.SELECTION, EnumTypeACTIONS.CARDS, twoRandomCards[0].type.ToString(), twoRandomCards[1].ToString(), null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = room.IpActualPlayer;
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).ActionProcessing = EnumActionProcessing.WAITINGRETREIVINGCARDS;
                    break;

                case EnumActionProcessing.CAPITAINSTEALINGMONEY:
                    int moneyStoled = roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[room.NumberOrder(room.MustAnswer)].StealMoney(AMOUTAMONEYCAPITAINCANSTOLE);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[room.NumberOrder(room.MustAnswer)].Money += moneyStoled;
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.CAPITAINSTEALINGMONEY, "SUCCESS", room.NumberOrder(room.MustAnswer).ToString(), null, null);
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.DUCHESSTAKINGINBANK:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.DUCHESSTAKINGINBANK, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[room.OrderNumberOfActualPlayer].Money += AMOUNTMONEYDUCHESSCANTAKEINBANK;
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.INQUISITORCONSULTING:
                    List<Card> listeCarte = roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players[room.OrderNumberOfActualPlayer].GetRandomCards(1);
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORCONSULTING, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                    SendToOnePlayer(idRoom, EnumTypeMSG.CONSULT, EnumTypeACTIONS.CARD, room.NumberOrder(room.MustAnswer).ToString(), listeCarte[0].type.ToString(), null, null);
                    NextPlayer(idRoom);
                    break;

                case EnumActionProcessing.INQUISITORSELECTINGCARD:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.INQUISITORSELECTINGCARD, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                    List<Card> oneRandomCard = roomIcollection.FirstOrDefault(r => r.Id == idRoom).GetRandomCards(1);
                    SendToOnePlayer(room.IpActualPlayer, EnumTypeMSG.SELECTION, EnumTypeACTIONS.CARD, oneRandomCard[0].type.ToString(), null, null, null);
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).MustAnswer = room.IpActualPlayer;
                    roomIcollection.FirstOrDefault(r => r.Id == idRoom).ActionProcessing = EnumActionProcessing.WAITINGRETREIVINGCARDS;
                    break;

                case EnumActionProcessing.KILLERKILLING:
                    SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.KILLERKILLING, "SUCCESS", room.OrderNumberOfActualPlayer.ToString(), null, null);
                    MustKillOneCard(idRoom, room.OrderNumberOfActualPlayer, room.MustAnswer);
                    break;
            }
        }

        public void CheckGameEndAndNextPlayerIfOk(string idRoom)
        {
            Room room = roomIcollection.FirstOrDefault(r => r.Id == idRoom);

            if (room.NumberPlayersAlive() >= 1)
            {
                NextPlayer(idRoom);
            }
            else
            {
                User user = room.Players.FirstOrDefault(r => r.Online == true);

                SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.WINNER, room.NumberOrder(user.Ip).ToString(), null, null, null);
                List<User> usersOnline = roomIcollection.FirstOrDefault(r => r.Id == idRoom).Players.FindAll(p => p.Online == true);

                foreach (User userO in usersOnline)
                {
                    waitingRoom.Players.Add(userO);
                    SendToOnePlayer(userO.Ip, EnumTypeMSG.SET, EnumTypeACTIONS.ROOM, "waitingRoom", null, null, null);
                }

                roomIcollection.FirstOrDefault(r => r.Id == room.Id).Players.Clear();
                roomIcollection.Remove(room);
            }
        }

        public string NextPlayer(string idRoom)
        {
            string ipNextPlayer = roomIcollection.FirstOrDefault(r => r.Id == idRoom).NextPlayer();

            SendToARoom(idRoom, EnumTypeMSG.NEXTPLAYER, EnumTypeACTIONS.NEXTPLAYER,roomIcollection.FirstOrDefault(r => r.Id == idRoom).NumberOrder(ipNextPlayer).ToString(), null, null, null);
            SendToARoom(idRoom, EnumTypeMSG.ORDER, EnumTypeACTIONS.WAIT, null, null, null, null);
            SendToOnePlayer(ipNextPlayer, EnumTypeMSG.ORDER, EnumTypeACTIONS.YOUR, null, null, null, null);

            return ipNextPlayer;
        }

        public void MustKillOneCard(string idRoom, int numberOrderOfPlayerLoose, string ipPlayerLoose)
        {
            SendToARoom(idRoom, EnumTypeMSG.ANNONCE, EnumTypeACTIONS.LOOSEONECARD, numberOrderOfPlayerLoose.ToString(), null, null, null);
            SendToOnePlayer(ipPlayerLoose, EnumTypeMSG.ORDER, EnumTypeACTIONS.MUSTKILLONECARD, null, null, null, null);
        }

        public void timer_Tick(Object state)
        {
            Random random = new Random();
            timer.Change(Timeout.Infinite, Timeout.Infinite);

            int iterations = waitingRoom.Players.Count();
            
            if (iterations >= MINPLAYERBYROOM)
            {
                string roomName = DateTime.UtcNow + ":" + random.Next();
                Room room = new Room(roomName);
                
                string playerList = "";

                for(int i = (iterations-1); i >=0; i--)
                {
                    if (room.Players.Count < 8)
                    {
                        waitingRoom.Players[i].CurrentRoom = roomName;

                        Console.WriteLine("MOVING PLAYER : " + waitingRoom.Players[i].Name + " => " + roomName);

                        room.Players.Add(waitingRoom.Players[i]);
                        
                        if (room.Players.Count == 0)
                        {
                            playerList += waitingRoom.Players[i].Name;
                        }
                        else
                        {
                            playerList += "#:!:#" + waitingRoom.Players[i].Name;
                        }

                        waitingRoom.Players.RemoveAt(i);
                    }
                }

                roomIcollection.Add(room);

                SendToARoom(roomName, EnumTypeMSG.ORDER, EnumTypeACTIONS.WAIT, null, null, null, null);

                SendToARoom(roomName, EnumTypeMSG.SET, EnumTypeACTIONS.ROOM, roomName, null, null, null);

                room.Restart();

                room.IpActualPlayer = room.IpActualPlayer;

                foreach (User user in room.Players)
                {
                    SendToARoom(roomName, EnumTypeMSG.SET, EnumTypeACTIONS.NUMBEROFPLAYERS, room.Players.Count().ToString(), null, null, null);

                    for (int i = 0; i <= room.Players.Count() - 1; i++)
                    {
                        SendToARoom(roomName, EnumTypeMSG.SET, EnumTypeACTIONS.PLAYER, i.ToString(), room.Players[i].Name.ToString(), null, null);
                        SendToARoom(roomName, EnumTypeMSG.SET, EnumTypeACTIONS.MONEY, i.ToString(), STARTINGMONEY.ToString(), null, null);
                    }

                    SendToOnePlayer(user.Ip, EnumTypeMSG.SET, EnumTypeACTIONS.CARDS, user.Hand[0].type.ToString(), user.Hand[1].type.ToString(), null, null);
                }

                SendToOnePlayer(room.IpActualPlayer, EnumTypeMSG.ORDER, EnumTypeACTIONS.YOUR, null, null, null, null);
                
                timer.Change(1000 * 3, Timeout.Infinite);
            }
        }
    }
}