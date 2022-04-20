using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections;

namespace MPCServer
{
    class DPFAdapter
    {
        public UInt16 Eval(int serverIndex, UInt16 key, UInt16 inputSum, UInt16 maskedOrignalInput)
        {
            return 0;
        }

        [DllImport("./ExtLibs/libdistributed_point_function.so", EntryPoint = "GenerateKeys")]
        private static extern void GenerateKeys(UInt64 alpha, UInt64 beta); 
        //1. Needs to be 128bit, check if 64 works
        //2. There are 2 more GenerateKeys with different parameters
        //3. Return type could be absl::Error (C++), what to do if an error is returned?

        [DllImport("./ExtLibs/libdistributed_point_function.so", EntryPoint = "EvaluateAt")]
        private static extern void EvaluateAt();
            //TODO create structs to match the parameters in the C++ proj:
            //(const DpfKey& key, int hierarchy_level, absl::Span<const absl::uint128> evaluation_points) 

    }
}
