using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCRandomnessClient
{
    class ExternalSystemAdapter
    {
        int lambda;
        int beta;
        int inputRange;

        public ExternalSystemAdapter(int pLambda, int pBeta, int pInputRange) 
        {
            lambda = pLambda;
            beta = pBeta;
            inputRange = pInputRange;
        }

        public void GenerateDCF(ulong masksDiff, out ulong key1, out ulong key2) 
        {
            //Call DPF with lambda, maskDiff, beta, inputRange
            key1 = 1;
            key2 = 2;
        }

        public void GenerateDPF(ulong maskedFixedPoint, out ulong key1, out ulong key2)
        {
            //Call DPF with lambda, maskDiff, beta, inputRange
            key1 = 3;
            key2 = 4;
        }

    }
}
