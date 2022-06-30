namespace MPCDataClient
{
    using MPCTools;
    using System;
    using System.Linq;


    public class ManagerDataClient
    {
        UserService userService;
        uint[] data;

        private static UserService userService1 = new UserService();
        private static CommunicationDataClient communicationA;
        private static CommunicationDataClient communicationB;

        public ManagerDataClient()
        {
            userService = new UserService();
        }

        static void Main(string[] args) // args = [ip1, port1, ip2, port2]
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Missing servers' communication details.");
                Environment.Exit(-1);
            }

            string ip1 = args[0];
            string ip2 = args[2];
            string sessionId;

            if (!Int32.TryParse(args[1], out int port1) | !Int32.TryParse(args[3], out int port2))
            {
                Console.WriteLine($"Invalid port number.");
                Environment.Exit(-1);
            }

            if (userService1.StartSession(out OPERATION operation, out int numberOfUsers, out bool debugMode))
            {
                sessionId = InitConnectionNewSession(ip1, port1, ip2, port2, operation, numberOfUsers, debugMode);
                if(sessionId == String.Empty)
                {
                    Console.WriteLine("Error: Could not create session.");
                    if(communicationA.response != string.Empty)
                    {
                        Console.WriteLine(communicationA.response);
                    }
                    else
                    {
                        Console.WriteLine("Check servers' addresses.");
                    }
                    
                    Environment.Exit(-1);
                }

                Console.WriteLine($"Session id: {sessionId}"); 
            }
            else
            {
                sessionId = userService1.ReadSessionId();
                if(!InitConnectionExistingSession(ip1, port1, sessionId))
                {
                    Console.WriteLine("Error: Could not create session. Check servers' addresses.");
                    Environment.Exit(-1);
                }
            }

            uint[] data = userService1.ReadData().ToArray();

            Run(ip2, port2, sessionId, data);
        }


        public static string InitConnectionNewSession(string ip1, int port1, string ip2, int port2, OPERATION operation, int numberOfUsers, bool debugMode)
        {
            communicationA = new CommunicationDataClient();
            if(port1 != ProtocolConstants.portServerA || port2 != ProtocolConstants.portServerB)
            {
                return String.Empty;
            }

            string sessionId;
            if (communicationA.Connect(ip1, port1))
            {
                sessionId = communicationA.SendInitMessage(operation, numberOfUsers, debugMode);
                communicationA.receiveDone.Reset();
                return sessionId;
            }

            return String.Empty;
        }

        public static bool InitConnectionExistingSession(string ip, int port, string sessionId)
        {
            if (port != ProtocolConstants.portServerA)
            {
                return false;
            }
            else
            {
                communicationA = new CommunicationDataClient();
                communicationA.Connect(ip, port);
                communicationA.receiveDone.Reset();
                return true;
            }
        }

        public static string Run(string ip, int port, string sessionId, uint[] data)
        {
            communicationB = new CommunicationDataClient();

            if (port != ProtocolConstants.portServerB)
            {
                return "Second server's port is incorrect.";
            }

            RandomUtils.SplitToSecretShares(data, out uint[] serverAShares, out uint[] serverBShares);
            
            communicationB.Connect(ip, port);

            communicationA.SendSharesToServer(sessionId, serverAShares);
            communicationB.SendSharesToServer(sessionId, serverBShares);

            communicationA.Receive();
            communicationB.Receive();

            Console.WriteLine("\nWaiting for results.");

            // Wait for resaults
            communicationA.receiveDone.WaitOne();
            communicationA.CloseSocket();

            communicationB.receiveDone.WaitOne();
            communicationB.CloseSocket();

            Console.WriteLine("Sockets are closed.");

            if (communicationA.dataResponse.Count > 0 && communicationB.dataResponse.Count > 0)
            {
                Console.WriteLine(
                $"Output list: {string.Join(", ", communicationA.dataResponse.Zip(communicationB.dataResponse, (x, y) => { return (uint)(x + y); }).ToList())}");

                var fileName = $"results_{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.csv";
                MPCFiles.writeToFile(communicationA.dataResponse.Zip(communicationB.dataResponse,
                    (x, y) => { return (uint)(x + y); }).ToArray(), $@"..\..\..\..\Results\{fileName}");

                if(communicationA.response.Length > 0)
                {
                    return communicationA.response + $"\nOutput is saved to Results\\{fileName}";
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

        public static string GetServerResponse()
        {
            return communicationA.response;
        }
    }
}
