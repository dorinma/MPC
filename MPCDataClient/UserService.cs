using MPCTools;
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

        internal bool StartSession(out OPERATION operation, out int numberOfUsers, out bool debugMode)
        {
            Console.WriteLine("Insert action to perform:");
            Console.WriteLine("1. Start new session");
            Console.WriteLine("2. Join existing session");
            int action;
            while (!int.TryParse(Console.ReadLine(), out action) || (action != 1 && action != 2))
            {
                Console.WriteLine("Invalid action.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }
            }

            if (action == 1)
            {
                operation = ReadOperation();
                numberOfUsers = ReadNumberOfUsers();
                debugMode = ReadDebugeMode();
            }
            else
            {
                operation = 0;
                numberOfUsers = 0;
                debugMode = false;
            }

            return action == 1;
        }

        private bool ReadDebugeMode()
        {
            Console.WriteLine("To run the computation in debug mode press 1, otherwise press any other character.");
            return Console.ReadLine().Equals("1");
        }

        public OPERATION ReadOperation()
        {
            int operationNumber;
            Console.WriteLine("Insert the number of operation you want to perform:");
            for (int i = 0; i < Operations.operations.Length; i++)
            { 
                Console.WriteLine($"{i + 1}. {Operations.operations[i]}");
            }
            while (!TryParseOperation(Console.ReadLine(), out operationNumber))
            {
                Console.WriteLine("Invalid operation number.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }
                else
                {
                    Console.WriteLine("Insert operation number:");
                }
            }
            return Operations.operations[operationNumber-1];
        }

        internal string ReadSessionId()
        {
            Console.WriteLine("Insert the session id");
            return Console.ReadLine();
        }

        private int ReadNumberOfUsers()
        {
            int numberOfUsers;
            Console.WriteLine("Insert number of users");
            while (!Int32.TryParse(Console.ReadLine(), out numberOfUsers))
            {
                Console.WriteLine("Invalid users number.");
                Console.WriteLine("If you want to try again press 1, otherwise press any other character.");
                var option = Console.ReadLine();
                if (option != "1")
                {
                    Environment.Exit(-1);
                }
                Console.Write("Number of users: ");
            }

            return numberOfUsers;
        }

        public bool TryParseOperation(string userChoice, out int operation)
        {
            return int.TryParse(userChoice, out operation) && operation <= Operations.operations.Length && operation >= 1;
        }

        public List<uint> ReadData()
        {
            Console.WriteLine("\nInsert data file path");
            string path = Console.ReadLine();
            //path = @"..\\..\\..\\..\\inputFile.csv";
            try
            {
                return ParseFile(path);
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

                return ReadData();
            }
        }

        public List<uint> ReadData(string filePath)
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

        public List<uint> ParseFile(string path)
        {
            List<uint> output = new List<uint>();
            Stream fileStream = fileSystem.File.OpenRead(path);
            using (var reader = new StreamReader(fileStream))
            {
                reader.ReadLine(); //skip column name
                while (!reader.EndOfStream)
                {
                    output.Add(uint.Parse(reader.ReadLine()));
                }
            }

            return output;
        }

    }
}