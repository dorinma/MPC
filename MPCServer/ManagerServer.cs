using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class Manager
    {
        Dictionary<LogicCircuit.Types.CIRCUIT_TYPE, LogicCircuit.Circuit> circuits;
        static Computer computer;
        bool isDebugMode;

        static void Main(string[] args)
        {
            List<UInt16> values = new List<UInt16>();
            Communication comm = new Communication(values, 2, 10);
            values = comm.StartServer();
            for (int i = 0; i < values.Count; i++) Console.WriteLine(values.ElementAt(i));
            computer.data = values;
        }

        public void ReceiveRandomness(LogicCircuit.Types.CIRCUIT_TYPE pOperation, LogicCircuit.Circuit pCircuit) { }

        public void Compute(LogicCircuit.Types.CIRCUIT_TYPE pOperation, List<UInt16> pData) { }

        public void SendResult() { }

        public void SumOutputs() { }
    }
}
