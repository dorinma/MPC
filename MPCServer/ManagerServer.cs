using MPCProtocol;
using MPCProtocol.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    public class ManagerServer
    {
        static bool isDebugMode = true;
        static uint[] values;
        static CommunicationServer comm = new CommunicationServer();
        static string instance;

        public static void Main(string[] args)
        {
            instance = args[0];

            string memberServerIP = args[1];
            int memberServerPort = instance == "A" ? 2023 : instance == "B" ? 2022 : 0;

            comm.setInstance(instance);
            comm.ConnectServers(memberServerIP, memberServerPort);
            comm.OpenSocket();

            while (true)
            {
                values = comm.StartServer();
                // if return null -> restart server
                if (isDebugMode)
                {
                    Console.WriteLine("[DEBUG] Secret shares of input:");
                    for (int i = 0; i < values.Length; i++) Console.Write("\t" + values[i] + "\t");
                    Console.WriteLine("");
                }
                uint[] res = Compute(OPERATION.E_OPER_SORT);

                if (!isDebugMode)
                {
                    string msg = "Message: Computation completed successfully."; //TODO if exception send another msg
                    comm.SendOutputMessage(msg);
                }
                else // debug mode
                {
                    Console.WriteLine("\n[DEBUG] Secret shares of output:");
                    Console.WriteLine("\nres:");

                    for (int i = 0; i < res.Length; i++)
                    {
                        Console.WriteLine("\t" + res[i] + "\t");
                    }

                    Console.WriteLine("\nvalues:");

                    for (int i = 0; i < values.Length; i++)
                    {
                        Console.WriteLine("\t" + values[i] + "\t");
                    }


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

        public static uint[] Compute(OPERATION op) 
        {
            //swich case per operation 
            //LogicCircuit.Circuit c = new LogicCircuit.SortCircuit();
            Computer computer = new Computer(values, comm.sortRandomRequest, instance, comm);
            //Future code
            //Computer computer = new Computer(values, comm.requeset[op]);
            uint[] res = computer.Compute(op);
            return res;
        }

        public void SendResult() { }

        public uint[] SumOutputs() 
        {
            return null;
        }
    }
}
