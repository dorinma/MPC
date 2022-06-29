using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPCTools.Requests;

namespace MPCRandomnessClient
{
    public abstract class Circuit
    {
        public int n;
        public int dcfMasksCount; // mask for each input element
        public int dpfMasksCount; // mask for each element's index sum 
        public int dcfGatesCount; // first layer (dcf gates) - n choose 2.
        public int dpfGatesCount; // dpf gate for each index - n 

        protected static DcfAdapterRandClient dcfAdapter = new DcfAdapterRandClient();
        protected static DpfAdapterRandClient dpfAdapter = new DpfAdapterRandClient();

        public Circuit(int n)
        {
            this.n = n;
            dcfMasksCount = GetDcfMasksCount(n);
            dpfMasksCount = GetDpfMasksCount(n);
            dcfGatesCount = GetDcfGatesCount(n);
            dpfGatesCount = GetDpfGatesCount(n);

        }

        public abstract int GetDcfMasksCount(int n);
        public abstract int GetDpfMasksCount(int n);
        public abstract int GetDcfGatesCount(int n);
        public abstract int GetDpfGatesCount(int n);

        public abstract void GenerateDcfKeys(uint[] masks, RandomRequest requestA, RandomRequest requestB);

        public abstract void GenerateDpfKeys(uint[] masks, uint[] outputMasks, RandomRequest requestA, RandomRequest requestB);
    }
}
