using MPCTools;
using System;
using System.Linq;

namespace Tests.ProtocolTest
{

    public class TestUtils
    {
        public static uint[] GenerateRandomList(int size)
        {
            Random random = new Random();
            var output = new uint[size];
            for (int i = 0; i < size; i++)
            {
                output[i] = random.NextUInt32();
            }
            return output;
        }

        public static uint[] SumList(uint[] listA, uint[] listB)
        {
            var a =  listA.Zip(listB, SumUints).ToArray();
            return a;
        }

        public static uint SumUints(uint a, uint b)
        {
            return (uint)(a - b);
        }
    }
}
