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
        public const int N = 10; // n
        /*public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n; // dpf gate for each index - n */

        private static DcfAdapterRandClient dcfAdapter = new DcfAdapterRandClient();
        private static DpfAdapterRandClient dpfAdapter = new DpfAdapterRandClient();

        private static CommunicationRandClient communicationA;
        private static CommunicationRandClient communicationB;
        private static string ip1 = "127.0.0.1";
        private static string ip2 = "127.0.0.1";
        private static int port1 = 2022;
        private static int port2 = 2023;

        private const int RETRY_COUNT = 5;
        private const int SLEEP_TIME = 3000; //3 seconds

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
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            CreateCircuits(newSessionId, out SortRandomRequest sortRequestA, out SortRandomRequest sortRequestB, n: N);
            SendToServers(newSessionId, sortRequestA, sortRequestB);
        }

        public static void CreateCircuits(string sessionId ,out SortRandomRequest sortRequestA, out SortRandomRequest sortRequestB, int n)
        {
            int dcfMasksCount = n; // mask for each input element
            int dpfMasksCount = n; // mask for each element's index sum 
            int dcfGatesCount = n * (n - 1) / 2; // first layer (dcf gates) - n choose 2.
            int dpfGatesCount = n; // dpf gate for each index - n 
            //create masks and split them to shares
            //dcf
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            //dpf
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);

            sortRequestA = new SortRandomRequest
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

            sortRequestB = new SortRandomRequest
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
            GenerateDcfKeys(n, dcfMasks, sortRequestA, sortRequestB);
            GenerateDpfKeys(n, dpfMasks, outputMasks: dcfMasks, sortRequestA, sortRequestB);
        }

        public static void GenerateDcfKeys(int n, uint[] masks, SortRandomRequest sortRequestA, SortRandomRequest sortRequestB)
        {
            // We define the range of the input to be between 0 to 2^31-1. (So the other half (from 2^31-1 to 2^32-1) will use for negative numbers)
            // In regulre state, this range is the positive area but it change with the masks diff 
            // Now we define the change in the offset.
            // Basically we have a lower and upper bound for negative area.

            int keyIndex = 0;
            uint[] shares01 = new uint[sortRequestA.shares01.Length];
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

                    sortRequestA.dcfKeysSmallerLowerBound[keyIndex] = keyA1;
                    sortRequestA.dcfKeysSmallerUpperBound[keyIndex] = keyA2;

                    sortRequestB.dcfKeysSmallerLowerBound[keyIndex] = keyB1;
                    sortRequestB.dcfKeysSmallerUpperBound[keyIndex] = keyB2;

                    sortRequestA.dcfAesKeysLower[keyIndex] = aesKey1;
                    sortRequestB.dcfAesKeysLower[keyIndex] = aesKey1;

                    sortRequestA.dcfAesKeysUpper[keyIndex] = aesKey2;
                    sortRequestB.dcfAesKeysUpper[keyIndex] = aesKey2;
                    keyIndex++;
                }
                RandomUtils.SplitToSecretShares(shares01, out uint[] shares01A, out uint[] shares01B);
                sortRequestA.shares01 = shares01A;
                sortRequestB.shares01 = shares01B;
            }
        }
        public static void GenerateDpfKeys(int n, uint[] masks, uint[] outputMasks, SortRandomRequest sortRequestA, SortRandomRequest sortRequestB)
        {
            for (int i = 0; i < n; i++)
            {
                dpfAdapter.GenerateDPF(masks[i], 0 - outputMasks[i], out string keyA, out string keyB, out string aesKey);
                sortRequestA.dpfKeys[i] = keyA;
                sortRequestB.dpfKeys[i] = keyB;

                sortRequestA.dpfAesKeys[i] = aesKey;
                sortRequestB.dpfAesKeys[i] = aesKey;
            }
        }

        private static void SendToServers(string sessionId, SortRandomRequest sortRequestA, SortRandomRequest sortRequestB)
        {
            communicationA.sessionId = sessionId;
            communicationB.sessionId = sessionId;
            // send to servers
            //connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send (need to verify that both server received correctly)

            SendRadomness(sortRequestA, sortRequestB);

            int tries = 0;

            while ((!communicationA.serversVerified || !communicationB.serversVerified) && tries < RETRY_COUNT)
            {
                Console.WriteLine($"Try #{tries + 1}: at least one server did not get the masks and keys correctly.");
                
                System.Threading.Thread.Sleep(SLEEP_TIME);
                tries++;
                SendRadomness(sortRequestA, sortRequestB);
            }

            if (communicationA.serversVerified && communicationB.serversVerified)
            { 
                Console.WriteLine($"\nThe correlated randomness was sent successfully to both servers.");
            }
            else
            {
                Console.WriteLine("Failed to send correlated randomness to the servers.");
            }
        }

        private static void SendRadomness(SortRandomRequest sortRequestA, SortRandomRequest sortRequestB)
        {
            communicationA.SendMasksAndKeys(sortRequestA);
            communicationB.SendMasksAndKeys(sortRequestB);
            //receive confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();
        }

        /*
        public bool Run(string ip_1, string ip_2, int port_1, int port_2)
        {
            bool res = true;
            communicationA = new CommunicationRandClient();
            communicationB = new CommunicationRandClient();

            //sort
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            communicationA.sessionId = newSessionId;
            communicationB.sessionId = newSessionId;

            //random requeset
            CreateCircuits(newSessionId, out SortRandomRequest sortRequestA, out SortRandomRequest sortRequestB);


            // send to servers
            //connect
            communicationA.Connect(ip_1, port_1);
            communicationB.Connect(ip_2, port_2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send 
            communicationA.SendMasksAndKeys(sortRequestA);
            communicationB.SendMasksAndKeys(sortRequestB);
            //receive confirmation
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
        */
    }
}
