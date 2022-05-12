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
        int inputRange;

        public ExternalSystemAdapter(int pLambda, int pInputRange) 
        {
            lambda = pLambda;
            inputRange = pInputRange;
        }

        public void GenerateDCF(ulong masksDiff, ulong beta, out ulong key1, out ulong key2) 
        {
            //Call DPF with lambda, maskDiff, beta, inputRange
            key1 = 1;
            key2 = 2;
        }

        public void GenerateDPF(ulong maskedFixedPoint, ulong beta, out ulong key1, out ulong key2)
        {
            //Call DPF with lambda, maskDiff, beta, inputRange
            key1 = 3;
            key2 = 4;
        }

    }
}
