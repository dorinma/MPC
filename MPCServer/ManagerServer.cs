using MPCProtocol;
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
        static bool isDebugMode = true;

        public static void Main(string[] args)
        {
            CommunicationServer2 comm = new CommunicationServer2();
            comm.OpenSocket();
            while (true)
            {
                List<UInt16> values = comm.StartServer();
                // if return null -> restart server
                if (isDebugMode)
                {
                    Console.WriteLine("[DEBUG] Secret shares of input:");
                    for (int i = 0; i < values.Count; i++) Console.Write("\t" + values.ElementAt(i) + "\t");
                    Console.WriteLine("");
                }
                computer.SetData(values);
                List<UInt16> res = Compute(LogicCircuit.Types.CIRCUIT_TYPE.SORT_UINT16);

                if (!isDebugMode)
                {
                    string msg = "Message: Computation completed successfully."; //TODO if exception send another msg
                    comm.SendOutputMessage(msg);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Secret shares of output:");
                    for (int i = 0; i < res.Count; i++) Console.WriteLine("\t" + res.ElementAt(i) + "\t");
                    Console.WriteLine("");
                    comm.SendOutputData(values);
                }

                comm.RestartServer();
                /*List<UInt16> values = comm.StartServer();
                // if return null -> restart server
                if (isDebugMode)
                {
                    Console.WriteLine("[DEBUG] Secret shares of input:");
                    for (int i = 0; i < values.Count; i++) Console.Write("\t" + values.ElementAt(i) + "\t");
                    Console.WriteLine("");
                }
                computer.SetData(values);
                List<UInt16> res = Compute(LogicCircuit.Types.CIRCUIT_TYPE.SORT_UINT16);

                if (!isDebugMode)
                {
                    string msg = "Message: Computation completed successfully."; //TODO if exception send another msg
                    comm.SendString(OPCODE_MPC.E_OPCODE_SERVER_MSG, msg, toClient: true);
                }
                else
                {
                    Console.WriteLine("[DEBUG] Secret shares of output:");
                    for (int i = 0; i < res.Count; i++) Console.WriteLine("\t" + res.ElementAt(i) + "\t");
                    Console.WriteLine("");
                    comm.SendData(OPCODE_MPC.E_OPCODE_SERVER_DATA, values);
                }

                comm.RestartServer();*/
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
