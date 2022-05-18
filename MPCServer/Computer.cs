using MPCTools;
using MPCTools.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MPCServer
{
    public class Computer
    {
        private uint[] data;
        private SortRandomRequest sortRandomRequest;
        private string instance;
        private IDcfAdapterServer dcfAdapter;
        private IDpfAdapterServer dpfAdapter;
        private CommunicationServer comm;


        public Computer(uint[] values, SortRandomRequest sortRandomRequest, string instance, CommunicationServer comm, IDcfAdapterServer dcfAdapter, IDpfAdapterServer dpfAdapter)
        {
            data = values;
            this.sortRandomRequest = sortRandomRequest;
            this.instance = instance;
            this.comm = comm;
            this.dcfAdapter = dcfAdapter;
            this.dpfAdapter = dpfAdapter;
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
            int numOfElement = data.Length;
            int n = sortRandomRequest.n;

            // todo check n > numofelement

            uint[] sumValuesMasks = SumServersPartsWithMasks(numOfElement, data, sortRandomRequest.dcfMasks);

            uint[] diffValues = DiffEachPairValues(sumValuesMasks, numOfElement);

            uint[] sharesIndexes = ComputeIndexesShares(diffValues, numOfElement, n);

            //DEBUG
            Console.WriteLine("shares Indexes");
            for (int i = 0; i < sharesIndexes.Length; i++)
            {
                Console.WriteLine("\t" + sharesIndexes[i] + "\t");
            }

            // second level - sum results
            uint[] sumIndexesMasks = SumServersPartsWithMasks(sharesIndexes.Length, sharesIndexes, sortRandomRequest.dpfMasks);

            //DEBUG
            Console.WriteLine("sum Indexes");
            for (int i = 0; i < sumIndexesMasks.Length; i++)
            {
                Console.WriteLine("\t" + sumIndexesMasks[i] + "\t");
            }

            // third level - compare eatch value result to all possible indexes and placing in the returned list
            uint[] sortList = ComputeResultsShares(sumIndexesMasks, sumValuesMasks, n);

            return sortList;
        }

        private uint[] ComputeResultsShares(uint[] sumIndexesMasks, uint[] sumValuesMasks, int numOfElement)
        {
            uint[] sortList = new uint[numOfElement];
            string[] dpfKeys = sortRandomRequest.dpfKeys;
            string[] dpfAesKeys = sortRandomRequest.dpfAesKeys;
            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = 0; j < numOfElement; j++)
                {
                    sortList[j] += dpfAdapter.EvalDPF(instance, dpfKeys[i], dpfAesKeys[i], sumIndexesMasks[i] - (uint)j, sumValuesMasks[i]);
                }
            }
            return sortList;
        }

        public uint[] ComputeIndexesShares(uint[] diffValues, int numOfElement, int n)
        {
            uint[] sharesIndexes = new uint[numOfElement];
            string[] dcfKeys = sortRandomRequest.dcfKeys;
            string[] dcfAesKeys = sortRandomRequest.dcfAesKeys;
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

            return sharesIndexes;
        }

        public uint[] DiffEachPairValues(uint[] sumValuesMasks, int numOfElement)
        {
            int numOfElementChoose2 = (numOfElement - 1) * numOfElement / 2;
            uint[] diffValues = new uint[numOfElementChoose2];

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

        public void SumEachSharesWithMask(uint[] sumSharesMasks, uint[] sharesValue, uint[] masks)
        {
            for (int i = 0; i < sumSharesMasks.Length; i++)
            {
                sumSharesMasks[i] += (sharesValue[i] + masks[i]);
            }
        }

        private uint[] SumServersPartsWithMasks(int numOfElement, uint[] partServer, uint[] masks) 
        {
            uint[] totalMaskedSum;

            if (instance == "A")
            {
                uint[] maskedSum = new uint[numOfElement]; //init with 0
                SumEachSharesWithMask(maskedSum, partServer, masks);
                comm.SendServerData(maskedSum);
                totalMaskedSum = comm.AReciveServerData();
            }
            else
            {
                totalMaskedSum = comm.BReciveServerData();
                SumEachSharesWithMask(totalMaskedSum, partServer, masks);
                comm.SendServerData(totalMaskedSum);
            }

            return totalMaskedSum;
        }

    }
}
