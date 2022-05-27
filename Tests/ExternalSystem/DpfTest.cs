
namespace Tests.ExternalSystem
{
    using MPCRandomnessClient;
    using MPCServer;
    using MPCTools.Requests;
    using System.Collections.Generic;
    using Xunit;

    public class DpfTest
    {
        private DpfAdapterRandClient dpfGenAdapter;
        private DpfAdapterServer dpfEvalAdapter;

        public DpfTest()
        {
            dpfGenAdapter = new DpfAdapterRandClient();
            dpfEvalAdapter = new DpfAdapterServer();
        }

        [Theory]
        [MemberData(nameof(EqualAlphaInputs))]
        public void DpfWithEqualAlpha_ReturnsSharesOfOne(uint alpha1, uint alpha2)
        {
            dpfGenAdapter.GenerateDPF(alpha1, beta: 0, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF("A", keyA, aesKey, alpha2, maskedInput: 0);
            uint shareB = dpfEvalAdapter.EvalDPF("B", keyB, aesKey, alpha2, maskedInput: 0);
            Assert.Equal((uint)1, shareA + shareB);
        }

        [Theory]
        [MemberData(nameof(DifferentAlphaInputs))]
        public void DcfWithBiggerAlpha_ReturnsSharesOfZero(uint alpha1, uint alpha2)
        {
            dpfGenAdapter.GenerateDPF(alpha1, beta: 0, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF("A", keyA, aesKey, alpha2, maskedInput: 0);
            uint shareB = dpfEvalAdapter.EvalDPF("B", keyB, aesKey, alpha2, maskedInput: 0);
            Assert.Equal((uint)0, shareA + shareB);
        }

        public static IEnumerable<object[]> EqualAlphaInputs()
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
        }

        public static IEnumerable<object[]> DifferentAlphaInputs()
        {
            yield return new object[]
            {
                5,
                10
            };

            yield return new object[]
            {
                10,
                5
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
