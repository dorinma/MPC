using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.DataClientTest
{
    public class TestUtils
    {
        public static ulong[] GenerateRandomList(int size)
        {
            Random random = new Random();
            var output = new ulong[size];
            for (int i = 0; i < size; i++)
            {
                output[i] = ((ulong)random.Next(1000));
            }
            return output;
        }
    }
}
