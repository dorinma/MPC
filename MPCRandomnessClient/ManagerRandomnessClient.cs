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
        private static string ip1 = "127.0.0.1";
        private static string ip2 = "127.0.0.1";
        private static int port1 = 2022;
        private static int port2 = 2023;

        public static void Main(string[] args)
        {
            // future code - while with timer
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
            Console.WriteLine($"New session is {newSessionId}");
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
                dcfMasks = dcfSharesA, //also masks for the dpf output
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
                dcfMasks = dcfSharesB, //also masks for the dpf output
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
            // send to servers
            //connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send (need to verify that both server recieved correctly)

            communicationA.SendMasksAndKeys(requestA);
            communicationB.SendMasksAndKeys(requestB);
            //recieve confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();

            if (!communicationA.serversVerified || !communicationB.serversVerified)
            {
                // retry ? 
                Console.WriteLine("At least one server did not get the masks and keys correctly");
            }
            else
            {
                Console.WriteLine($"\nThe correlated randomness was sent successfully to both servers.");
            }
        }
    }
}
