using MPCProtocol;
using MPCProtocol.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MPCServer
{
        class Computer
    {
        private ulong[] data;
        private SortRandomRequest sortRandomRequest;
        private string instance;
        private DCFAdapter dcfAdapter = new DCFAdapter();
        private DPFAdapter dpfAdapter = new DPFAdapter();
        private CommunicationServer comm;

        public Computer(ulong[] values, SortRandomRequest sortRandomRequest, string instance, CommunicationServer comm)
        {
            data = values;
            this.sortRandomRequest = sortRandomRequest;
            this.instance = instance;
            this.comm = comm;
        }

        public ulong[] Compute(OPERATION op) 
        {
            switch (op)
            {
                case OPERATION.E_OPER_SORT:
                    {
                        return sortCompute();
                    }
            }

            return null;
        } 

        private ulong[] sortCompute()
        {
            //actually logic

            // first level - dcf between each pair values
            // second level - sum results

            int numOfElement = data.Length;
            ulong[] sharesIndexes = new ulong[numOfElement];

            int n = sortRandomRequest.n;
            ulong[] dcfKeys = sortRandomRequest.dcfKeys;
            ulong[] sumValuesMasks = SumServersShares(numOfElement);

            ulong[] diffValues = SumEachPairValues(sumValuesMasks, numOfElement);

            int valuesIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    int keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1;
                    ulong outputShare = dcfAdapter.Eval(instance, dcfKeys[keyIndex], diffValues[valuesIndex]); // if values[i] < values[j] returened 1
                    sharesIndexes[i] -= instance == "A" ? outputShare : (outputShare - 1);
                    sharesIndexes[j] += outputShare;
                    valuesIndex++;
                }
            }

            //DEBUG
            Console.WriteLine("Sum Results");
            for (int i = 0; i < sharesIndexes.Length; i++)
            {
                Console.WriteLine("\t" + sharesIndexes[i] + "\t");
            }

            // third level - compare eatch value result to all possible indexes and placing in the returned list
            ulong[] sortList = new ulong[numOfElement];
            ulong[] dpfKeys = sortRandomRequest.dpfKeys;
            ulong[] sumIndexesMasks = sumIndexesWithMasks(sharesIndexes);

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = 0; j < numOfElement; j++)
                {
                    sortList[j] += dpfAdapter.Eval(instance, dpfKeys[i], sumIndexesMasks[i] - (ulong)j, sumValuesMasks[i]);
                }
            }
            return sortList;
        }

        private ulong[] SumEachPairValues(ulong[] sumValuesMasks, int numOfElement)
        {
            int nChoose2 = (numOfElement - 1) * numOfElement / 2;
            ulong[] diffValues = new ulong[nChoose2];

            int currIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    diffValues[currIndex] += (sumValuesMasks[i] - sumValuesMasks[j]);
                    currIndex++;
                }
            }
            return diffValues;
        }

        private ulong[] SumEachValueWithMask(ulong[] sumValuesMasks)
        {
            ulong[] masks = sortRandomRequest.dcfMasks;

            for (int i = 0; i < sumValuesMasks.Length; i++)
            {
                sumValuesMasks[i] += data[i] + masks[i];
            }
            return sumValuesMasks;
        }

        private ulong[] SumServersShares(int numOfElement)
        {
            ulong[] sumValuesMasks = new ulong[numOfElement];
            ulong[] x = new ulong[numOfElement];

            if (instance == "A")
            {
                x = SumEachValueWithMask(x);
                comm.SendServerData(x);
                sumValuesMasks = comm.AReciveServerData();
            }
            else
            {
                x = comm.BReciveServerData();
                sumValuesMasks = SumEachValueWithMask(x);
                comm.SendServerData(sumValuesMasks);
            }

            return sumValuesMasks;
        }

        private ulong[] sumIndexesWithMasks(ulong[] sharesIndexes)
        {
            ulong[] totalSumIndexesWithMasks = new ulong[sharesIndexes.Length];

            if (instance == "A")
            {
                ulong[] sumIndexesWithMasks = sumIndexesMasks(sharesIndexes);
                comm.SendServerData(sumIndexesWithMasks);
                totalSumIndexesWithMasks = comm.AReciveServerData();
            }
            else
            {
                totalSumIndexesWithMasks = comm.BReciveServerData();
                totalSumIndexesWithMasks = sumIndexesMasks(totalSumIndexesWithMasks);
                comm.SendServerData(totalSumIndexesWithMasks);
            }

            return totalSumIndexesWithMasks;
        }

        private ulong[] sumIndexesMasks(ulong[] sharesIndexes)
        {
            ulong[] sharesWithMasks = new ulong[sharesIndexes.Length];

            for (int i = 0; i < sharesIndexes.Length; i++)
            {
                sharesWithMasks[i] += sharesWithMasks[i] + sortRandomRequest.dpfMasks[i];
            }
            return sharesWithMasks;
        }

    }
}
