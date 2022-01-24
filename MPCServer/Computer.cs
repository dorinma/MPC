using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class Computer
    {
        public List<UInt16> data;

        public Computer()
        {
            data = new List<UInt16>();
        }

        public List<UInt16> Compute(LogicCircuit.Circuit pCircuit) 
        {
            return null;
        }

        public void SendMaskValues(UInt16 pValue) { }

        public UInt16 ReceiveMaskedValues() 
        {
            return 0;
        }

    }
}
