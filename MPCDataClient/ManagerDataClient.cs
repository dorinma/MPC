namespace MPCDataClient
{ 
    using System;
    using System.Collections.Generic;
    using System.IO;
public class ManagerDataClient
    {
        static void Main(string[] args) // ip1 port1 ip2 port2
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Missing servers ip.");
                Environment.Exit(-1);
            }

            string ip1 = args[0];
            string ip2 = args[2];
            int port1 = 0;
            int port2 = 0;
            try
            {
                port1 = Int32.Parse(args[1]);
                port2 = Int32.Parse(args[3]);
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalid port number.");
                Environment.Exit(-1);
            }

            List<UInt16> data = UserService.readData();
            int operation = UserService.readOperation();
            DataService dataService = new DataService();
            dataService.generateSecretShares(data);

            CommunicationDataClient<UInt16> commServerA = new CommunicationDataClient<UInt16>(ip1, port1);
            CommunicationDataClient<UInt16> commServerB = new CommunicationDataClient<UInt16>(ip2, port2);

            commServerA.Connect();
            commServerB.Connect();

            commServerA.SendRequest(dataService.serverAList);
            commServerB.SendRequest(dataService.serverBList);

            commServerA.ReceiveRequest();
            commServerB.ReceiveRequest();

            commServerA.WaitForReceive();
            commServerB.WaitForReceive();

            Console.WriteLine(commServerA.response);
            Console.WriteLine(commServerB.response);
        }

        
    }
}
