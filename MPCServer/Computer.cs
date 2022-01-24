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

        public void Compute(LogicCircuit.Circuit pCircuit) { }

        public void SendMaskValues(UInt16 pValue) { }

        public void ReceiveMaskedValues() { }

    }
}
