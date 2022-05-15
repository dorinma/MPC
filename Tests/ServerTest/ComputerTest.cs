
namespace Tests.ServerTest
{
    using MPCTools;
    using MPCTools.Requests;
    using MPCServer;
    using System.Collections.Generic;
    using System.Linq;
    using Tests.ProtocolTest;
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
            uint[] outputA = computerA.SumEachSharesWithMask(new uint[masksA.Length], sharesValueA, masksA);
            uint[] outputB = computerB.SumEachSharesWithMask(outputA, sharesValueB, masksB);
            Assert.Equal(outputB, expectedOutput);
        }

        [Fact]
        public void test()
        { 
            uint[] values = new uint[] { 1, 5, 10 }; //TestUtils.GenerateRandomList(10);
            uint[] masks = new uint[] { 2, 3, 4 };//TestUtils.GenerateRandomList(10);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB);
            Computer computerA = InitEmptyComuter("A");
            Computer computerB = InitEmptyComuter("B");
            uint[] outputA = computerA.SumEachSharesWithMask(new uint[masksA.Length], valuesA, masksA);
            uint[] outputB = computerB.SumEachSharesWithMask(outputA, valuesB, masksB);
            Assert.Equal(outputB, new uint[] { 3, 8, 14 });
        }

        public static IEnumerable<object[]> SumEachSharesWithMask_inputs()
        {
            uint[] values = new uint[] {1,5,10 }; //TestUtils.GenerateRandomList(10);
            uint[] masks = new uint[] { 2, 3, 4 };//TestUtils.GenerateRandomList(10);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB);
            yield return new object[]
            {
                valuesA,
                masksA,
                valuesB,
                masksB,
                new uint[] {3,8,14}
                //TestUtils.SumList(values, masks)

            };

        }
    }
}
