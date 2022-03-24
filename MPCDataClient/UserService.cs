using System;
using System.Collections.Generic;
using System.IO;

namespace MPCDataClient
{
    public class UserService
    {
        public static int readOperation()
        {
            int operation; 
            Console.WriteLine("Insert the number of operation you want to perform:\n1. merge\n2. find the K'th element\n3. sort");
            while(!int.TryParse(Console.ReadLine(), out operation) || operation > 3 || operation < 1)
            {
                Console.WriteLine("Invalid operation number.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }
                Console.Write("Number of operation: ");
            }
            return operation;

        }

        public static List<UInt16> readData()
         {
            Console.WriteLine("Insert data file path");
            string path = @"C:\Users\eden\Desktop\BGU\Semester7\Project\MPC\inputFile.csv";
            path = Console.ReadLine();

            List<UInt16> output = new List<UInt16>();
            try
            {
                using (var reader = new StreamReader(path))
                {
                    reader.ReadLine(); //skip column name
                    while (!reader.EndOfStream)
                    {
                        output.Add(UInt16.Parse(reader.ReadLine()));
                    }
                }
                Console.WriteLine("Read file successfuly");
            }
            catch (Exception e)
            {
                Console.WriteLine("Reading failed - {0}", e.Message);
                Console.WriteLine("If you want to try again press 1, otherwise press any other character");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }

                readData();
            }
            return output;
        }

    }
}