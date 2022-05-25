
namespace Tests.ExternalSystem
{
    using MPCRandomnessClient;
    using MPCServer;
    using System.Collections.Generic;
    using Xunit;

    public class DcfTest
    {
        private DcfAdapterRandClient dcfGenAdapter;
        private DcfAdapterServer dcfEvalAdapter;

        public DcfTest()
        {
            dcfGenAdapter = new DcfAdapterRandClient();
            dcfEvalAdapter = new DcfAdapterServer();
        }

        [Theory]
        [MemberData(nameof(SmallerOrEqualAlphaInputs))]
        public void DcfWithSmallerOrEqualAlpha_ReturnsSharesOfOne(uint alpha1, uint alpha2)
        {
            dcfGenAdapter.GenerateDCF(alpha1, out string keyA, out string keyB, out string aesKey);
            uint shareA = dcfEvalAdapter.EvalDCF("A", keyA, aesKey, alpha2);
            uint shareB = dcfEvalAdapter.EvalDCF("B", keyB, aesKey, alpha2);
            Assert.Equal((uint)1, shareA + shareB);
        }

        [Theory]
        [MemberData(nameof(BiggerAlphaInputs))]
        public void DcfWithBiggerAlpha_ReturnsSharesOfZero(uint alpha1, uint alpha2)
        {
            dcfGenAdapter.GenerateDCF(alpha1, out string keyA, out string keyB, out string aesKey);
            uint shareA = dcfEvalAdapter.EvalDCF("A", keyA, aesKey, alpha2);
            uint shareB = dcfEvalAdapter.EvalDCF("B", keyB, aesKey, alpha2);
            Assert.Equal((uint)0, shareA + shareB);
        }

        public static IEnumerable<object[]> SmallerOrEqualAlphaInputs()
        {
            yield return new object[]
            {
                5,
                5
            };

            yield return new object[]
            {
                1024,
                1024
            };

            yield return new object[]
            {
                uint.MaxValue,
                uint.MaxValue
            };

            yield return new object[]
            {
                10,
                5
            };

            yield return new object[]
            {
                2000,
                1500
            };
        }

        public static IEnumerable<object[]> BiggerAlphaInputs()
        {
            yield return new object[]
            {
                5,
                10
            };

            yield return new object[]
            {
                1024,
                2048
            };

            yield return new object[]
            {
                0,
                1
            };

            yield return new object[]
            {
                10,
                uint.MaxValue
            };
        }
    }
}
