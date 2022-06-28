namespace Tests.IntegrationTest
{
    using Moq;
    using MPCRandomnessClient;
    using MPCServer;
    using MPCTools;
    using MPCTools.Requests;
    using NLog;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Tests.ToolsTest;
    using Xunit;

    public class ComputationTest
    {
        private readonly ILogger loggerMock;

        public ComputationTest()
        {
            loggerMock = new Mock<ILogger>().Object;
        }

        [Theory]
        [MemberData(nameof(ValuesAndShares))]
        public void ComputeSortWithMock_ShouldSuccess(uint[] values, uint[] expectedIndexes)
        {
            RandomUtils.SplitToSecretShares(values, out uint[] sharesA, out uint[] sharesB);
            ManagerRandomnessClient.CreateCircuits(string.Empty, out RandomRequest requestA, out RandomRequest requestB);

            Mock<IDpfAdapterServer> dpfMock = new Mock<IDpfAdapterServer>();
            SetupDpfMock(dpfMock);
            FixKeysForMock(requestA, requestB);


            SortComputer computerA = new SortComputer(sharesA, requestA, 0, null, new DcfAdapterServer(), dpfMock.Object, loggerMock);
            SortComputer computerB = new SortComputer(sharesB, requestB, 1, null, new DcfAdapterServer(), dpfMock.Object, loggerMock);

            uint[] sumValuesMasks = TestUtils.SumLists(TestUtils.SumLists(sharesA, requestA.dcfMasks), TestUtils.SumLists(sharesB, requestB.dcfMasks));

            uint[] diffValues = computerA.DiffEachPairValues(sumValuesMasks, values.Length);

            uint[] sharesIndexesA = computerA.ComputeIndexesShares(diffValues, values.Length, 10);
            uint[] sharesIndexesB = computerB.ComputeIndexesShares(diffValues, values.Length, 10);

            Assert.Equal(expectedIndexes, TestUtils.SumLists(sharesIndexesA, sharesIndexesB));

            uint[] maskedIndexes = TestUtils.SumLists(TestUtils.SumLists(sharesIndexesA, requestA.dpfMasks), TestUtils.SumLists(sharesIndexesB, requestB.dpfMasks));

            uint[] resultA = computerA.ComputeResultsShares(maskedIndexes, sumValuesMasks, values.Length);
            uint[] resultB = computerB.ComputeResultsShares(maskedIndexes, sumValuesMasks, values.Length);

            uint[] sharesSum = TestUtils.SumLists(resultA, resultB);
            Array.Sort(values);

            Assert.Equal(values, sharesSum);
        }

        private void FixKeysForMock(RandomRequest requestA, RandomRequest requestB)
        {
            for (int i = 0; i < requestA.n; i++)
            {
                var dpfMask = (requestA.dpfMasks[i] + requestB.dpfMasks[i]).ToString();
                requestA.dpfKeys[i] = dpfMask;
                requestB.dpfKeys[i] = dpfMask;
                var dcfMask = (requestA.dcfMasks[i] + requestB.dcfMasks[i]).ToString();
                requestA.dpfAesKeys[i] = dcfMask;
                requestB.dpfAesKeys[i] = dcfMask;
            }
        }

        private void SetupDpfMock(Mock<IDpfAdapterServer> dpfMock)
        {
            int calls = 0;
            var rand = new Random();
            uint firstShare = rand.NextUInt32();
            uint secondShare = rand.NextUInt32();
            dpfMock.Setup(mock => mock.EvalDPF(0, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Callback(() => calls++)
                .Returns((byte instance, string key, string aes, uint alpha, uint beta) => alpha == UInt32.Parse(key) ? beta - UInt32.Parse(aes) - secondShare : firstShare);
            dpfMock.Setup(mock => mock.EvalDPF(1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<uint>(), It.IsAny<uint>()))
                .Returns((byte instance, string key, string aes, uint alpha, uint beta) => alpha == UInt32.Parse(key) ? secondShare : (uint)0 - firstShare);
        }

        [Theory]
        [MemberData(nameof(Values))]
        public void ComputeSort_ShouldSuccess(uint[] values)
        {
            RandomUtils.SplitToSecretShares(values, out uint[] sharesA, out uint[] sharesB);
            ManagerRandomnessClient.CreateCircuits(string.Empty, out RandomRequest requestA, out RandomRequest requestB);

            SortComputer computerA = new SortComputer(sharesA, requestA, 0, null, new DcfAdapterServer(), new DpfAdapterServer(), loggerMock);
            SortComputer computerB = new SortComputer(sharesB, requestB, 1, null, new DcfAdapterServer(), new DpfAdapterServer(), loggerMock);

            uint[] sumValuesMasks = TestUtils.SumLists(TestUtils.SumLists(sharesA, requestA.dcfMasks), TestUtils.SumLists(sharesB, requestB.dcfMasks));

            uint[] diffValues = computerA.DiffEachPairValues(sumValuesMasks, values.Length);

            uint[] sharesIndexesA = computerA.ComputeIndexesShares(diffValues, values.Length, 10);
            uint[] sharesIndexesB = computerB.ComputeIndexesShares(diffValues, values.Length, 10);

            uint[] maskedIndexes = TestUtils.SumLists(TestUtils.SumLists(sharesIndexesA, requestA.dpfMasks), TestUtils.SumLists(sharesIndexesB, requestB.dpfMasks));

            uint[] resultA = computerA.ComputeResultsShares(maskedIndexes, sumValuesMasks, values.Length);
            uint[] resultB = computerB.ComputeResultsShares(maskedIndexes, sumValuesMasks, values.Length);

            uint[] sharesSum = TestUtils.SumLists(resultA, resultB);
            Array.Sort(values);

            Assert.Equal(values, sharesSum);
        }

        public static IEnumerable<object[]> ValuesAndShares()
        {
            yield return new object[]
            {
                new uint[] { 3, 1, 2 }, // values
                new uint[] { 2, 0, 1 } // correct indexes
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
        }

        public static IEnumerable<object[]> Values()
        {
            yield return new object[]
           {
                new uint[] { 10, 5, 7 , 10000, 2147483647, 0, 1, 4444, 12, 100},
           };

            yield return new object[]
             {
                new uint[] { 1, 2, 3 , 4, 5, 6, 7, 8, 9, 10},
              };

            yield return new object[]
              {
                new uint[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1},
              };

            yield return new object[]
            {
                new uint[] { 7, 621, 212, 2080, 3265, 553, 25, 1 },
            };

            yield return new object[]
            {
                new uint[] { 13050108, 250082, 990200, 3048969, 3748079 },
            };

            yield return new object[]
            {
                TestUtils.GenerateRandomList(5)
            };

            yield return new object[]
            {
                TestUtils.GenerateRandomList(10)
            };
        }
    }
}
