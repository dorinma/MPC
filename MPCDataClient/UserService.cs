using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;

namespace MPCDataClient
{
    public class UserService
    {
        //https://github.com/TestableIO/System.IO.Abstractions
        //https://stackoverflow.com/questions/52077416/unit-test-a-method-that-has-dependency-on-streamreader-for-reading-file
        readonly IFileSystem fileSystem;

        public UserService(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }
        /// <summary>Create MyComponent</summary>
        public UserService() : this(fileSystem: new FileSystem()) {}

        public int ReadOperation()
        {
            int operation; 
            Console.WriteLine("Insert the number of operation you want to perform:\n1. merge\n2. find the K'th element\n3. sort");
            while(!TryParseOperation(Console.ReadLine(), out operation))
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

        public bool TryParseOperation(string userChoice, out int operation)
        {
            return int.TryParse(userChoice, out operation) && operation <= 3 && operation >= 1;
        }

        public List<UInt16> ReadData(string filePath)
         {
            try
            {
                return ParseFile(filePath);
            }
            catch (Exception e)
            {
                /*Console.WriteLine("Reading failed - {0}", e.Message);
                Console.WriteLine("If you want to try again press 1, otherwise press any other character");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }

                ReadData();*/
                return null;
            }
        }

        public List<UInt16> ParseFile(string path)
        {
            List<UInt16> output = new List<UInt16>();
            Stream fileStream = fileSystem.File.OpenRead(path);
            using (var reader = new StreamReader(fileStream))
            {
                reader.ReadLine(); //skip column name
                while (!reader.EndOfStream)
                {
                    output.Add(UInt16.Parse(reader.ReadLine()));
                }
            }

            return output;
        }

    }
}