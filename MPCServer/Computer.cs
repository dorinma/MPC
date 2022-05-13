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
        private uint[] data;
        private SortRandomRequest sortRandomRequest;
        private string instance;
        private DcfAdapterServer dcfAdapter = new DcfAdapterServer();
        private DpfAdapterServer dpfAdapter = new DpfAdapterServer();
        private CommunicationServer comm;

        public Computer(uint[] values, SortRandomRequest sortRandomRequest, string instance, CommunicationServer comm)
        {
            data = values;
            this.sortRandomRequest = sortRandomRequest;
            this.instance = instance;
            this.comm = comm;
        }

        public uint[] Compute(OPERATION op) 
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

        private uint[] sortCompute()
        {
            //actually logic

            // first level - dcf between each pair values
            // second level - sum results

            int numOfElement = data.Length;
            uint[] sharesIndexes = new uint[numOfElement];

            int n = sortRandomRequest.n;
            string[] dcfKeys = sortRandomRequest.dcfKeys;
            string[] dcfAesKeys = sortRandomRequest.dcfAesKeys;
            uint[] sumValuesMasks = SumServersShares(numOfElement);

            uint[] diffValues = SumEachPairValues(sumValuesMasks, numOfElement);

            int valuesIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    int keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1;
                    uint outputShare = dcfAdapter.EvalDCF(instance, dcfKeys[keyIndex], dcfAesKeys[keyIndex], diffValues[valuesIndex]); // if values[i] < values[j] returened 1
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
            uint[] sortList = new uint[numOfElement];
            string[] dpfKeys = sortRandomRequest.dpfKeys;
            string[] dpfAesKeys = sortRandomRequest.dpfAesKeys;
            uint[] sumIndexesMasks = sumIndexesWithMasks(sharesIndexes);

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = 0; j < numOfElement; j++)
                {
                    sortList[j] += dpfAdapter.Eval(instance, dpfKeys[i], dpfAesKeys[i], sumIndexesMasks[i] - (uint)j, sumValuesMasks[i]);
                }
            }
            return sortList;
        }

        private uint[] SumEachPairValues(uint[] sumValuesMasks, int numOfElement)
        {
            int nChoose2 = (numOfElement - 1) * numOfElement / 2;
            uint[] diffValues = new uint[nChoose2];

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

        private uint[] SumEachValueWithMask(uint[] sumValuesMasks)
        {
            //uint[] sumValuesMasks = new uint[numOfElement];
            uint[] masks = sortRandomRequest.dcfMasks;

            for (int i = 0; i < sumValuesMasks.Length; i++)
            {
                sumValuesMasks[i] += data[i] + masks[i];
            }
            return sumValuesMasks;
        }

        private uint[] SumServersShares(int numOfElement)
        {
            uint[] totalsumValuesMasks = new uint[numOfElement];
            uint[] sumValuesMasks = new uint[numOfElement];

            if (instance == "A")
            {
                uint[] sumValuesMasksA = SumEachValueWithMask(sumValuesMasks);
                comm.SendServerData(sumValuesMasksA);
                totalsumValuesMasks = comm.AReciveServerData();
            }
            else
            {
                uint[] sumValuesMasksB = comm.BReciveServerData();
                totalsumValuesMasks = SumEachValueWithMask(sumValuesMasksB);
                comm.SendServerData(totalsumValuesMasks);
            }

            return totalsumValuesMasks;
        }

        private uint[] sumIndexesWithMasks(uint[] sharesIndexes)
        {
            uint[] totalSumIndexesWithMasks = new uint[sharesIndexes.Length];

            if (instance == "A")
            {
                uint[] sumIndexesWithMasks = sumIndexesMasks(sharesIndexes);
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

        private uint[] sumIndexesMasks(uint[] sharesIndexes)
        {
            uint[] sharesWithMasks = new uint[sharesIndexes.Length];

            for (int i = 0; i < sharesIndexes.Length; i++)
            {
                sharesWithMasks[i] += sharesWithMasks[i] + sortRandomRequest.dpfMasks[i];
            }
            return sharesWithMasks;
        }

    }
}
