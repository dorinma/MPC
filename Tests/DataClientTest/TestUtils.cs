using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataClientTest
{
    public class TestUtils
    {
        public static List<UInt16> GenerateRandomList(int size)
        {
            Random random = new Random();
            var output = new List<UInt16>();
            for (int i = 0; i < size; i++)
            {
                output.Add((UInt16)random.Next(1000));
            }
            return output;
        }
    }
}
