﻿using System;
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

        internal bool StartSession(out int operation, out uint numberOfUsers)
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
                Console.Write("Number of action: ");
            }

            if (action == 1)
            {
                operation = ReadOperation();
                numberOfUsers = ReadNumberOfUsers();
            }
            else
            {
                operation = 0;
                numberOfUsers = 0;
            }

            return action == 1;
        }

        public int ReadOperation()
        {
            int operation;
            Console.WriteLine("Insert the number of operation you want to perform:\n1. merge\n2. find the K'th element\n3. sort");
            while (!TryParseOperation(Console.ReadLine(), out operation))
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

        internal string ReadSessionId()
        {
            Console.WriteLine("Insert the session id");
            return Console.ReadLine();
        }

        private uint ReadNumberOfUsers()
        {
            uint numberOfUsers;
            Console.WriteLine("Insert number of users");
            while (!UInt32.TryParse(Console.ReadLine(), out numberOfUsers))
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
            return int.TryParse(userChoice, out operation) && operation <= 3 && operation >= 1;
        }

        public List<ulong> ReadData()
        {
            Console.WriteLine("Insert data file path");
            string path = Console.ReadLine();
            path = @"C:\Users\t-edentanami\OneDrive - Microsoft\Desktop\MPC project\Code\MPC\inputFile.csv";
            //path = @"C:\Users\דורין\Downloads\";
            path = "C:\\Users\\hodaya\\Desktop\\test.csv";
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

                ReadData();
            }
            return null;
        }

        public List<ulong> ReadData(string filePath)
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

        public List<ulong> ParseFile(string path)
        {
            List<ulong> output = new List<ulong>();
            Stream fileStream = fileSystem.File.OpenRead(path);
            using (var reader = new StreamReader(fileStream))
            {
                reader.ReadLine(); //skip column name
                while (!reader.EndOfStream)
                {
                    output.Add(ulong.Parse(reader.ReadLine()));
                }
            }

            return output;
        }

    }
}