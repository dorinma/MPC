using System;
using System.Runtime.InteropServices;

namespace MPCServer
{
    public class DpfAdapterServer
    {
        private const string dllPath = @"..\\..\\..\\ExtLibs\\sycret.dll";
        [DllImport(dllPath)]
        private static extern UInt32 eval_dcf2(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        public uint Eval(string serverIndex, string key, string aesKey, uint inputSum, uint maskedOrignalInput)
        {
            return 0;
        }
    }
}
