using System;
using System.Collections.Generic;
using System.IO;

namespace MPCDataClient
{
    class UserService
    {
        static void Main(string[] args)
        {
            List<UInt16> data = readData();
            int operation = readOperation();
            DataService dataService = new DataService();
            dataService.generateSecretShares(data);


            string IP = "172.16.205.108"; //eden 100.64.182.7
            Connect(IP);
            Communication<UInt16>.SendRequest(data);
            while (true)
            {
                if (Console.Read() == 'q')
                    break;
            }
        }

        public static List<UInt16> readData()
        {
            //Console.WriteLine("Insert data file path");
            //string path = "";
            //path = Console.ReadLine();
            string path = "C:\\Users\\USER\\Desktop\\inputFile.csv";
            return readFromFile(path);

        }

        public static int readOperation()
        {
            Console.WriteLine("Insert the number of operation you want to perform:\n1. merge\n2. find the K'th element\n3. sort");
            int operation = 0;
            operation = Convert.ToInt32(Console.ReadLine());
            return operation;

        }

         
        private static void Connect(string IP)
        {
            Communication<UInt16>.Connect(IP);
        }

        public static List<UInt16> readFromFile(string path)
        {
            List<UInt16> output = new List<UInt16>();
            try
            {
                using (var reader = new StreamReader(path))
                {
                    UInt16 curr;
                    reader.ReadLine(); //skip column name
                    while (!reader.EndOfStream)
                    {
                        if (!UInt16.TryParse(reader.ReadLine(), out curr))
                        {
                            return null;
                        }
                        output.Add(curr);
                    }
                }
                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading failed - {0}", e.Message);
                return null;
            }
        }

    }
}