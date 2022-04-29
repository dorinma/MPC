using MPCDataClient;
using MPCProtocol;
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
        public void CanAddTheoryMemberDataProperty(ulong[] inputList)
        {
            Randomness.SplitToSecretShares(inputList, out ulong[] serverAList, out ulong[] serverBList);
            Assert.Equal(inputList, SumList(serverAList, serverBList));
        }

        private ulong[] SumList(ulong[] listA, ulong[] listB)
        {
            return listA.Zip(listB, SumUints).ToArray();
        }

        private ulong SumUints(ulong a, ulong b)
        {
            return (ulong)(a + b);
        }

        public static IEnumerable<object[]> Data() {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
            };

            yield return new object[]
            {
                new ulong[]{ 1, 2, 3 }
            };

            yield return new object[]
            {
                new ulong[0]
            };
        }
    }
}
