using System;
using System.Runtime.InteropServices;

namespace MPCRandomnessClient
{
    public class ExternalSystemAdapter
    {
        //private const string dllPath = @"C:\Users\eden\Desktop\BGU\Project\MPC\MPCRandomnessClient\ExtLibs\sycret.dll";
        private const string dllPath = @"C:\Users\hodaya\Desktop\MPC project\MPC\MPCRandomnessClient\ExtLibs\sycret.dll";
        [StructLayout(LayoutKind.Sequential)]
        public struct Keys
        {
            public IntPtr aesKeys;
            public IntPtr keyA;
            public IntPtr keyB;
        }

        [DllImport(dllPath)]
        private static extern Keys gen_dpf(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern Keys gen_dcf2(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern UInt32 eval_dcf2(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        [DllImport(dllPath)]
        private static extern void free_string(IntPtr pointerToFree);


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

        public void GenerateDCF_File(uint masksDiff, out byte[] key1, out byte[] key2) 
        {
            key1 = null;
            key2 = null;
            gen_dpf_test(masksDiff); // writes the keys to file

            /*Keys keys = gen_dcf(masksDiff);
            key1 = PointersToBytes(keys.keyA, keys.aesKeys, dcfKeySize, aesKeysSize);
            key2 = PointersToBytes(keys.keyB, keys.aesKeys, dcfKeySize, aesKeysSize);*/
        }

        public void GenerateDPF(uint maskedFixedPoint, uint beta, out string keyA, out string keyB, out string aesKey)
        {
            keyA = string.Empty;
            keyB = string.Empty;
            aesKey = string.Empty;
            /*Keys keys = gen_dpf(maskedFixedPoint);
            keyA = PointersToBytes(keys.keyA, keys.aesKeys, dpfKeySize, aesKeysSize);
            keyB = PointersToBytes(keys.keyB, keys.aesKeys, dpfKeySize, aesKeysSize);*/
        }


        // -----------------------------

        [DllImport(dllPath)]
        private static extern UInt32 eval_dpf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        [DllImport(dllPath)]
        private static extern UInt32 eval_dcf(IntPtr key, IntPtr aesKeys, UInt32 alpha, byte partyId);

        [DllImport(dllPath)]
        private static extern UInt32 eval2(Keys keys, UInt32 alpha);

        [DllImport(dllPath)]
        private static extern UInt32 gen_dpf2(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern UInt32 gen_dpf_test(UInt32 alpha);

        [DllImport(dllPath)]
        private static extern UInt32 eval_dpf_test(UInt32 alpha, byte partyId);

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

        public uint GenEval(uint alpha, uint x) // for future int insted of string server index
        {
            var keys = gen_dcf2(5);
            var aes = Marshal.PtrToStringAnsi(keys.aesKeys);
            string keyA = Marshal.PtrToStringAnsi(keys.keyA);
            string keyB = Marshal.PtrToStringAnsi(keys.keyB);
            ////
            IntPtr keyAPointer = (IntPtr)Marshal.StringToHGlobalAnsi(keyA);
            IntPtr keyBPointer = (IntPtr)Marshal.StringToHGlobalAnsi(keyB);
            IntPtr aesPointer = (IntPtr)Marshal.StringToHGlobalAnsi(aes);
            var i = eval_dcf2(keyAPointer, aesPointer, 6, 0);
            var j = eval_dcf2(keyBPointer, aesPointer, 6, 1);
            Console.WriteLine($"a {i} , j {j} ->  output {i+j}");
            return 5;

            /*var a = gen_dpf_test(5);
            var o1 = eval_dpf_test(7, 0);
            var o2 = eval_dpf_test(7, 1);
            var o = o1 + o2;
            return a;*/
           /* Keys keys = gen_dpf(alpha);
            uint output = eval2(keys, x);
            Console.WriteLine($"output {output}");
            return output;*/
            /*keyA = PointersToBytes(keys.keyA, keys.aesKeys, dpfKeySize, aesKeysSize);
            keyB = PointersToBytes(keys.keyB, keys.aesKeys, dpfKeySize, aesKeysSize);*/
        }



    }
}
