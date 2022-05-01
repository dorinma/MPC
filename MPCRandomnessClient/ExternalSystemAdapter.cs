using System;
using System.Runtime.InteropServices;

namespace MPCRandomnessClient
{
    public class ExternalSystemAdapter
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Keys
        {
            public IntPtr aesKeys;
            public IntPtr keyA;
            public IntPtr keyB;
        }

        [DllImport(@"..\..\..\ExtLibs\sycret.dll")]
        private static extern Keys gen_(UInt32 alpha);

        [DllImport(@"..\..\..\ExtLibs\sycret.dll")]
        private static extern UInt32 eval_(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        [DllImport(@"..\..\..\ExtLibs\sycret.dll")]
        private static extern void clean_mem_(IntPtr dataPtr);


        private const int keySize = 596;
        private const int aesKeysSize = 64;
        public ExternalSystemAdapter() {}

        private static byte[] PointersToBytes(IntPtr keyPointer, IntPtr aesPointer)
        {
            byte[] bytes = new byte[keySize + aesKeysSize];
            Marshal.Copy(keyPointer, bytes, 0, keySize);
            Marshal.Copy(aesPointer, bytes, keySize, aesKeysSize);
            return bytes;
        }

        public void GenerateDCF(uint masksDiff, out uint key1, out uint key2) 
        {
            Keys keys = gen_(masksDiff);
            key1 = 1;
            key2 = 2;
        }

        public void GenerateDPF(uint maskedFixedPoint, out byte[] keyA, out byte[] keyB)
        {
            Keys keys = gen_(maskedFixedPoint);
            keyA = PointersToBytes(keys.keyA, keys.aesKeys);
            keyB = PointersToBytes(keys.keyB, keys.aesKeys);
        }

    }
}
