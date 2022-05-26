using System;
using System.Runtime.InteropServices;

namespace MPCServer
{
    public interface IDpfAdapterServer
    {
        uint EvalDPF(string serverIndex, string key, string aesKey, uint inputSum, uint maskedInput);
    }

    public class DpfAdapterServer : IDpfAdapterServer
    {
        //private const string dllPath = @"..\\..\\..\\ExtLibs\\sycret.dll";
        private const string dllPath = @"C:\Users\eden\Desktop\BGU\Project\MPC\sycretDPF\target\debug\sycret.dll";

        [DllImport(dllPath)]
        private static extern UInt32 eval_dpf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        public uint EvalDPF(string serverIndex, string key, string aesKey, uint inputSum, uint maskedInput)
        {
            var index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = Marshal.StringToHGlobalAnsi(key);
            IntPtr aesPointer = Marshal.StringToHGlobalAnsi(aesKey);
            UInt32 share = eval_dpf(keyPointer, aesPointer, inputSum, index);
            Marshal.FreeHGlobal(keyPointer);
            Marshal.FreeHGlobal(aesPointer);
            return share;
        }
    }   
}
