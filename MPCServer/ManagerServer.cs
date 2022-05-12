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
            Console.WriteLine("Insert server instance:");
            Console.WriteLine("1. A");
            Console.WriteLine("2. B");
            int choose;
            while (!int.TryParse(Console.ReadLine(), out choose) || (choose != 1 && choose != 2))
            {
                Console.WriteLine("Invalid action.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }
                Console.Write("Number of action: ");
            }
            instance = choose == 1 ? "A" : "B";

            string memberServerIP = args[0];
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

                // stop timer

                //clean randmones used

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

                deleteUsedMasksAndKeys(values.Length);
                comm.RestartServer();

                // future code 
                // matching timer to change server state to OFFLINE and turn on randomness client (for send randomness) 
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

        private static void deleteUsedMasksAndKeys(int numOfElement)
        {
            comm.sortRandomRequest.dpfMasks = comm.sortRandomRequest.dpfMasks.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dpfKeys = comm.sortRandomRequest.dpfKeys.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dcfMasks = comm.sortRandomRequest.dcfMasks.Skip(numOfElement).ToArray();

            ulong[] oldDcfKeys = comm.sortRandomRequest.dpfKeys;
            int oldDcfKeysSize = comm.sortRandomRequest.dcfKeys.Length;
            int[] isUsedIndex = new int[oldDcfKeysSize];
            int newDcfKeysSize = oldDcfKeysSize - (numOfElement * (numOfElement - 1) / 2); // num of comparisons
            ulong[] newDcfKeys = new ulong[newDcfKeysSize];
            int index = 0;

            for (int i = 0; i < numOfElement; i++)
            {
                for (int j = i + 1; j < numOfElement; j++)
                {
                    int keyIndex = (2 * comm.sortRandomRequest.n - i - 1) * i / 2 + j - i - 1;
                    isUsedIndex[keyIndex] = -1;
                }
            }

            for (int k = 0; k < oldDcfKeysSize; k++)
            {
                if (isUsedIndex[k] != -1)
                {
                    newDcfKeys[index] = oldDcfKeys[k];
                    index++;
                }
            }
            comm.sortRandomRequest.dcfKeys = newDcfKeys;

            comm.sortRandomRequest.n = comm.sortRandomRequest.n - numOfElement;
        }

        public void SendResult() { }

        public uint[] SumOutputs() 
        {
            return null;
        }
    }
}
