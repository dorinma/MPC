namespace MPCRandomnessClient
{
    using System;
    using System.Runtime.InteropServices;

    public class DpfAdapterRandClient
    {
        private const string dllPath = @"..\\..\\..\\ExtLibs\\sycret.dll";

        [StructLayout(LayoutKind.Sequential)]
        public struct Keys
        {
            public IntPtr aesKeys;
            public IntPtr keyA;
            public IntPtr keyB;
        }

        [DllImport(dllPath)]
        private static extern Keys gen_dcf(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern void free_string(IntPtr pointerToFree);

        public void GenerateDPF(uint maskedFixedPoint, uint beta, out string keyA, out string keyB, out string aesKey)
        {
            keyA = string.Empty;
            keyB = string.Empty;
            aesKey = string.Empty;
        }
    }
}
