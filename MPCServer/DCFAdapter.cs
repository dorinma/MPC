using System;
using System.Runtime.InteropServices;

namespace MPCServer
{
    public class DCFAdapter
    {
        
        [DllImport(@"..\..\..\ExtLibs\sycret.dll")]
        private static extern UInt32 eval_(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);


        private const int keySize = 596;
        private const int aesKeysSize = 64;

        private static IntPtr BytesToPointer(byte[] bytes, int offset, int length)
        {
            IntPtr pointer = Marshal.AllocHGlobal(length);
            Marshal.Copy(bytes, offset, pointer, length);
            return pointer;

        }
        public uint Eval(string serverIndex, byte[] keyBytes, uint alpha) // for future int insted of string server index
        {
            byte index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = BytesToPointer(keyBytes, 0, keySize);
            IntPtr aesKeysPointer = BytesToPointer(keyBytes, keySize, aesKeysSize);
            uint share = eval_(keyPointer, aesKeysPointer, alpha, index);
            return share;
        }
    }
}
