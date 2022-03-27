using System;
using System.Collections.Generic;
using System.IO;

namespace MPCDataClient
{
    public class UserService
    {
        public static List<UInt16> readData()
        {
            Console.WriteLine("Insert data file path");
            string path = "";
            path = Console.ReadLine();
            //string path = "C:\\Users\\USER\\Desktop\\inputFile.csv";
            path = "C:\\Users\\hodaya\\Desktop\\test.csv";
            return readFromFile(path);

        }

        public static int readOperation()
        {
            Console.WriteLine("Insert the number of operation you want to perform:\n1. merge\n2. find the K'th element\n3. sort");
            int operation = 0;
            
            operation = Convert.ToInt32(Console.ReadLine());
            return operation;

        }

        public static List<UInt16> readFromFile(string path)
        {
            Console.WriteLine("Start reading..");
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

                        Console.WriteLine("curr = " + curr);
                    }
                }
                Console.WriteLine("Read file successfuly");
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