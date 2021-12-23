using System;

namespace MPCServer
{
    class Program
    {
        static void Main(string[] args)
        {
            Communication comm = new Communication();
            comm.StartServer();
        }
    }
}
