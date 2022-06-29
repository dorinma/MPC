using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPCTools.Requests;
using MPCTools;

namespace MPCRandomnessClient
{
    public class SortCircuit : Circuit
    {
        public SortCircuit(int n): base(n)
        {
        }

        public override int GetDcfMasksCount(int n) {
            // mask for each input element
            return n; 
        }

        public override int GetDpfMasksCount(int n) {
            // mask for each element's index sum 
            return n;
        }

        public override int GetDcfGatesCount(int n) {
            // first layer (dcf gates) - n choose 2.
            return n * (n - 1) / 2;
        }

        public override int GetDpfGatesCount(int n) {
            // dpf gate for each index - n 
            return n;
        }

        public override void GenerateDcfKeys(uint[] masks, RandomRequest requestA, RandomRequest requestB)
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
        public override void GenerateDpfKeys(uint[] masks, uint[] outputMasks, RandomRequest requestA, RandomRequest requestB)
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

    }
}
