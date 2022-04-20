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
        public const int n = 100; // n
        public const int dcfMasksCount = n; // mask for each input element
        public const int dpfMasksCount = n; // mask for each element's index sum 
        public const int dcfGatesCount = n*(n-1)/2; // first layer (dcf gates) - n choose 2.
        public const int dpfGatesCount = n*n; // first layer (dcf gates) - last layer (dpf gates) - n*n 

        private static ExternalSystemAdapter externalSystemAdapter = new ExternalSystemAdapter(0, 0, 0);

        private static CommunicationRandClient communicationA;
        private static CommunicationRandClient communicationB;
        private static string ip1 = "127.0.0.1";
        private static string ip2 = "127.0.0.1";
        private static int port1 = 2022;
        private static int port2 = 2023;

        public static void Main(string[] args)
        {
            //while with timer
            //sort
            CreateAndSendCircuits();
            //other circuits..
        }

        private static void CreateAndSendCircuits()
        {
            //dcf
            //create masks and shares
            ulong[] dcfMasks = CreateRandomMasks(dcfMasksCount);
            ulong[] dcfSharesA = new ulong[dcfMasksCount];
            ulong[] dcfSharesB = new ulong[dcfMasksCount];
            Randomness.SplitToSecretShares(dcfMasks, dcfSharesA, dcfSharesB);
            //generate keys
            ulong[] dcfKeysA = new ulong[dcfGatesCount];
            ulong[] dcfKeysB = new ulong[dcfGatesCount];
            
            GenerateDcfKeys(dcfMasks, dcfKeysA, dcfKeysB);

            //dpf
            //create masks and shares
            ulong[] dpfMasks = CreateRandomMasks(dpfMasksCount);
            ulong[] dpfSharesA = new ulong[dpfMasksCount];
            ulong[] dpfSharesB = new ulong[dpfMasksCount];
            Randomness.SplitToSecretShares(dpfMasks, dpfSharesA, dpfSharesB);
            //generate keys
            ulong[] dpfKeysA = new ulong[dpfGatesCount];
            ulong[] dpfKeysB = new ulong[dpfGatesCount];

            GenerateDpfKeys(dpfMasks, dpfKeysA, dpfKeysB);

            // send to servers
            //connect
            communicationA = new CommunicationRandClient();
            communicationB = new CommunicationRandClient();
            /*communicationA.Connect(ip1, port1);
            communicationB.Connect(ip2, port2);
            communicationA.connectDone.WaitOne();
            communicationB.connectDone.WaitOne();*/
            //send (need to verify that both server recieved correctly)
            communicationA.SendMasksAndKeys(n, dcfMasks, dcfKeysA, dpfMasks, dpfKeysA);
        }

        private static ulong[] CreateRandomMasks(int count)
        {
            Random rnd = new Random();
            ulong[] masks = new ulong[count];
            for(int i = 0; i < count; i++)
            {
                masks[i] = rnd.NextUInt64();
            }
            return masks;
            /*return Enumerable
                .Repeat((ulong)default, masksCount)
                .Select(i => rnd.NextUInt64())
                .ToList();*/
        }

        private static void GenerateDcfKeys(ulong[] masks, ulong[] keysA, ulong[] keysB)
        {
            int keyIndex;
            for(int i = 0; i < n; i++)
            {
                for (int j = i+1; j < n; j++)
                {
                    externalSystemAdapter.GenerateDCF(masks[i]-masks[j], out ulong keyA, out ulong keyB); // mask1-mask2
                    keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1; // calculate the index for keyij -> key for the gate with input with mask i and j
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                }
            }
        }

        private static void GenerateDpfKeys(ulong[] masks, ulong[] keysA, ulong[] keysB)
        {
            int keyIndex;
            for (int i = 0; i < n; i++)
            {
                foreach (int index in Enumerable.Range(0, n))
                {
                    externalSystemAdapter.GenerateDPF((ulong)index + masks[i], out ulong keyA, out ulong keyB);
                    keyIndex = i * n + index;
                    keysA[keyIndex] = keyA;
                    keysB[keyIndex] = keyB;
                }
            }
        }
    }
}
