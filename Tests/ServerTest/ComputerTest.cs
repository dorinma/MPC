
namespace Tests.ServerTest
{
    using Moq;
    using MPCServer;
    using MPCTools;
    using MPCTools.Requests;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tests.ToolsTest;
    using Xunit;

    public class ComputerTest
    {
        private readonly ILogger loggerMock;
        public RandomRequest emptyRequest;

        public ComputerTest()
        {
            loggerMock = new Mock<ILogger>().Object;
            emptyRequest = new RandomRequest
            {
                sessionId = string.Empty,
                n = 10,
                dcfMasks = new uint[10], //also masks for the dpf output
                dcfKeysSmallerLowerBound = new string[45], // 10 C 2
                dcfKeysSmallerUpperBound = new string[45],
                shares01 = new uint[45],
                dcfAesKeysLower = new string[45],
                dcfAesKeysUpper = new string[45],
                dpfMasks = new uint[10],
                dpfKeys = new string[10],
                dpfAesKeys = new string[10]
            };
        }

        

        public SortComputer InitComuter(byte instance, RandomRequest randomRequest = default, IDcfAdapterServer dcfAdapter = default, IDpfAdapterServer dpfAdapter = default)
        {
            return new SortComputer(new uint[0], randomRequest, instance, null, dcfAdapter, dpfAdapter, loggerMock);
        }

        [Theory]
        [MemberData(nameof(SumEachSharesWithMask_ValidInputs))]
        public void SumEachSharesWithMask_ShouldSucceed(
            uint[] sharesValueA, uint[] masksA,
            uint[] sharesValueB, uint[] masksB,
            uint[] expectedOutput)
        {
            Computer computerA = InitComuter(0);
            Computer computerB = InitComuter(1);
            uint[] outputA = new uint[sharesValueA.Length];
            computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA);
            computerB.SumEachSharesWithMask(outputA, sharesValueB, masksB);
            Assert.Equal(outputA, expectedOutput);
        }

        [Theory]
        [MemberData(nameof(SumEachSharesWithMask_InvalidInputs))]
        public void SumEachSharesWithMask_ShouldFail(uint[] sharesValueA, uint[] masksA) 
        {
            Computer computerA = InitComuter(0);
            uint[] outputA = new uint[sharesValueA.Length];
             Assert.ThrowsAny<Exception>(() => computerA.SumEachSharesWithMask(outputA, sharesValueA, masksA));
        }

        [Theory]
        [MemberData(nameof(DiffValues))]
        public void ComputesIndexesShares_ShouldSuccess(uint[] values, uint[] expectedIndexes)
        {
            Mock<IDcfAdapterServer> dcfMock = new Mock<IDcfAdapterServer>();
            SortComputer computerA = InitComuter(0, emptyRequest, dcfAdapter: dcfMock.Object);
            SortComputer computerB = InitComuter(1, emptyRequest, dcfAdapter: dcfMock.Object);

            uint[] diffValues = computerA.DiffEachPairValues(values, values.Length); //same for both computers

            SetupDcfMock(dcfMock);
            FixKeysForMock(emptyRequest);

            uint[] sharesA = computerA.ComputeIndexesShares(diffValues, values.Length, 10);
            uint[] sharesB = computerB.ComputeIndexesShares(diffValues, values.Length, 10);

            uint[] sharesSum = TestUtils.SumLists(sharesA, sharesB);
            Assert.Equal(expectedIndexes, sharesSum);
        }

        [Theory]
        [MemberData(nameof(TooMuchValues))]
        public void ComputesIndexesShares_NotEnoughRandomness_ShouldFail(uint[] diffValues)
        {
            Mock<IDcfAdapterServer> dcfMock = new Mock<IDcfAdapterServer>();
            dcfMock.Setup(mock => mock.EvalDCF(0, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(0);
            SortComputer computerA = InitComuter(0, emptyRequest, dcfAdapter: dcfMock.Object);
            
            Assert.ThrowsAny<Exception>(() => computerA.ComputeIndexesShares(diffValues, diffValues.Length, 10));
        }

        [Theory]
        [MemberData(nameof(IndexesAndValues))]
        public void ComputesResultsShares_ShouldSuccess(uint[] values, uint[] sharesAValues, uint[] sharesBValues, uint[] indexes)
        {
            Mock<IDpfAdapterServer> dpfMock = new Mock<IDpfAdapterServer>();
            SortComputer computerA = InitComuter(0, emptyRequest, dpfAdapter: dpfMock.Object);
            SortComputer computerB = InitComuter(1, emptyRequest, dpfAdapter: dpfMock.Object);

            SetupDpfMock(dpfMock);

            uint[] sharesAResult = computerA.ComputeResultsShares(indexes, sharesAValues, values.Length);
            uint[] sharesBResult = computerB.ComputeResultsShares(indexes, sharesBValues, values.Length);

            uint[] sharesSum = TestUtils.SumLists(sharesAResult, sharesBResult);
            Array.Sort(values);
            Assert.Equal(values, sharesSum);
        }

        private void FixKeysForMock(RandomRequest request)
        {
            for (int i = 0; i < request.dcfKeysSmallerLowerBound.Length; i++)
            {
                request.dcfKeysSmallerLowerBound[i] = "0";
                request.dcfKeysSmallerUpperBound[i] = "1";
            }
        }

        private void SetupDpfMock(Mock<IDpfAdapterServer> dpfMock)
        {
            var rand = new Random();
            uint firstShare = rand.NextUInt32();
            dpfMock.Setup(mock => mock.EvalDPF(0, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((byte instance, string key, string aes, uint alpha, uint beta) => alpha == 0 ? beta : firstShare);
            dpfMock.Setup(mock => mock.EvalDPF(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((byte instance, string key, string aes, uint alpha, uint beta) => alpha == 0 ? beta : (uint)0-firstShare);
        }

        private void SetupDcfMock(Mock<IDcfAdapterServer> dcfMock)
        {
            var rand = new Random();
            uint firstShare = rand.NextUInt32();
            dcfMock.Setup(mock => mock.EvalDCF(0, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>()))
                .Returns(firstShare);
            dcfMock.Setup(mock => mock.EvalDCF(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>()))
                .Returns((byte instance, string key, string aes, uint alpha) => 
                    key.Equals("0") ? (alpha <= int.MaxValue ? 1-firstShare : 0-firstShare) :
                    (alpha <= uint.MaxValue ? 1 - firstShare : 0 - firstShare));
        }

        public static IEnumerable<object[]> SumEachSharesWithMask_ValidInputs()
        {
            uint[] values = TestUtils.GenerateRandomList(100);
            uint[] masks = TestUtils.GenerateRandomList(100);
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB);
            RandomUtils.SplitToSecretShares(masks, out uint[] masksA, out uint[] masksB);

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
            yield return new object[]
            {
                new uint[] { 10, 1, 100, 5 },
                new uint[] { 2, 0, 3, 1 },
            };

            yield return new object[]
            {
                new uint[] { 7, 621, 212, 2080, 3265, 553, 25, 1 },
                new uint[] { 1, 5, 3, 6, 7, 4, 2 ,0 },
            };

            yield return new object[]
            {
                new uint[] { 1 },
                new uint[] { 0 },
            };

            yield return new object[]
            {
                new uint[0],
                new uint[0],
            };

        }

        public static IEnumerable<object[]> TooMuchValues()
        {
            yield return new object[]
            {
                TestUtils.GenerateRandomList(15)
            };
        }

        public static IEnumerable<object[]> IndexesAndValues()
        {
            uint[] values = new uint[] { 7, 621, 212, 2080, 3265, 553, 25, 1 };
            uint[] indexes = new uint[] { 1, 5, 3, 6, 7, 4, 2, 0 };
            RandomUtils.SplitToSecretShares(values, out uint[] valuesA, out uint[] valuesB);

            yield return new object[]
            {
                values,
                valuesA,
                valuesB,
                indexes,
            };

            yield return new object[]
            {
                new uint[] { 10 },
                new uint[] { 1 },
                new uint[] { 9 },
                new uint[] { 0 },
            };

            yield return new object[]
            {
                new uint[0],
                new uint[0],
                new uint[0],
                new uint[0],
            };
        }
    }
}
