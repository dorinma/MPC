using MPCTools;
using MPCTools.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;

namespace MPCServer
{
    public class ManagerServer
    {
        static bool isDebugMode = true;
        static uint[] values;
        static CommunicationServer comm = new CommunicationServer();
        static byte instance;

        public static void Main(string[] args)
        {
            Console.WriteLine("Insert server instance:");
            Console.WriteLine("1. A");
            Console.WriteLine("2. B");
            int choose;
            while (!int.TryParse(Console.ReadLine(), out choose) || (choose != 1 && choose != 2))
            {
                Console.WriteLine("Invalid option.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(0);
                }
            }
            instance = choose == 1 ? (byte)0 : (byte)1;

            string memberServerIP = args[0];
            int memberServerPort = instance == 0 ? 2023 : instance == 1 ? 2022 : 0;
            comm.setInstance(instance);

            if (instance == 0)
            {
                if(!comm.ConnectServers(memberServerIP, memberServerPort))
                {
                    Environment.Exit(-1);
                }
            }

            if(!comm.OpenSocket(instance == 0 ? 2022 : 2023))
            {
                Environment.Exit(-1);
            }

            while (true)
            {
                values = comm.StartServer();
                // if return null -> restart server
                if(values == null)
                {
                    Environment.Exit(-1);
                }

                if (isDebugMode)
                {
                    Console.WriteLine("Secret shares of input:");
                    for (int i = 0; i < values.Length; i++)
                    {
                        Console.WriteLine(i + ". " + values[i]);
                    }
                }
                uint[] res = Compute(OPERATION.E_OPER_SORT);

                // stop timer

                // clean randmoness used

                if (!isDebugMode)
                {
                    string msg = "Message: Computation completed successfully."; //TODO if exception send another msg
                    comm.SendOutputMessage(msg);
                    string fileName = System.IO.Directory.GetCurrentDirectory() + "\\output\\" + (instance == 0 ? "outA"  : "outB") + comm.sessionId + ".csv";
                    MPCFiles.writeToFile(res, fileName);
                }
                else // debug mode
                {
                    Console.WriteLine("\nSecret shares of output:");
                    Console.WriteLine("\nres:");

                    for (int i = 0; i < res.Length; i++)
                    {
                        Console.WriteLine("\t" + res[i] + "\t");
                    }

                    Console.WriteLine("");

                    comm.SendOutputData(res);
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
            Computer computer = new Computer(values, comm.sortRandomRequest, instance, comm, new DcfAdapterServer(), new DpfAdapterServer());
            //Future code
            //Computer computer = new Computer(values, comm.requeset[op]);
            uint[] res = computer.Compute(op);
            return res;
        }

        private static void deleteUsedMasksAndKeys(int numOfElement)
        {
            comm.sortRandomRequest.dpfMasks = comm.sortRandomRequest.dpfMasks.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dpfKeys = comm.sortRandomRequest.dpfKeys.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dpfAesKeys = comm.sortRandomRequest.dpfAesKeys.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dcfMasks = comm.sortRandomRequest.dcfMasks.Skip(numOfElement).ToArray();

            string[] oldDcfKeys = comm.sortRandomRequest.dcfKeys;
            string[] oldAesDcfKeys = comm.sortRandomRequest.dcfAesKeys;
            int oldDcfKeysSize = comm.sortRandomRequest.dcfKeys.Length;
            int[] isUsedIndex = new int[oldDcfKeysSize];
            int newDcfKeysSize = oldDcfKeysSize - (numOfElement * (numOfElement - 1) / 2); // num of comparisons
            string[] newDcfKeys = new string[newDcfKeysSize];
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
