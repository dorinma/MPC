using MPCTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCRandomnessClient
{
    public class ManagerRandomnessClient
    {
        public const int n = 3; // n
        public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n*n; // first layer (dcf gates) - last layer (dpf gates) - n*n 

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
            string newSessionId = RandomUtils.GenerateSessionId();
            Console.WriteLine($"New session is {newSessionId}");
            communicationA.sessionId = newSessionId;
            communicationB.sessionId = newSessionId;
            //dcf
            //create masks and shares
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB);
            //generate keys
            string[] dcfKeysA = new string[dcfGatesCount];
            string[] dcfKeysB = new string[dcfGatesCount];
            string[] dcfAesKeys = new string[dcfGatesCount];
            
            GenerateDcfKeys(dcfMasks, dcfKeysA, dcfKeysB, dcfAesKeys);

            //dpf
            //create masks and shares
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB);

            //generate keys
            string[] dpfKeysA = new string[dpfGatesCount];
            string[] dpfKeysB = new string[dpfGatesCount];
            string[] dpfAesKeys = new string[dpfGatesCount];

            GenerateDpfKeys(dpfMasks, outputMasks: dcfMasks, dpfKeysA, dpfKeysB, dpfAesKeys);

            // send to servers
            //connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send (need to verify that both server recieved correctly)
            communicationA.SendMasksAndKeys(n, dcfSharesA, dcfKeysA, dcfAesKeys, dpfSharesA, dpfKeysA, dpfAesKeys);
            communicationB.SendMasksAndKeys(n, dcfSharesB, dcfKeysB, dcfAesKeys, dpfSharesB, dpfKeysB, dpfAesKeys);
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
        }

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

            //dcf
            //create masks and shares
            uint[] dcfMasks = RandomUtils.CreateRandomMasks(dcfMasksCount);
            RandomUtils.SplitToSecretShares(dcfMasks, out uint[] dcfSharesA, out uint[] dcfSharesB, true);
            //generate keys
            string[] dcfKeysA = new string[dcfGatesCount];
            string[] dcfKeysB = new string[dcfGatesCount];
            string[] dcfAesKeys = new string[dcfGatesCount];

            GenerateDcfKeys(dcfMasks, dcfKeysA, dcfKeysB, dcfAesKeys);

            //dpf
            //create masks and shares
            uint[] dpfMasks = RandomUtils.CreateRandomMasks(dpfMasksCount);
            RandomUtils.SplitToSecretShares(dpfMasks, out uint[] dpfSharesA, out uint[] dpfSharesB, true);

            //generate keys
            string[] dpfKeysA = new string[dpfGatesCount];
            string[] dpfKeysB = new string[dpfGatesCount];
            string[] dpfAesKeys = new string[dpfGatesCount];

            GenerateDpfKeys(dpfMasks, outputMasks: dcfMasks, dpfKeysA, dpfKeysB, dpfAesKeys);

            // send to servers
            //connect
            communicationA.Connect(ip_1, port_1);
            communicationB.Connect(ip_2, port_2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send 
            communicationA.SendMasksAndKeys(n, dcfSharesA, dcfKeysA, dcfAesKeys, dpfSharesA, dpfKeysA, dpfAesKeys);
            communicationB.SendMasksAndKeys(n, dcfSharesB, dcfKeysB, dcfAesKeys, dpfSharesB, dpfKeysB, dpfAesKeys);
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

        public static void GenerateDcfKeys(uint[] masks, string[] keysA, string[] keysB, string[] aesKeys)
        {
            int keyIndex;
            for(int i = 0; i < n; i++)
            {
                for (int j = i+1; j < n; j++)
                {
                    dcfAdapter.GenerateDCF(masks[i]-masks[j], out string keyA, out string keyB, out string aesKey); // mask1-mask2
                    keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1; // calculate the index for keyij -> key for the gate with input with mask i and j
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                    aesKeys[keyIndex] = aesKey;
                }
            }
        }

        public static void GenerateDpfKeys(uint[] masks, uint[] outputMasks, string[] keysA, string[] keysB, string[] aesKeys)
        {
            for (int i = 0; i < n; i++)
            {
                dpfAdapter.GenerateDPF(masks[i], 0 - outputMasks[i], out string keyA, out string keyB, out string aesKey);
                keysA[i] = keyA;
                keysB[i] = keyB;
                aesKeys[i] = aesKey;
            }
        }
    }
}
