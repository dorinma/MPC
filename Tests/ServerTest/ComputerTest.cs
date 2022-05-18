
namespace Tests.ServerTest
{
    using Moq;
    using MPCServer;
    using MPCTools;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tests.ToolsTest;
    using Xunit;

    public class ComputerTest
    {
        public ComputerTest()
        {
        }

        public Computer InitComuter(string instance, IDcfAdapterServer dcfAdapter = default, IDpfAdapterServer dpfAdapter = default)
        {
            return new Computer(null, null, instance, null, dcfAdapter, dpfAdapter);
        }

        [Theory]
        [MemberData(nameof(SumEachSharesWithMask_ValidInputs))]
        public void SumEachSharesWithMask_ShouldSucceed(
            uint[] sharesValueA, uint[] masksA,
            uint[] sharesValueB, uint[] masksB,
            uint[] expectedOutput)
        {
            Computer computerA = InitComuter("A");
            Computer computerB = InitComuter("B");
            uint[] outputA = new uint[sharesValueA.Length];
            computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA);
            computerB.SumEachSharesWithMask(outputA, sharesValueB, masksB);
            Assert.Equal(outputA, expectedOutput);
        }

        [Theory]
        [MemberData(nameof(SumEachSharesWithMask_InvalidInputs))]
        public void SumEachSharesWithMask_ShouldFail(uint[] sharesValueA, uint[] masksA) 
        {
            Computer computerA = InitComuter("A");
            uint[] outputA = new uint[sharesValueA.Length];
             Assert.ThrowsAny<Exception>(() => computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA));
        }

        [Theory]
        [MemberData(nameof(DiffValues))]
        public void ComputesIndexesShares_ShouldSuccess(uint[] values, uint[] expectedIndexes)
        {
            Mock<IDcfAdapterServer> dcfMock = new Mock<IDcfAdapterServer>();
            Computer computerA = InitComuter("A");
            uint[] diffValues = computerA.DiffEachPairValues(values, values.Length);
            uint[] sharesA = computerA.ComputeIndexesShares(diffValues, values.Length, 100);

            Assert.Equal(expectedIndexes, TestUtils.SumLists(sharesA, sharesB));
           /* Mock<IDcfAdapterServer> dcfMock = new Mock<IDcfAdapterServer>();
            Computer computerA = InitEmptyComuter("A");
            uint[] outputA = new uint[sharesValueA.Length];
            Assert.ThrowsAny<Exception>(() => computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA));*/
        }


        public static IEnumerable<object[]> SumEachSharesWithMask_ValidInputs()
        {
            uint[] values = TestUtils.GenerateRandomList(100);
            uint[] masks = TestUtils.GenerateRandomList(100);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB, true);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB, true);

            yield return new object[]
            {
                valuesA,
                masksA,
                valuesB,
                masksB,
                TestUtils.SumLists(values, masks)
            };

            yield return new object[]
            {
                new uint[0],
                new uint[0],
                new uint[0],
                new uint[0],
                new uint[0]
            };

        }

        public static IEnumerable<object[]> SumEachSharesWithMask_InvalidInputs()
        {
            yield return new object[]
            {
                new uint[10],
                new uint[5], // fewer masks the value -> not enough randomness
            };

            yield return new object[]
            {
                new uint[10],
                null,
            };

        }

        public static IEnumerable<object[]> DiffValues()
        {
            uint[] values = new uint[] { 10, 1, 100, 5 };
            uint[] expectedIndexes = new uint[] { 2, 0, 3, 1 };
            yield return new object[]
            {
                new uint[] { 10, 1, 5 },
                new uint[] { 2, 0, 3, 1 },
        };

            yield return new object[]
            {
                new uint[10],
                null,
            };

        }
    }
}
