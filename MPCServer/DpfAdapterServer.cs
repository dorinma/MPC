using System;
using System.Runtime.InteropServices;

namespace MPCServer
{
    public interface IDpfAdapterServer
    {
        uint EvalDPF(byte serverIndex, string key, string aesKey, uint inputSum, uint maskedInput);
    }

    public class DpfAdapterServer : IDpfAdapterServer
    {
        private const string dllPath = @"..\\..\\..\\ExtLibs\\DPF\\sycret.dll";
        //private const string dllPath = @"C:\Users\eden\Desktop\BGU\Project\MPC\sycretDPF\target\debug\sycret.dll";
        
        [StructLayout(LayoutKind.Sequential)]
        public struct Pair
        {
            public UInt32 first;
            public UInt32 second;
        }

        [DllImport(dllPath)]
        private static extern Pair eval_dpf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        public uint EvalDPF(byte serverIndex, string key, string aesKey, uint inputSum, uint maskedInput)
        {
            var index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = Marshal.StringToHGlobalAnsi(key);
            IntPtr aesPointer = Marshal.StringToHGlobalAnsi(aesKey);
            Pair shares = eval_dpf(keyPointer, aesPointer, inputSum, index);
            Marshal.FreeHGlobal(keyPointer);
            Marshal.FreeHGlobal(aesPointer);
            return ComputeFinalShare(shares, maskedInput);
        }

        private uint ComputeFinalShare(Pair shares, uint maskedInput)
        {
            return shares.first * maskedInput + shares.second;
        }
    }   
}
