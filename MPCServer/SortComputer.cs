using MPCTools.Requests;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    public class SortComputer : Computer
    {
        public SortComputer(uint[] values, RandomRequest randomRequest, byte instance, CommunicationServer comm, IDcfAdapterServer dcfAdapter, IDpfAdapterServer dpfAdapter, ILogger logger) 
            : base(values, randomRequest, instance, comm, dcfAdapter, dpfAdapter, logger)
        {
        }

        public override uint[] Compute()
        {
            //actually logic

            // first level - dcf between each pair values
            int numOfElement = data.Length;
            int n = randomRequest.n;

            // todo check n > numofelement

            uint[] sumValuesMasks = SumServersPartsWithMasks(numOfElement, data, randomRequest.dcfMasks);

            uint[] diffValues = DiffEachPairValues(sumValuesMasks, numOfElement);

            uint[] sharesIndexes = ComputeIndexesShares(diffValues, numOfElement, n);

            logger.Debug("Indexes shares:");
            logger.Debug(string.Join(", ", sharesIndexes));

            // second level - sum results
            uint[] sumIndexesMasks = SumServersPartsWithMasks(sharesIndexes.Length, sharesIndexes, randomRequest.dpfMasks);

            logger.Debug("Masked indexes:");
            logger.Debug(string.Join(", ", sumIndexesMasks));

            // third level - compare eatch value result to all possible indexes and placing in the returned list
            uint[] sortList = ComputeResultsShares(sumIndexesMasks, sumValuesMasks, numOfElement);

            return sortList;
        }

        public uint[] ComputeIndexesShares(uint[] diffValues, int numOfElement, int n)
        {
            uint[] sharesIndexes = new uint[numOfElement];
            string[] dcfKeys = randomRequest.dcfKeys;
            string[] dcfAesKeys = randomRequest.dcfAesKeys;
            int valuesIndex = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    int keyIndex = (2 * n - i - 1) * i / 2 + j - i - 1;
                    uint outputShare = dcfAdapter.EvalDCF(instance, dcfKeys[keyIndex], dcfAesKeys[keyIndex], diffValues[valuesIndex]); // return 1 if values[i] < values[j] otherxise 0
                    sharesIndexes[i] -= instance == 0 ? outputShare : (outputShare - 1);
                    sharesIndexes[j] += outputShare;
                    valuesIndex++;
                }
            }

            return sharesIndexes;
        }

        public uint[] ComputeResultsShares(uint[] sumIndexesMasks, uint[] sumValuesMasks, int numOfElement)
        {
            uint[] sortList = new uint[numOfElement];
            string[] dpfKeys = randomRequest.dpfKeys;
            string[] dpfAesKeys = randomRequest.dpfAesKeys;
            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = 0; j < numOfElement; j++)
                {
                    sortList[j] += dpfAdapter.EvalDPF(instance, dpfKeys[i], dpfAesKeys[i], sumIndexesMasks[i] - (uint)j, sumValuesMasks[i]);
                }
            }
            return sortList;
        }

    }
}
