using MPCDataClient;
using MPCTools;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.DataClientTest
{
    public class DataServiceTest
    {
        [Theory]
        [MemberData(nameof(Data))]
        public void CanAddTheoryMemberDataProperty(uint[] inputList)
        {
            RandomUtils.SplitToSecretShares(inputList, out uint[] serverAList, out uint[] serverBList);
            Assert.Equal(inputList, SumList(serverAList, serverBList));
        }

        private uint[] SumList(uint[] listA, uint[] listB)
        {
            return listA.Zip(listB, SumUints).ToArray();
        }

        private uint SumUints(uint a, uint b)
        {
            return (uint)(a + b);
        }

        public static IEnumerable<object[]> Data() {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
            };

            yield return new object[]
            {
                new uint[]{ 1, 2, 3 }
            };

            yield return new object[]
            {
                new uint[0]
            };
        }
    }
}
