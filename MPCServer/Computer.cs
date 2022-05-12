using MPCProtocol;
using MPCProtocol.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class Computer
    {
        private uint[] data;
        private SortRandomRequest sortRandomRequest;
        private string instance;
        private DCFAdapter dcfAdapter = new DCFAdapter();
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
            uint[] sumResults = new uint[numOfElement];

            int n = sortRandomRequest.n;
            string[] keys = sortRandomRequest.dcfKeys;
            string[] dcfAesKeys = sortRandomRequest.dcfAesKeys;
            uint[] diffValues = SumAerversShares(numOfElement);
           
            int valuesIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for(int j = i + 1; j < numOfElement; j++)
                {
                    int keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1;
                    uint outputShare = dcfAdapter.EvalDCF(instance, keys[keyIndex], dcfAesKeys[keyIndex], diffValues[valuesIndex]); // if values[i] < values[j] returened 1
                    sumResults[i] -= instance == "A" ? outputShare : (outputShare - 1);
                    sumResults[j] += outputShare;
                    valuesIndex++;
                }
            }

            Console.WriteLine("shock but success");
         
            // third level - compare eatch value result to all possible indexes and placing in the returned list

            if (data.Length == 1)
                return data;
            else
                return data;
        }

        private uint[] SumEachPairValues(uint[] diffValues, int numOfElement)
        {
            uint[] masks = sortRandomRequest.dcfMasks;
            int currIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    diffValues[currIndex] += data[i] + masks[i] - (data[j] + masks[j]);
                    currIndex++;
                }
            }
            return diffValues;
        }

        private uint[] SumAerversShares(int numOfElement)
        {
            int nChoose2 = (numOfElement - 1) * numOfElement / 2;
            uint[] diffValues = new uint[nChoose2];
            uint[] sharedData;

            if (instance == "A")
            {           
                diffValues = SumEachPairValues(diffValues, numOfElement);
                comm.SendServerData(diffValues);
                sharedData = comm.AReciveServerData();
            }
            else
            {
                diffValues = comm.BReciveServerData();
                sharedData = SumEachPairValues(diffValues, numOfElement);
                comm.SendServerData(diffValues); 
            }
            return sharedData;
        }

        public void SendMaskValues(uint pValue) { }

        public uint ReceiveMaskedValues() 
        {
            return 0;
        }

        public void SetData(uint[] pData)
        {
            data = pData;
        }

    }
}
