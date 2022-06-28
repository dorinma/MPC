namespace MPCServer
{
    using MPCTools;
    using MPCTools.Requests;
    using System;
    using NLog;

    public abstract class Computer
    {
        protected uint[] data;
        protected RandomRequest randomRequest;
        protected byte instance;
        protected IDcfAdapterServer dcfAdapter;
        protected IDpfAdapterServer dpfAdapter;
        protected CommunicationServer comm;
        protected ILogger logger;
        public long communicationBytesCounter;
        public long memoryBytesCounter;


        public Computer(uint[] values, RandomRequest randomRequest, byte instance,
            CommunicationServer comm, IDcfAdapterServer dcfAdapter, IDpfAdapterServer dpfAdapter, ILogger logger)
        {
            data = values;
            this.randomRequest = randomRequest;
            this.instance = instance;
            this.comm = comm;
            this.dcfAdapter = dcfAdapter;
            this.dpfAdapter = dpfAdapter;
            this.logger = logger;
            communicationBytesCounter = 0;
            memoryBytesCounter = data.Length * 660 + (data.Length * (data.Length - 1) / 2) * 900; ;
        }

        public abstract uint[] Compute();

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

        public uint[] SumServersPartsWithMasks(int numOfElement, uint[] partServer, uint[] masks) 
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
