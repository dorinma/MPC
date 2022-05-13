using MPCProtocol;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.ProtocolTest
{
    public class RandomnessTest
    {
        [Theory]
        [MemberData(nameof(ListsToSplit))]
        public void SplitToSecretShares_Success(uint[] inputList)
        {
            RandomUtils.SplitToSecretShares(inputList, out uint[] sharesA, out uint[] sharesB);
            Assert.Equal(inputList, SumList(sharesA, sharesB));
        }

        [Fact]
        public void SplitToSecretShares_Faliure()
        {
            RandomUtils.SplitToSecretShares(null, out uint[] sharesA, out uint[] sharesB); //assert not throw
        }

        private uint[] SumList(uint[] listA, uint[] listB)
        {
            return (uint[])listA.Zip(listB, SumUints).ToArray();
        }

        private uint SumUints(uint a, uint b)
        {
            return (uint)(a - b);
        }

        public static IEnumerable<object[]> ListsToSplit()
        {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
            };

            yield return new object[]
            {
                new uint[] { 1, 2, 3 }
            };

            yield return new object[]
            {
                new uint[0]
            };
        }
    }
}
