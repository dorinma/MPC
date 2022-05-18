using MPCTools;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Tests.ToolsTest
{
    public class RandomnessTest
    {
        [Theory]
        [MemberData(nameof(ListsToSplit))]
        public void SplitToSecretShares_Success(uint[] inputList)
        {
            RandomUtils.SplitToSecretShares(inputList, out uint[] sharesA, out uint[] sharesB);
            Assert.Equal(inputList, TestUtils.SumList(sharesA, sharesB));
        }

        [Fact]
        public void SplitToSecretShares_Faliure()
        {
            RandomUtils.SplitToSecretShares(null, out uint[] sharesA, out uint[] sharesB); //assert not throw
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
