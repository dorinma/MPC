namespace MPCServer
{
    using MPCTools;
    using MPCTools.Requests;
    using System;
    using NLog;
    using System.Diagnostics;

    public class Computer
    {
        private uint[] data;
        private SortRandomRequest sortRandomRequest;
        private byte instance;
        private IDcfAdapterServer dcfAdapter;
        private IDpfAdapterServer dpfAdapter;
        private CommunicationServer comm;
        private ILogger logger;

        private long communicationBytesCounter;
        private long memoryBytesCounter;


        public Computer(uint[] values, SortRandomRequest sortRandomRequest, byte instance,
            CommunicationServer comm, IDcfAdapterServer dcfAdapter, IDpfAdapterServer dpfAdapter, ILogger logger)
        {
            data = values;
            this.sortRandomRequest = sortRandomRequest;
            this.instance = instance;
            this.comm = comm;
            this.dcfAdapter = dcfAdapter;
            this.dpfAdapter = dpfAdapter;
            this.logger = logger;
            communicationBytesCounter = 0;
            memoryBytesCounter = 0;
        }

        public uint[] Compute(OPERATION op)
        {
            uint[] result = null;
            memoryBytesCounter = data.Length * 660 + (data.Length * (data.Length - 1) / 2) * 900;
            var watch = Stopwatch.StartNew();
            
            switch (op)
            {
                case OPERATION.SORT:
                    {
                        result = sortCompute();
                        break;
                    }
            }

            watch.Stop();

            var elapsedMs = watch.ElapsedMilliseconds;
            logger.Trace($"Operation {op} on {data.Length} elements.");
            logger.Trace($"Runtime {elapsedMs} ms");
            logger.Trace($"Memory consumption {memoryBytesCounter} bytes");
            logger.Trace($"communication sent {communicationBytesCounter} bytes");
            return result;
        }

        public uint[] sortCompute()
        {

            long totalMemoryBefore = GC.GetTotalMemory(true);

            // first level - dcf between each pair values
            int numOfElement = data.Length;
            int n = sortRandomRequest.n;

            // todo check n > numofelement

            uint[] sumValuesMasks = SumServersPartsWithMasks(numOfElement, data, sortRandomRequest.dcfMasks);

            uint[] diffValues = DiffEachPairValues(sumValuesMasks, numOfElement);

            uint[] sharesIndexes = ComputeIndexesShares(diffValues, numOfElement, n);

            logger.Debug("Indexes shares:");
            logger.Debug(string.Join(", ", sharesIndexes));

            // second level - sum results
            uint[] sumIndexesMasks = SumServersPartsWithMasks(sharesIndexes.Length, sharesIndexes, sortRandomRequest.dpfMasks);

            logger.Debug("Masked indexes:");
            logger.Debug(string.Join(", ", sumIndexesMasks));

            // third level - compare eatch value result to all possible indexes and placing in the returned list
            uint[] sortList = ComputeResultsShares(sumIndexesMasks, sumValuesMasks, numOfElement);

            long totalMemoryAfter = GC.GetTotalMemory(true);
            memoryBytesCounter += totalMemoryAfter - totalMemoryBefore;
            
            return sortList;
        }

        public uint[] ComputeResultsShares(uint[] sumIndexesMasks, uint[] sumValuesMasks, int numOfElement)
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
                    int keyIndex = ((2 * n - i - 1) * i / 2 + j - i - 1) * 2;
                    uint outputShare1 = dcfAdapter.EvalDCF(instance, dcfKeys[keyIndex], dcfAesKeys[keyIndex], diffValues[valuesIndex]); // return 1 if values[i] < values[j] otherxise 0
                    uint outputShare2 = dcfAdapter.EvalDCF(instance, dcfKeys[keyIndex + 1], dcfAesKeys[keyIndex + 1], diffValues[valuesIndex]); // return 1 if values[i] < values[j] otherxise 0
                    uint outputShare = outputShare1 - outputShare2;
                    sharesIndexes[i] -= instance == 0 ? outputShare : (outputShare - 1);
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

            if (instance == 0)
            {
                uint[] maskedSum = new uint[numOfElement]; //init with 0
                SumEachSharesWithMask(maskedSum, partServer, masks);
                comm.SendServerData(maskedSum);
                totalMaskedSum = comm.ReceiveServerData();
            }
            else
            {
                totalMaskedSum = comm.ReceiveServerData();
                SumEachSharesWithMask(totalMaskedSum, partServer, masks);
                comm.SendServerData(totalMaskedSum);
            }

            communicationBytesCounter += numOfElement * sizeof(uint);

            return totalMaskedSum;
        }

    }
}
