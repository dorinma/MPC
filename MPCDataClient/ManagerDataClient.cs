namespace MPCDataClient
{ 
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ManagerDataClient
    {
        static bool debug = false;
        UserService userService;
        List<UInt16> data;
        CommunicationDataClient<UInt16> commServerA;

        private static UserService userService1 = new UserService();
        private static CommunicationDataClient<UInt16> communicationA;
        private static CommunicationDataClient<UInt16> communicationB;

        public ManagerDataClient()
        {
            userService = new UserService();
        }

        static void Main(string[] args) // ip1 port1 ip2 port2
        {
            // -----------------------------------------------------------------------
            // All comments are for testing with only 1 local server
            // -----------------------------------------------------------------------

            // TODO send init msg first
            //if (args.Length < 4)
            if (args.Length < 2)
            {
                Console.WriteLine("Missing servers' communication details.");
                Environment.Exit(-1);
            }

            if (args.Length > 4 && args[4].Equals("-d"))
            {
                debug = true;
            }

            string ip1 = args[0];
            //string ip2 = args[2];
            int port1 = 0;
            //int port2 = 0;
            try
            {
                port1 = Int32.Parse(args[1]);
                //port2 = Int32.Parse(args[3]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid port number.");
                Environment.Exit(-1);
            }

            Start(ip1, port1);
            
        }

        private static void Start(string ip1, int port1)
        {
            communicationA = new CommunicationDataClient<UInt16>(ip1, port1);
            string sessionId;
            if (userService1.StartSession(out int operation, out uint numberOfUsers))
            {
                communicationA.Connect();
                sessionId = communicationA.SendInitMessage(operation, (int)numberOfUsers);
                Console.WriteLine($"Session id: {sessionId}");
            }
            else
            {
                sessionId = userService1.ReadSessionId();
                communicationA.Connect();
            }




            List<UInt16> data = userService1.ReadData("");

            DataService dataService = new DataService();
            dataService.generateSecretShares(data);

            CommunicationDataClient<UInt16> commServerA = new CommunicationDataClient<UInt16>(ip1, port1);
            //CommunicationDataClient<UInt16> commServerB = new CommunicationDataClient<UInt16>(ip2, port2);

            Console.WriteLine($"ip1: {ip1} port1: {port1}");
            //Console.WriteLine($"ip2: {ip2} port2: {port2}");

            commServerA.Connect();
            //commServerB.Connect();

            Console.WriteLine("Connect to servers successfuly");

            //commServerA.SendRequest(dataService.serverAList);
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
        }

        public bool ReadInput(string filePath)
        {
            data = userService.ReadData(filePath);
            if (data == null) return false;
            else return true;
        }

        public string StartSession(string ip1, int port1)
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
        }
    }
}
