using System;
using System.Runtime.InteropServices;

namespace MPCServer
{
    public class DCFAdapter
    {

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
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

        // --------------------------- remove ----------------
        [StructLayout(LayoutKind.Sequential)]
        public struct Keys
        {
            public IntPtr aesKeys;
            public IntPtr keyA;
            public IntPtr keyB;
        }

        private static byte[] PointersToBytes(IntPtr keyPointer, IntPtr aesPointer)
        {
            byte[] bytes = new byte[keySize + aesKeysSize];
            Marshal.Copy(keyPointer, bytes, 0, keySize);
            Marshal.Copy(aesPointer, bytes, keySize, aesKeysSize);
            return bytes;
        }

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern Keys gen_(UInt32 alpha);

        public void GenerateDPF(uint maskedFixedPoint, out byte[] keyA, out byte[] keyB)
        {
            Keys keys = gen_(maskedFixedPoint);
            keyA = PointersToBytes(keys.keyA, keys.aesKeys);
            keyB = PointersToBytes(keys.keyB, keys.aesKeys);
        }
    }
}
