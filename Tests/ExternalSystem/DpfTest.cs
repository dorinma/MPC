
namespace Tests.ExternalSystem
{
    using MPCRandomnessClient;
    using MPCServer;
<<<<<<< HEAD
    using MPCTools;
    using System;
=======
    using MPCTools.Requests;
>>>>>>> master
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
<<<<<<< HEAD
        public void DpfWithEqualAlpha_ReturnsSharesOfOne(uint alpha, uint mask, uint input)
        {   
            dpfGenAdapter.GenerateDPF(alpha, 0-mask, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF("A", keyA, aesKey, alpha, mask+input);
            uint shareB = dpfEvalAdapter.EvalDPF("B", keyB, aesKey, alpha, mask+input);
            Assert.Equal(input, shareA + shareB);
=======
        public void DpfWithEqualAlpha_ReturnsSharesOfOne(uint alpha1, uint alpha2)
        {
            dpfGenAdapter.GenerateDPF(alpha1, beta: 0, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF(0, keyA, aesKey, alpha2, maskedInput: 0);
            uint shareB = dpfEvalAdapter.EvalDPF(1, keyB, aesKey, alpha2, maskedInput: 0);
            Assert.Equal((uint)1, shareA + shareB);
>>>>>>> master
        }

        [Theory]
        [MemberData(nameof(DifferentAlphaInputs))]
        public void DpfWithDifferentAlpha_ReturnsSharesOfZero(uint alpha1, uint alpha2, uint mask, uint input)
        {
<<<<<<< HEAD
            dpfGenAdapter.GenerateDPF(alpha1, 0-mask, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF("A", keyA, aesKey, alpha2, mask+input);
            uint shareB = dpfEvalAdapter.EvalDPF("B", keyB, aesKey, alpha2, mask+input);
=======
            dpfGenAdapter.GenerateDPF(alpha1, beta: 0, out string keyA, out string keyB, out string aesKey);
            uint shareA = dpfEvalAdapter.EvalDPF(0, keyA, aesKey, alpha2, maskedInput: 0);
            uint shareB = dpfEvalAdapter.EvalDPF(1, keyB, aesKey, alpha2, maskedInput: 0);
>>>>>>> master
            Assert.Equal((uint)0, shareA + shareB);
        }

        public static IEnumerable<object[]> EqualAlphaInputs()
        {
            Random rand = new Random();

            yield return new object[]
            {
                rand.NextUInt32(),
                rand.NextUInt32(),
                rand.NextUInt32(),
            };

            yield return new object[]
            {
                1024,
                2048,
                4096

            };

            yield return new object[]
            {
                uint.MaxValue,
                uint.MaxValue,
                uint.MaxValue,
            };
        }

        public static IEnumerable<object[]> DifferentAlphaInputs()
        {
            Random rand = new Random();

            yield return new object[]
            {
                5,
                10,
                rand.NextUInt32(),
                rand.NextUInt32(),
            };

            yield return new object[]
            {
                10,
                5,
                1,
                2
            };

            yield return new object[]
            {
                0,
                1,
                2,
                3
            };

            yield return new object[]
            {
                10,
                uint.MaxValue,
                10,
                uint.MaxValue

            };
        }
    }
}
