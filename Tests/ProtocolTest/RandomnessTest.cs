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
        public void SplitToSecretShares_Success(ulong[] inputList)
        {
            Randomness.SplitToSecretShares(inputList, out ulong[] sharesA, out ulong[] sharesB);
            Assert.Equal(inputList, SumList(sharesA, sharesB));
        }

        [Fact]
        public void SplitToSecretShares_Faliure()
        {
            Randomness.SplitToSecretShares(null, out ulong[] sharesA, out ulong[] sharesB); //assert not throw
        }

        private ulong[] SumList(ulong[] listA, ulong[] listB)
        {
            return (ulong[])listA.Zip(listB, SumUints).ToArray();
        }

        private ulong SumUints(ulong a, ulong b)
        {
            return (ulong)(a - b);
        }

        public static IEnumerable<object[]> ListsToSplit()
        {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(10000)
            };

            yield return new object[]
            {
                new ulong[] { 1, 2, 3 }
            };

            yield return new object[]
            {
                new ulong[0]
            };
        }
    }
}
