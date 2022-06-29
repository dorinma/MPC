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

            // Sort
            CreateAndSendCircuits();
            communicationA.Reset();
            communicationB.Reset();
            // Other circuits
        }

        public static void CreateAndSendCircuits()
        {
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"[INFO] New session id: {newSessionId}");
            CreateCircuits(newSessionId, out SortRandomRequest sortRequestA, out SortRandomRequest sortRequestB, n: N);
            SendToServers(newSessionId, sortRequestA, sortRequestB);
        }

        public static void CreateCircuits(string sessionId, out SortRandomRequest sortRequestA, out SortRandomRequest sortRequestB, int n)
        {
            int dcfMasksCount = n; // Mask for each input element
            int dpfMasksCount = n; // Mask for each element's index sum 
            int dcfGatesCount = n * (n - 1) / 2; // First layer (dcf gates) - n choose 2.
            int dpfGatesCount = n; // Dpf gate for each index - n 
            // Create masks and split them to shares
            // Dcf
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            // Dpf
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);

            sortRequestA = new SortRandomRequest
            {
                sessionId = sessionId,
                n = n,
                dcfMasks = dcfSharesA, // Also masks for the dpf output
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
                dcfMasks = dcfSharesB, // Also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[dcfGatesCount],
                dcfKeysSmallerUpperBound = new string[dcfGatesCount],
                shares01 = new uint[dcfGatesCount],
                dcfAesKeysLower = new string[dcfGatesCount],
                dcfAesKeysUpper = new string[dcfGatesCount],
                dpfMasks = dpfSharesB,
                dpfKeys = new string[dpfGatesCount],
                dpfAesKeys = new string[dpfGatesCount]
            };

            // Create keys
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
            // Connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();

            // Send
            SendRadomness(sortRequestA, sortRequestB);

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

        private static void SendRadomness(SortRandomRequest sortRequestA, SortRandomRequest sortRequestB)
        {
            communicationA.SendMasksAndKeys(sortRequestA);
            communicationB.SendMasksAndKeys(sortRequestB);

            // Receive confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();
        }
    }
}
