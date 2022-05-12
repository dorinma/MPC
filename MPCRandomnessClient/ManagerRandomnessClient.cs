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
        public const int n = 10; // n
        public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n*n; // first layer (dcf gates) - last layer (dpf gates) - n*n 

        private static ExternalSystemAdapter externalSystemAdapter = new ExternalSystemAdapter(0, 0);

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
            ulong[] dcfMasks = Randomness.CreateRandomMasks(dcfMasksCount);
            /*ulong[] dcfSharesA = new ulong[dcfMasksCount];
            ulong[] dcfSharesB = new ulong[dcfMasksCount];*/
            Randomness.SplitToSecretShares(dcfMasks, out ulong[] dcfSharesA, out ulong[] dcfSharesB);
            //generate keys
            ulong[] dcfKeysA = new ulong[dcfGatesCount];
            ulong[] dcfKeysB = new ulong[dcfGatesCount];

            
            GenerateDcfKeys(dcfMasks, dcfKeysA, dcfKeysB);

            //dpf
            //create masks and shares
            ulong[] dpfMasks = Randomness.CreateRandomMasks(dpfMasksCount);
            ulong[] mBetaDpf = Randomness.CreateRandomMasks(dpfMasksCount);
            /*ulong[] dpfSharesA = new ulong[dpfMasksCount];
            ulong[] dpfSharesB = new ulong[dpfMasksCount];*/
            Randomness.SplitToSecretShares(dpfMasks, out ulong[] dpfSharesA, out ulong[] dpfSharesB);
            Randomness.SplitToSecretShares(mBetaDpf, out ulong[] mBetaDpfA, out ulong[] mBetaDpfB);
            //generate keys
            ulong[] dpfKeysA = new ulong[dpfGatesCount];
            ulong[] dpfKeysB = new ulong[dpfGatesCount];

            GenerateDpfKeys(dpfMasks, mBetaDpf, dpfKeysA, dpfKeysB);

            // send to servers
            //connect
            communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();
            //send (need to verify that both server recieved correctly)
            communicationA.SendMasksAndKeys(n, dcfSharesA, dcfKeysA, dpfSharesA, mBetaDpfA, dpfKeysA);
            communicationB.SendMasksAndKeys(n, dcfSharesB, dcfKeysB, dpfSharesB, mBetaDpfA, dpfKeysB);
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

        public static void GenerateDcfKeys(ulong[] masks, ulong[] keysA, ulong[] keysB)
        {
            int keyIndex;
            for(int i = 0; i < n; i++)
            {
                for (int j = i+1; j < n; j++)
                { 
                    externalSystemAdapter.GenerateDCF(masks[i]-masks[j], 1, out ulong keyA, out ulong keyB); // mask1-mask2
                    keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1; // calculate the index for keyij -> key for the gate with input with mask i and j
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                }
            }
        }

        public static void GenerateDpfKeys(ulong[] masks, ulong[] mBetaDpf, ulong[] keysA, ulong[] keysB)
        {
            for (int i = 0; i < n; i++)
            {
                externalSystemAdapter.GenerateDPF(masks[i], 0 - mBetaDpf[i], out ulong keyA, out ulong keyB);
                keysA[i] = keyA;
                keysB[i] = keyB;
            }
        }
    }
}
