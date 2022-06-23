﻿using MPCTools;
using System;
using System.Linq;
using NLog;
using System.IO;

namespace MPCServer
{
    public class ManagerServer
    {
        private static Logger logger;
        private static bool isDebugMode = true;
        private static uint[] values;
        private static CommunicationServer comm;
        private static byte instance;

        private const int RETRY_TIME = 10; // Minutes

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

            SetupLogger();
            comm = new CommunicationServer(logger);

            string memberServerIP = args[0];
            int memberServerPort = instance == 0 ? 2023 : instance == 1 ? 2022 : 0;
            comm.setInstance(instance);

            while (true)
            {
                Run(memberServerIP, memberServerPort);
                comm.RestartServer();
            }
        }

        private static void Run(string memberServerIP, int memberServerPort)
        {
            long startMinute = DateTimeOffset.Now.Minute;
            if (instance == 0)
            {
                bool connected = comm.ConnectServers(memberServerIP, memberServerPort);
                while (!connected && DateTimeOffset.Now.Minute - startMinute <= RETRY_TIME)
                {
                    connected = comm.ConnectServers(memberServerIP, memberServerPort);
                }
                if (!connected)
                {
                    logger.Error("Could not connect to other server.");
                    Environment.Exit(-1);
                }
            }

            if (!comm.OpenSocket(instance == 0 ? 2022 : 2023))
            {
                logger.Error("Could not create a socket between servers.");
                return;
            }

            values = comm.StartServer();

            if (values == null)
            {
                logger.Error("Variable \"values\" is null.");
                return;
            }

            logger.Debug("Input shares:");
            logger.Debug(string.Join(", ", values));

            uint[] res = Compute(comm.operation);

            if (res == null)
            {
                return;
            }

            logger.Debug("Output shares:");
            logger.Debug(string.Join(", ", res));

            string msg = "Computation completed successfully.";
            logger.Info(msg);

            string fileName = (instance == 0 ? "outA" : "outB") + "_" + comm.sessionId + ".csv";
            String fullPath = Path.Combine(@"..\\..\\..\\Results", fileName);
            MPCFiles.writeToFile(res, fullPath);

            if (isDebugMode)
            {
                comm.SendOutputToAllClients(res);
            }
            else
            {
                comm.SendMessageToAllClients(OPCODE_MPC.E_OPCODE_SERVER_MSG, msg);
            }

            // Clean used randmoness
            deleteUsedMasksAndKeys(values.Length);
        }

        private static void SetupLogger()
        {
            GlobalDiagnosticsContext.Set("serverInstance", instance == 0 ? "A" : "B");

            if (isDebugMode)
            {
                LogManager.Configuration.AddRuleForAllLevels("logconsole", loggerNamePattern: "*");
            }

            logger = LogManager.GetLogger("Server logger");
        }

        public static uint[] Compute(OPERATION op)
        {
            logger.Info($"Number of elements: {values.Length}.");
            if (values.Length > comm.sortRandomRequest.n)
            {
                string serverInstance = instance == 0 ? "A" : "B";
                string msg = $"Server {serverInstance} doesn't have enough correlated randomness to perform the computation.";
                logger.Error(msg);
                comm.SendMessageToAllClients(OPCODE_MPC.E_OPCODE_ERROR, msg);
                return null;
            }

            Computer computer = new Computer(values, comm.sortRandomRequest, instance,
                comm, new DcfAdapterServer(), new DpfAdapterServer(), logger);

            return computer.Compute(op);
        }

        private static void deleteUsedMasksAndKeys(int numOfElement)
        {
            int n = comm.sortRandomRequest.n;
            int dcfKeysToSkip = (2 * n - 1 - numOfElement) * numOfElement / 2;

            comm.sortRandomRequest.n = comm.sortRandomRequest.n - numOfElement;

            comm.sortRandomRequest.dcfMasks = comm.sortRandomRequest.dcfMasks.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dcfKeysSmallerLowerBound = comm.sortRandomRequest.dcfKeysSmallerLowerBound.Skip(dcfKeysToSkip).ToArray();
            comm.sortRandomRequest.dcfKeysSmallerUpperBound = comm.sortRandomRequest.dcfKeysSmallerUpperBound.Skip(dcfKeysToSkip).ToArray();
            comm.sortRandomRequest.shares01 = comm.sortRandomRequest.shares01.Skip(dcfKeysToSkip).ToArray();
            comm.sortRandomRequest.dcfAesKeysLower = comm.sortRandomRequest.dcfAesKeysLower.Skip(dcfKeysToSkip).ToArray();
            comm.sortRandomRequest.dcfAesKeysUpper = comm.sortRandomRequest.dcfAesKeysUpper.Skip(dcfKeysToSkip).ToArray();

            comm.sortRandomRequest.dpfMasks = comm.sortRandomRequest.dpfMasks.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dpfKeys = comm.sortRandomRequest.dpfKeys.Skip(numOfElement).ToArray();
            comm.sortRandomRequest.dpfAesKeys = comm.sortRandomRequest.dpfAesKeys.Skip(numOfElement).ToArray();

            logger.Debug($"Clear used randomness. {comm.sortRandomRequest.n} elements left.");
        }
    }
}