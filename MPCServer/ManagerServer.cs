using MPCTools;
using System;
using System.Linq;
using NLog;
using System.IO;
using System.Diagnostics;
using MPCTools.Requests;
using NLog.Config;

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

            if(args.Length <= 0)
            {
                logger.Error("Missing second server's IP address.");
                Environment.Exit(-1);
            }

            string memberServerIP = args[0];
            int memberServerPort = instance == 0 ? ProtocolConstants.portServerB : instance == 1 ? ProtocolConstants.portServerA : 0;
            comm.setInstance(instance);

            comm.ConnectServers(memberServerIP, memberServerPort);

            if (!comm.OpenSocket(instance == 0 ? ProtocolConstants.portServerA : ProtocolConstants.portServerB))
            {
                logger.Error("Could not create a socket between servers.");
                return;
            }

            while (true)
            {
                Run(memberServerIP, memberServerPort);
                comm.RestartServer();
            }
        }

        private static void Run(string memberServerIP, int memberServerPort)
        {      
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
            string fullPath = Path.Combine(@"..\\..\\..\\Results", fileName);
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
            deleteUsedMasksAndKeys(values.Length, comm.operation);
            comm.RestartServer();
         }

        private static void SetupLogger()
        {
            GlobalDiagnosticsContext.Set("serverInstance", instance == 0 ? "A" : "B");
            LogManager.Configuration = new XmlLoggingConfiguration(@"..\\..\\..\\Properties\\NLog.config");
            logger = LogManager.GetLogger("Server logger");

            if (isDebugMode)
            {
                LogManager.Configuration.AddRuleForAllLevels("logconsole", loggerNamePattern: "*");
                LogManager.Configuration.LoggingRules.Last().RuleName = ServerConstants.debugRuleName;
            }

            LogManager.ReconfigExistingLoggers();
        }

        public static uint[] Compute(OPERATION op)
        {
            logger.Info($"Number of elements: {values.Length}.");

            RandomRequest randomRequest = comm.randomRequests[op];

            if (values.Length > randomRequest.n)
            {
                string serverInstance = instance == 0 ? "A" : "B";
                string msg = $"Server {serverInstance} doesn't have enough correlated randomness to perform the computation.";
                logger.Error(msg);
                comm.SendMessageToAllClients(OPCODE_MPC.E_OPCODE_ERROR, msg);
                return null;
            }          

            Computer computer = InitComputer(op, randomRequest);

            var watch = Stopwatch.StartNew();

            uint[] res = computer.Compute();

            watch.Stop();

            var elapsedMs = watch.ElapsedMilliseconds;
            logger.Trace($"Operation {op} on {values.Length} elements: {elapsedMs} ms");
            logger.Trace($"Runtime {elapsedMs} ms");
            logger.Trace($"Memory consumption {computer.memoryBytesCounter} bytes");
            logger.Trace($"communication sent {computer.communicationBytesCounter} bytes");

            return res;
        }

        private static Computer InitComputer(OPERATION op, RandomRequest randomRequest)
        {
            switch (op)
            {
                case OPERATION.Sort:
                    {
                        return new SortComputer(values, randomRequest, instance, comm, new DcfAdapterServer(), new DpfAdapterServer(), logger);
                    }
            }

            return null;
        }

        private static void deleteUsedMasksAndKeys(int numOfElement, OPERATION op)
        {
            RandomRequest randomRequest = comm.randomRequests[op];
            int n = randomRequest.n;
            int dcfKeysToSkip = (2 * n - 1 - numOfElement) * numOfElement / 2;

            randomRequest.n = randomRequest.n - numOfElement;

            randomRequest.dcfMasks = randomRequest.dcfMasks.Skip(numOfElement).ToArray();
            randomRequest.dcfKeysSmallerLowerBound = randomRequest.dcfKeysSmallerLowerBound.Skip(dcfKeysToSkip).ToArray();
            randomRequest.dcfKeysSmallerUpperBound = randomRequest.dcfKeysSmallerUpperBound.Skip(dcfKeysToSkip).ToArray();
            randomRequest.shares01 = randomRequest.shares01.Skip(dcfKeysToSkip).ToArray();
            randomRequest.dcfAesKeysLower = randomRequest.dcfAesKeysLower.Skip(dcfKeysToSkip).ToArray();
            randomRequest.dcfAesKeysUpper = randomRequest.dcfAesKeysUpper.Skip(dcfKeysToSkip).ToArray();

            randomRequest.dpfMasks = randomRequest.dpfMasks.Skip(numOfElement).ToArray();
            randomRequest.dpfKeys = randomRequest.dpfKeys.Skip(numOfElement).ToArray();
            randomRequest.dpfAesKeys = randomRequest.dpfAesKeys.Skip(numOfElement).ToArray();

            comm.randomRequests[op] = randomRequest;
            logger.Debug($"Clear used {op} randomness. {randomRequest.n} elements left.");
        }
    }
}