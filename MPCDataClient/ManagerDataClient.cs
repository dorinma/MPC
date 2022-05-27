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
                Console.WriteLine($"\nThe session id for your computation is: {sessionId}");
            }
            else
            {
                sessionId = userService1.ReadSessionId();
                communicationA.Connect(ip1, port1);
            }

            uint[] data = userService1.ReadData().ToArray();

            Console.WriteLine($"\nThe input values are:");
            for (int i = 0; i < data.Length; i++)
            {
                Console.WriteLine(i + ". " + data[i]);
            }

            RandomUtils.SplitToSecretShares(data, out uint[] serverAShares, out uint[] serverBShares);

            communicationB.Connect(ip2, port2);

            communicationA.SendSharesToServer(sessionId, serverAShares);
            communicationB.SendSharesToServer(sessionId, serverBShares);

            communicationA.Receive();
            communicationB.Receive();

            Console.WriteLine("\nwait for results");

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

        public string InitConnectionNewSession(string ip, int port, int operation, int numberOfUsers)
        {
            communicationA = new CommunicationDataClient();
            string sessionId;
            communicationA.Connect(ip, port);
            sessionId = communicationA.SendInitMessage(operation, numberOfUsers);
            communicationA.receiveDone.Reset();

            return sessionId;
        }

        public void InitConnectionExistingSession(string ip, int port, string sessionId)
        {
            communicationA = new CommunicationDataClient();
            communicationA.Connect(ip, port);
            communicationA.receiveDone.Reset();
        }

        public string Run(string ip, int port, string sessionId, uint[] data, bool debugMode)
        {
            communicationB = new CommunicationDataClient();

            RandomUtils.SplitToSecretShares(data, out uint[] serverAShares, out uint[] serverBShares);
            
            communicationB.Connect(ip, port);

            communicationA.SendSharesToServer(sessionId, serverAShares);
            communicationB.SendSharesToServer(sessionId, serverBShares);

            communicationA.Receive();
            communicationB.Receive();

            //Wait for resaults
            communicationA.receiveDone.WaitOne();
            communicationA.CloseSocket();

            communicationB.receiveDone.WaitOne();
            communicationB.CloseSocket();

            if(debugMode)
            {
                return String.Join(", ", communicationA.dataResponse.Zip(communicationB.dataResponse, (x, y) => { return (uint)(x + y); }).ToList());
            }
            return "";

        }

        public uint[] ReadInput(string filePath)
        {
            data = userService.ReadData(filePath).ToArray();
            //if (data == null) return false;
            return data;
        }
    }
}
