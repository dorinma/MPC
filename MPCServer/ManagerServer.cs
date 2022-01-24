using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class ManagerServer
    {
        Dictionary<LogicCircuit.Types.CIRCUIT_TYPE, LogicCircuit.Circuit> circuits;
        static Computer computer = new Computer();
        static bool isDebugMode = false;

        static void Main(string[] args)
        {
            List<UInt16> values = new List<UInt16>();
            Communication comm = new Communication(values, 1, 1);            
            values = comm.StartServer();
            //if (isDebugMode)
            for (int i = 0; i < values.Count; i++) Console.WriteLine(values.ElementAt(i));
            Console.WriteLine("here1");
            computer.SetData(values);
            Console.WriteLine("end compute");
            List<UInt16> res = Compute(LogicCircuit.Types.CIRCUIT_TYPE.SORT_UINT16);
            string msg = "Computation is done :)"; //todo if exception send another msg
            comm.SendStr(msg, "DataClient");
            if (isDebugMode)
            {
                for (int i = 0; i < res.Count; i++) Console.WriteLine(res.ElementAt(i));
                comm.SendData(res, "DataClient");
            }
        }

        public void ReceiveRandomness(LogicCircuit.Types.CIRCUIT_TYPE pOperation, LogicCircuit.Circuit pCircuit) 
        {
            //Update circuits dictonry 
        }

        public static List<UInt16> Compute(LogicCircuit.Types.CIRCUIT_TYPE pOperation) 
        {
            //swich case per operation 
            LogicCircuit.Circuit c = new LogicCircuit.SortCircuit();
            List<UInt16> res = computer.Compute(c);
            return res;
        }

        public void SendResult() { }

        public List<UInt16> SumOutputs() 
        {
            return null;
        }
    }
}
