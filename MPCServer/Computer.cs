using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class Computer
    {
        private List<UInt16> data;

        public Computer()
        {
            data = new List<UInt16>();
        }

        public List<UInt16> Compute(LogicCircuit.Circuit pCircuit) 
        {
            if (data.Count == 1)
                return data;
            else 
                return null;
        }

        public void SendMaskValues(UInt16 pValue) { }

        public UInt16 ReceiveMaskedValues() 
        {
            return 0;
        }

        public void SetData(List<UInt16> pData)
        {
            data = pData;
        }

    }
}
