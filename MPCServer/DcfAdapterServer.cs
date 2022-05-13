namespace MPCServer
{
    using System;
    using System.Runtime.InteropServices;

    public class DcfAdapterServer
    {
        private const string dllPath = @"..\\..\\..\\ExtLibs\\sycret.dll";
        [DllImport(dllPath)]
        private static extern UInt32 eval_dcf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        public uint EvalDCF(string serverIndex, string key, string aesKey, uint alpha) // for future int insted of string server index
        {
            var index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = Marshal.StringToHGlobalAnsi(key);
            IntPtr aesPointer = Marshal.StringToHGlobalAnsi(aesKey);
            UInt32 share = eval_dcf(keyPointer, aesPointer, alpha, index);
            Marshal.FreeHGlobal(keyPointer);
            Marshal.FreeHGlobal(aesPointer);
            return share;
        }
    }
}
