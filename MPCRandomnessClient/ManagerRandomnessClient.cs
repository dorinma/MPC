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
        public const int n = 10; // n
        public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n; // dpf gate for each index - n 

        private static DcfAdapterRandClient dcfAdapter = new DcfAdapterRandClient();
        private static DpfAdapterRandClient dpfAdapter = new DpfAdapterRandClient();

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

            //sort
            CreateAndSendCircuits();
            communicationA.Reset();
            communicationB.Reset();
            //other circuits..
        }

        public static void CreateAndSendCircuits()
        {
            //for op
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            CreateCircuits(newSessionId, out RandomRequest requestA, out RandomRequest requestB);
            SendToServers(newSessionId, requestA, requestB);
        }

        public static void CreateCircuits(string sessionId ,out RandomRequest requestA, out RandomRequest requestB)
        {
            //create masks and split them to shares
            //dcf
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            //dpf
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);

            requestA = new RandomRequest
            {
                sessionId = sessionId,
                n = n,
                dcfMasks = dcfSharesA, //also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[dcfGatesCount],
                dcfKeysSmallerUpperBound = new string[dcfGatesCount],
                shares01 = new uint[dcfGatesCount],
                dcfAesKeysLower = new string[dcfGatesCount],
                dcfAesKeysUpper = new string[dcfGatesCount],
                dpfMasks = dpfSharesA,
                dpfKeys = new string[dpfGatesCount],
                dpfAesKeys = new string[dpfGatesCount]
            };

            requestB = new RandomRequest
            {
                sessionId = sessionId,
                n = n,
                dcfMasks = dcfSharesB, //also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[dcfGatesCount],
                dcfKeysSmallerUpperBound = new string[dcfGatesCount],
                shares01 = new uint[dcfGatesCount],
                dcfAesKeysLower = new string[dcfGatesCount],
                dcfAesKeysUpper = new string[dcfGatesCount],
                dpfMasks = dpfSharesB,
                dpfKeys = new string[dpfGatesCount],
                dpfAesKeys = new string[dpfGatesCount]
            };

            //create keys
            GenerateDcfKeys(dcfMasks, requestA, requestB);
            GenerateDpfKeys(dpfMasks, outputMasks: dcfMasks, requestA, requestB);
        }

        public static void GenerateDcfKeys(uint[] masks, RandomRequest requestA, RandomRequest requestB)
        {
            // We define the range of the input to be between 0 to 2^31-1. (So the other half (from 2^31-1 to 2^32-1) will use for negative numbers)
            // In regulre state, this range is the positive area but it change with the masks diff 
            // Now we define the change in the offset.
            // Basically we have a lower and upper bound for negative area.

            int keyIndex = 0;
            uint[] shares01 = new uint[requestA.shares01.Length];
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    uint maxValuaWithDiff = uint.MaxValue + (masks[i] - masks[j]);
                    uint halfMaxValuaWithDiff = uint.MaxValue / 2 + (masks[i] - masks[j]);
                    string keyA1, keyA2, keyB1, keyB2, aesKey1, aesKey2;
                    shares01[keyIndex] = (maxValuaWithDiff > halfMaxValuaWithDiff) ? (uint)0 : 1;
                    dcfAdapter.GenerateDCF(halfMaxValuaWithDiff, out keyA1, out keyB1, out aesKey1); 
                    dcfAdapter.GenerateDCF(maxValuaWithDiff, out keyA2, out keyB2, out aesKey2);

                    requestA.dcfKeysSmallerLowerBound[keyIndex] = keyA1;
                    requestA.dcfKeysSmallerUpperBound[keyIndex] = keyA2;

                    requestB.dcfKeysSmallerLowerBound[keyIndex] = keyB1;
                    requestB.dcfKeysSmallerUpperBound[keyIndex] = keyB2;

                    requestA.dcfAesKeysLower[keyIndex] = aesKey1;
                    requestB.dcfAesKeysLower[keyIndex] = aesKey1;

                    requestA.dcfAesKeysUpper[keyIndex] = aesKey2;
                    requestB.dcfAesKeysUpper[keyIndex] = aesKey2;
                    keyIndex++;
                }
                RandomUtils.SplitToSecretShares(shares01, out uint[] shares01A, out uint[] shares01B);
                requestA.shares01 = shares01A;
                requestB.shares01 = shares01B;
            }
        }
        public static void GenerateDpfKeys(uint[] masks, uint[] outputMasks, RandomRequest requestA, RandomRequest requestB)
        {
            for (int i = 0; i < n; i++)
            {
                dpfAdapter.GenerateDPF(masks[i], 0 - outputMasks[i], out string keyA, out string keyB, out string aesKey);
                requestA.dpfKeys[i] = keyA;
                requestB.dpfKeys[i] = keyB;

                requestA.dpfAesKeys[i] = aesKey;
                requestB.dpfAesKeys[i] = aesKey;
            }
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

        //GUI
        public bool Run(string ip_1, string ip_2, int port_1, int port_2)
        {
            bool res = true;
            communicationA = new CommunicationRandClient();
            communicationB = new CommunicationRandClient();

            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            communicationA.sessionId = newSessionId;
            communicationB.sessionId = newSessionId;

            //random requeset
            CreateCircuits(newSessionId, out RandomRequest requestA, out RandomRequest requestB);


            // send to servers
            //connect
            communicationA.Connect(ip_1, port_1);
            communicationB.Connect(ip_2, port_2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send 
            communicationA.SendMasksAndKeys(requestA);
            communicationB.SendMasksAndKeys(requestB);
            //recieve confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();

            if (!communicationA.serversVerified || !communicationB.serversVerified)
            {
                res = false;
            }

            communicationA.Reset();
            communicationB.Reset();
            return res;
        }
    }
}
