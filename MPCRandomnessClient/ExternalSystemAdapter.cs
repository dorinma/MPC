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

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern Keys gen_dpf(UInt32 alpha);

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern Keys gen_dcf(UInt32 alpha);


        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern void clean_mem_(IntPtr dataPtr);


        private const int dpfKeySize = 596;
        private const int dcfKeySize = 916;
        private const int aesKeysSize = 64;
        public ExternalSystemAdapter() {}

        private static byte[] PointersToBytes(IntPtr keyPointer, IntPtr aesPointer, int firstSize, int secondSize)
        {
            byte[] bytes = new byte[firstSize + secondSize];
            Marshal.Copy(keyPointer, bytes, 0, firstSize);
            Marshal.Copy(aesPointer, bytes, firstSize, secondSize);
            return bytes;
        }

        public void GenerateDCF(uint masksDiff, out byte[] key1, out byte[] key2) 
        {
            Keys keys = gen_dcf(masksDiff);
            key1 = PointersToBytes(keys.keyA, keys.aesKeys, dcfKeySize, aesKeysSize);
            key2 = PointersToBytes(keys.keyB, keys.aesKeys, dcfKeySize, aesKeysSize);
        }

        public void GenerateDPF(uint maskedFixedPoint, out byte[] keyA, out byte[] keyB)
        {
            Keys keys = gen_dpf(maskedFixedPoint);
            keyA = PointersToBytes(keys.keyA, keys.aesKeys, dpfKeySize, aesKeysSize);
            keyB = PointersToBytes(keys.keyB, keys.aesKeys, dpfKeySize, aesKeysSize);
        }


        // -----------------------------

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern UInt32 eval_dpf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        [DllImport(@"C:\Users\דורין\Desktop\ExtLibs\MPC_master\MPC\MPCRandomnessClient\ExtLibs\sycret.dll")]
        private static extern UInt32 eval_dcf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        private static IntPtr BytesToPointer(byte[] bytes, int offset, int length)
        {
            IntPtr pointer = Marshal.AllocHGlobal(length);
            Marshal.Copy(bytes, offset, pointer, length);
            return pointer;

        }
        public uint EvalDpf(string serverIndex, byte[] keyBytes, uint alpha) // for future int insted of string server index
        {
            byte index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = BytesToPointer(keyBytes, 0, dpfKeySize);
            IntPtr aesKeysPointer = BytesToPointer(keyBytes, dpfKeySize, aesKeysSize);
            uint share = eval_dpf(keyPointer, aesKeysPointer, alpha, index);
            return share;
        }

        public uint EvalDcf(string serverIndex, byte[] keyBytes, uint alpha) // for future int insted of string server index
        {
            byte index = serverIndex.Equals("A") ? (byte)0 : (byte)1;
            IntPtr keyPointer = BytesToPointer(keyBytes, 0, dcfKeySize);
            IntPtr aesKeysPointer = BytesToPointer(keyBytes, dcfKeySize, aesKeysSize);
            uint share = eval_dcf(keyPointer, aesKeysPointer, alpha, index);
            return share;
        }

    }
}
