using MPCProtocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCRandomnessClient
{
    public class ManagerRandomnessClient
    {
        public const int n = 2; // n
        public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n*n; // first layer (dcf gates) - last layer (dpf gates) - n*n 

        private static ExternalSystemAdapter externalSystemAdapter = new ExternalSystemAdapter();

        private static CommunicationRandClient communicationA;
        private static CommunicationRandClient communicationB;
        private static string ip1 = "127.0.0.1";
        private static string ip2 = "127.0.0.1";
        private static int port1 = 2022;
        private static int port2 = 2023;

        public static void Main(string[] args)
        {
            //while with timer
            communicationA = new CommunicationRandClient();
            communicationB = new CommunicationRandClient();
            //sort
            CreateAndSendCircuits();
            communicationA.Reset();
            communicationB.Reset();
            //other circuits..
        }

        private static void CreateAndSendCircuits()
        {
            string newSessionId = Randomness.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            communicationA.sessionId = newSessionId;
            communicationB.sessionId = newSessionId;
            //dcf
            //create masks and shares
            uint[] dcfMasks = Randomness.CreateRandomMasks(dcfMasksCount);
            /*uint[] dcfSharesA = new uint[dcfMasksCount];
            uint[] dcfSharesB = new uint[dcfMasksCount];*/
            Randomness.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            //generate keys
            byte[][] dcfKeysA = new byte[dcfGatesCount][];
            byte[][] dcfKeysB = new byte[dcfGatesCount][];
            
            GenerateDcfKeys(dcfMasks, dcfKeysA, dcfKeysB);

            //dpf
            //create masks and shares
            uint[] dpfMasks = Randomness.CreateRandomMasks(dpfMasksCount);
            Randomness.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);
            //generate keys
            byte[][] dpfKeysA = new byte[dpfGatesCount][];
            byte[][] dpfKeysB = new byte[dpfGatesCount][];

            GenerateDpfKeys(dpfMasks, dpfKeysA, dpfKeysB);

            // send to servers
            //connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send (need to verify that both server recieved correctly)
            communicationA.SendMasksAndKeys(n, dcfSharesA, dcfKeysA, dpfSharesA, dpfKeysA);
            communicationB.SendMasksAndKeys(n, dcfSharesB, dcfKeysB, dpfSharesB, dpfKeysB);
            //recieve confirmation
            communicationA.Receive();
            communicationB.Receive();

            communicationA.receiveDone.WaitOne();
            communicationB.receiveDone.WaitOne();

            if(!communicationA.serversVerified || !communicationB.serversVerified)
            {
                // retry ? 
                Console.WriteLine("At least one server did not get the masks and keys correctly");
            }
            else
            {
                Console.WriteLine("Success");
            }
            int i = 8;
        }

        public static void GenerateDcfKeys(uint[] masks, byte[][] keysA, byte[][] keysB)
        {
            int keyIndex;
            for(int i = 0; i < n; i++)
            {
                for (int j = i+1; j < n; j++)
                { 
                    externalSystemAdapter.GenerateDCF(5, out byte[] keyA, out byte[] keyB); // mask1-mask2
                    // ---------------------------
                    uint a = 3437318326;
                    uint b = 857648971;
                    uint c = a + b;
                    uint shareA = externalSystemAdapter.EvalDcf("A", keyA, 6);
                    uint shareB = externalSystemAdapter.EvalDcf("B", keyB, 6);

                    var x = shareA + shareB;
                    // ---------------------------
                    keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1; // calculate the index for keyij -> key for the gate with input with mask i and j
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                }
            }
        }

        public static void GenerateDpfKeys(uint[] masks, byte[][] keysA, byte[][] keysB)
        {
            int keyIndex;
            for (int i = 0; i < n; i++)
            {
                foreach (int index in Enumerable.Range(0, n))
                {
                    externalSystemAdapter.GenerateDPF((uint)index + masks[i], out byte[] keyA, out byte[] keyB);
                    keyIndex = i * n + index;
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                }
            }
        }
    }
}
