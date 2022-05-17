namespace MPCDataClient
{
    using MPCTools;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class ManagerDataClient
    {
        static bool debug = true;
        UserService userService;
        uint[] data;
        //CommunicationDataClient<UInt16> commServerA;

        private static UserService userService1 = new UserService();
        private static CommunicationDataClient communicationA;
        private static CommunicationDataClient communicationB;

        public ManagerDataClient()
        {
            userService = new UserService();
        }

        static void Main(string[] args) // ip1 port1 ip2 port2
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Missing servers' communication details.");
                Environment.Exit(-1);
            }

            if (args.Length > 4 && args[4].Equals("-d"))
            {
                debug = true;
            }

            string ip1 = args[0];
            string ip2 = args[2];
            if (!Int32.TryParse(args[1], out int port1) | !Int32.TryParse(args[3], out int port2))
            {
                Console.WriteLine($"Invalid port number");
                Environment.Exit(-1);
            }
            Start(ip1, port1, ip2, port2);
        }

        private static void Start(string ip1, int port1, string ip2, int port2)
        {
            communicationA = new CommunicationDataClient();
            communicationB = new CommunicationDataClient();
            string sessionId;
            if (userService1.StartSession(out int operation, out uint numberOfUsers))
            {
                communicationA.Connect(ip1, port1);
                sessionId = communicationA.SendInitMessage(operation, (int)numberOfUsers);
                communicationA.receiveDone.Reset();
                Console.WriteLine($"Session id: {sessionId}");
            }
            else
            {
                sessionId = userService1.ReadSessionId();
                communicationA.Connect(ip1, port1);
            }
            
            uint[] data = userService1.ReadData().ToArray();

            RandomUtils.SplitToSecretShares(data, out uint[] serverAShares, out uint[] serverBShares, false);
            /*DataService dataService = new DataService();
            dataService.GenerateSecretShares(data);*/

            communicationB.Connect(ip2, port2);

            communicationA.SendData(sessionId, serverAShares);
            communicationB.SendData(sessionId, serverBShares);

            communicationA.Receive();
            communicationB.Receive();

            Console.WriteLine("wait for results");

            communicationA.receiveDone.WaitOne();
            communicationA.CloseSocket();

            communicationB.receiveDone.WaitOne();
            communicationB.CloseSocket();

            Console.WriteLine("sockets closed");

            if (communicationA.dataResponse.Count > 0 && communicationB.dataResponse.Count > 0)
            {
                /*Console.WriteLine($"Server A list: {String.Join(", ", communicationA.dataResponse)}");
                Console.WriteLine($"Server B list: {String.Join(", ", communicationB.dataResponse)}");*/

                Console.WriteLine(
                    $"Output list: {String.Join(", ", communicationA.dataResponse.Zip(communicationB.dataResponse, (x, y) => { return (uint)(x + y); }).ToList())}");
            }

            if (communicationA.response.Length > 0)
            {
                Console.WriteLine(communicationA.response);
            }
            if (communicationB.response.Length > 0)
            {
                Console.WriteLine(communicationB.response);
            }
        }

        public bool ReadInput(string filePath)
        {
            data = userService.ReadData(filePath).ToArray();
            if (data == null) return false;
            else return true;
        }

       /* public string StartSession(string ip1, int port1)
        {
            commServerA = new CommunicationDataClient<UInt16>(ip1, port1);
            // TODO send request for session id
            return "";
        }

        public void SendData(string ip1, string ip2, int port1, int port2, int operation, string sessionId)
        {
            DataService dataService = new DataService();
            dataService.generateSecretShares(data);

            //CommunicationDataClient<UInt16> commServerA = new CommunicationDataClient<UInt16>(ip1, port1);
            // TODO check connection exists
            //CommunicationDataClient<UInt16> commServerB = new CommunicationDataClient<UInt16>(ip2, port2);

            Console.WriteLine($"ip1: {ip1} port1: {port1}");
            //Console.WriteLine($"ip2: {ip2} port2: {port2}");

            commServerA.Connect();
            //commServerB.Connect();

            Console.WriteLine("Connect to servers successfuly");

            //commServerA.SendRequest(dataService.serverAList, sessionId);
            //commServerB.SendRequest(dataService.serverBList);

            Console.WriteLine("Messages sent to servers");

            commServerA.ReceiveRequest();
            //commServerB.ReceiveRequest();


            commServerA.WaitForReceive();
            //commServerB.WaitForReceive();

            if (debug)
            {
                Console.WriteLine($"Server A list: {String.Join(", ", commServerA.dataResponse)}");
                //Console.WriteLine($"Server B list: {String.Join(", ", commServerB.dataResponse)}");

                //Console.WriteLine(
                //    $"Output list: {String.Join(", ", commServerA.dataResponse.Zip(commServerB.dataResponse, (x, y) => { return (UInt16)(x + y); }).ToList())}");
                Console.WriteLine(
                    $"Output list: {String.Join(", ", commServerA.dataResponse.ToList())}");
            }
            if (commServerA.response.Length > 0)
            {
                Console.WriteLine(commServerA.response);
            }
            //if (commServerB.response.Length > 0)
            //{
            //    Console.WriteLine(commServerB.response);
            //}
        }*/
    }
}
