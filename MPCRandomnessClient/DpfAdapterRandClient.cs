namespace MPCRandomnessClient
{
    using System;
    using System.Runtime.InteropServices;

    public class DpfAdapterRandClient
    {
        //private const string dllPath = @"..\\..\\..\\ExtLibs\\sycret.dll";
        private const string dllPath = @"C:\Users\eden\Desktop\BGU\Project\MPC\sycretDPF\target\debug\sycret.dll";

        [StructLayout(LayoutKind.Sequential)]
        public struct Keys
        {
            public IntPtr aesKeys;
            public IntPtr keyA;
            public IntPtr keyB;
        }

        [DllImport(dllPath)]
        private static extern Keys gen_dpf(UInt32 alpha, UInt32 beta);

        [DllImport(dllPath)]
        private static extern void free_string(IntPtr pointerToFree);

        public void GenerateDPF(uint mask, uint beta, out string keyA, out string keyB, out string aesKey)
        {
            Keys keys = gen_dpf(mask, beta);
            aesKey = Marshal.PtrToStringAnsi(keys.aesKeys);
            keyA = Marshal.PtrToStringAnsi(keys.keyA);
            keyB = Marshal.PtrToStringAnsi(keys.keyB);
            free_string(keys.aesKeys);
            free_string(keys.keyA);
            free_string(keys.keyB);
        }
    }
}
