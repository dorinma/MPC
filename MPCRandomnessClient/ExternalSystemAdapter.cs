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

        private void GenerateDCF(UInt16 maskDiff) 
        {
            //Call DCF with lambda, maskDiff, beta, inputRange
        }
        private void GenerateDPF(UInt16 maskDiff) 
        {
            //Call DPF with lambda, maskDiff, beta, inputRange
        }

    }
}
