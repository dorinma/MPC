using MPCTools;
using MPCTools.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCRandomnessClient
{
    public class ManagerRandomnessClient
    {
        private static CommunicationRandClient communicationA;
        private static CommunicationRandClient communicationB;
        private static string ip1;
        private static string ip2;
        private static int port1;
        private static int port2;

        private const int RETRY_COUNT = 5;
        private const int SLEEP_TIME = 3000; //3 seconds

        public static void Main(string[] args) // args = [ip1, port1, ip2, port2]
        {
            if (args.Length < 4)
            {
                Console.WriteLine("[ERROR] Missing servers' communication details.");
                Environment.Exit(-1);
            }

            ip1 = args[0];
            ip2 = args[2];
            bool validPorts = int.TryParse(args[1], out port1) && int.TryParse(args[3], out port2);

            if(!validPorts)
            {
                Console.WriteLine("[ERROR] Invalid ports.");
                Environment.Exit(-1);
            }

            // Future code - while with timer
            communicationA = new CommunicationRandClient();
            communicationB = new CommunicationRandClient();
            int n = 0;
            if (args.Length < 1)
            {
                Console.WriteLine("Missing randomness details.");
                Environment.Exit(-1);
            }

            try
            {
                n = int.Parse(args[0]);
            }
            catch
            {
                Console.WriteLine("Illegal randomness details.");
                Environment.Exit(-1);
            }

            CreateAndSendCircuits(n);

            communicationA.Reset();
            communicationB.Reset();
        }

        public static void CreateAndSendCircuits(int n)
        {
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"[INFO] New session id: {newSessionId}");
            Circuit currCircuit = null;

            foreach (OPERATION op in Operations.operations)
            {
                switch (op)
                {
                    case OPERATION.SORT:
                    {
                        currCircuit =  new SortCircuit(n);
                        break;
                    }
                }
                CreateCircuits(newSessionId, currCircuit, op, out RandomRequest requestA, out RandomRequest requestB);
                SendToServers(newSessionId, requestA, requestB);
            }
        }

        public static void CreateCircuits(string sessionId, Circuit circuit, OPERATION op, out RandomRequest requestA, out RandomRequest requestB)
        {
            //create masks and split them to shares
            //dcf
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(circuit.dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            //dpf
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(circuit.dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);

            requestA = new RandomRequest
            {
                sessionId = sessionId,
                operation = op,
                n = circuit.n,
                dcfMasks = dcfSharesA, // Also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[circuit.dcfGatesCount],
                dcfKeysSmallerUpperBound = new string[circuit.dcfGatesCount],
                shares01 = new uint[circuit.dcfGatesCount],
                dcfAesKeysLower = new string[circuit.dcfGatesCount],
                dcfAesKeysUpper = new string[circuit.dcfGatesCount],
                dpfMasks = dpfSharesA,
                dpfKeys = new string[circuit.dpfGatesCount],
                dpfAesKeys = new string[circuit.dpfGatesCount]
            };

            requestB = new RandomRequest
            {
                sessionId = sessionId,
                operation = op,
                n = circuit.n,
                dcfMasks = dcfSharesB, // Also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[circuit.dcfGatesCount],
                dcfKeysSmallerUpperBound = new string[circuit.dcfGatesCount],
                shares01 = new uint[circuit.dcfGatesCount],
                dcfAesKeysLower = new string[circuit.dcfGatesCount],
                dcfAesKeysUpper = new string[circuit.dcfGatesCount],
                dpfMasks = dpfSharesB,
                dpfKeys = new string[circuit.dpfGatesCount],
                dpfAesKeys = new string[circuit.dpfGatesCount]
            };

            //create keys
            circuit.GenerateDcfKeys(dcfMasks, requestA, requestB);
            circuit.GenerateDpfKeys(dpfMasks, outputMasks: dcfMasks, requestA, requestB);
        }

        private static void SendToServers(string sessionId, RandomRequest requestA, RandomRequest requestB)
        {
            communicationA.sessionId = sessionId;
            communicationB.sessionId = sessionId;
            // Connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();

            // Send
            SendRadomness(requestA, requestB);

            int tries = 0;

            while ((!communicationA.serversVerified || !communicationB.serversVerified) && tries < RETRY_COUNT)
            {
                Console.WriteLine($"[ERROR] Try #{tries + 1}: at least one server did not get the masks and keys correctly.");
                
                System.Threading.Thread.Sleep(SLEEP_TIME);
                tries++;
                SendRadomness(sortRequestA, sortRequestB);
            }

            if (communicationA.serversVerified && communicationB.serversVerified)
            { 
                Console.WriteLine($"[INFO] The correlated randomness was sent successfully to both servers.");
            }
            else
            {
                Console.WriteLine("[ERROR] Failed to send correlated randomness to the servers.");
            }
        }

        private static void SendRadomness(RandomRequest requestA, RandomRequest requestB)
        {
            communicationA.SendMasksAndKeys(requestA);
            communicationB.SendMasksAndKeys(requestB);

            // Receive confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();
        }
    }
}
