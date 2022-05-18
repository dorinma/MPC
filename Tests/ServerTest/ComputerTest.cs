
namespace Tests.ServerTest
{
    using MPCServer;
    using MPCTools;
    using System.Collections.Generic;
    using System.Linq;
    using Tests.ToolsTest;
    using Xunit;

    public class ComputerTest
    {
        public ComputerTest()
        {
        }

        public Computer InitEmptyComuter(string instance)
        {
            return new Computer(null, null, instance, null);
        }

        [Theory]
        [MemberData(nameof(SumEachSharesWithMask_inputs))]
        public void SumEachSharesWithMask_ShouldSucceed(
            uint[] sharesValueA, uint[] masksA,
            uint[] sharesValueB, uint[] masksB,
            uint[] expectedOutput)
        {
            Computer computerA = InitEmptyComuter("A");
            Computer computerB = InitEmptyComuter("B");
            uint[] outputA = new uint[masksA.Length];
            computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA);
            computerB.SumEachSharesWithMask(outputA, sharesValueB, masksB);
            Assert.Equal(outputA, expectedOutput);
        }

        [Fact]
        public void test()
        { 
            uint[] values = new uint[] { 1, 5, 10 }; //TestUtils.GenerateRandomList(10);
            uint[] masks = new uint[] { 2, 3, 4 };//TestUtils.GenerateRandomList(10);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB, true);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB, true);
            Computer computerA = InitEmptyComuter("A");
            Computer computerB = InitEmptyComuter("B");
            uint[] outputA = new uint[masksA.Length];
            computerA.SumEachSharesWithMask(outputA, valuesA, masksA);
            computerB.SumEachSharesWithMask(outputA, valuesB, masksB);
            Assert.Equal(outputA, new uint[] { 3, 8, 14 });
        }

        public static IEnumerable<object[]> SumEachSharesWithMask_inputs()
        {
            uint[] values = TestUtils.GenerateRandomList(10);
            uint[] masks = TestUtils.GenerateRandomList(10);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB, true);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB, true);
            yield return new object[]
            {
                valuesA,
                masksA,
                valuesB,
                masksB,
                TestUtils.SumList(values, masks)
            };

        }
    }
}
