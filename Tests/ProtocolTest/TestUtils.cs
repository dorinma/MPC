using MPCProtocol;
using System;

namespace Tests.ProtocolTest
{

    public class TestUtils
    {
        public static ulong[] GenerateRandomList(int size)
        {
            Random random = new Random();
            var output = new ulong[size];
            for (int i = 0; i < size; i++)
            {
                output[i] = random.NextUInt32();
            }
            return output;
        }
    }
}
