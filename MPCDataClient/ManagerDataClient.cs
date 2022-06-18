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

        static void Main(string[] args) //args = [ip1, port1, ip2, port2]
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
            string sessionId = "";

            if (!Int32.TryParse(args[1], out int port1) | !Int32.TryParse(args[3], out int port2))
            {
                Console.WriteLine($"Invalid port number.");
                Environment.Exit(-1);
            }

            if (userService1.StartSession(out OPERATION operation, out int numberOfUsers))
            {
                sessionId = InitConnectionNewSession(ip1, port1, operation, numberOfUsers);
            }
            else
            {
                sessionId = userService1.ReadSessionId();
                InitConnectionExistingSession(ip1, port1, sessionId);
            }

            uint[] data = userService1.ReadData().ToArray();

            Run(ip2, port2, sessionId, data, debug);
        }


        public static string InitConnectionNewSession(string ip, int port, OPERATION operation, int numberOfUsers)
        {
            communicationA = new CommunicationDataClient();
            string sessionId;
            communicationA.Connect(ip, port);
            sessionId = communicationA.SendInitMessage(operation, numberOfUsers);
            communicationA.receiveDone.Reset();

            return sessionId;
        }

        public static void InitConnectionExistingSession(string ip, int port, string sessionId)
        {
            communicationA = new CommunicationDataClient();
            communicationA.Connect(ip, port);
            communicationA.receiveDone.Reset();
        }

        public static string Run(string ip, int port, string sessionId, uint[] data, bool debugMode)
        {
            communicationB = new CommunicationDataClient();

            RandomUtils.SplitToSecretShares(data, out uint[] serverAShares, out uint[] serverBShares);
            
            communicationB.Connect(ip, port);

            communicationA.SendSharesToServer(sessionId, serverAShares);
            communicationB.SendSharesToServer(sessionId, serverBShares);

            communicationA.Receive();
            communicationB.Receive();

            Console.WriteLine("\nWaiting for results.");

            //Wait for resaults
            communicationA.receiveDone.WaitOne();
            communicationA.CloseSocket();

            communicationB.receiveDone.WaitOne();
            communicationB.CloseSocket();

            Console.WriteLine("Sockets are closed.");

            if (communicationA.dataResponse.Count > 0 && communicationB.dataResponse.Count > 0)
            {
                if (debugMode)
                {
                    Console.WriteLine(
                    $"Output list: {string.Join(", ", communicationA.dataResponse.Zip(communicationB.dataResponse, (x, y) => { return (uint)(x + y); }).ToList())}");

                    var fileName = $"results_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.csv";
                    MPCFiles.writeToFile(communicationA.dataResponse.Zip(communicationB.dataResponse,
                        (x, y) => { return (uint)(x + y); }).ToArray(), $@"..\..\..\..\Results\{fileName}");

                    if(communicationA.response.Length > 0)
                    {
                        //return communicationA.response.Length + $"\nOutput is saved to Results\\{fileName}";
                        return $"\nOutput is saved to Results\\{fileName}";
                    }
                }
            }

            if (communicationA.response.Length > 0 && communicationB.response.Length > 0)
            {
                Console.WriteLine(communicationA.response);
                return communicationA.response;
            }            
            
            return "Something went wrong.";
        }

        public uint[] ReadInput(string filePath)
        {
            data = userService.ReadData(filePath).ToArray();
            return data;
        }
    }
}
