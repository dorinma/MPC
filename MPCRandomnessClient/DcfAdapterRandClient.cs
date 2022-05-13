namespace MPCRandomnessClient
{
    using System;
    using System.Runtime.InteropServices;

    public class DcfAdapterRandClient
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
        private static extern Keys gen_dcf2(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern void free_string(IntPtr pointerToFree);

        public void GenerateDCF(uint masksDiff, out string keyA, out string keyB, out string aesKey)
        {
            Keys keys = gen_dcf2(masksDiff);
            aesKey = Marshal.PtrToStringAnsi(keys.aesKeys);
            keyA = Marshal.PtrToStringAnsi(keys.keyA);
            keyB = Marshal.PtrToStringAnsi(keys.keyB);
            free_string(keys.aesKeys);
            free_string(keys.keyA);
            free_string(keys.keyB);
        }
    }
}
