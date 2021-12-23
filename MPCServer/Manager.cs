using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCServer
{
    class Manager
    {
        private static Computer comp = new SortCircuit();

        static void Main(string[] args)
        {
            List<UInt16> values = new List<UInt16>();
            Communication comm = new Communication(values, 2, 10);
            values = comm.StartServer();
            for (int i = 0; i < values.Count; i++) Console.WriteLine(values.ElementAt(i));
            comp.data = values;
        }
    }
}
