using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCProtocol
{
    public class MPCConvertor
    {
        public static List<ulong> BytesToList(byte[] Data, int StartOffset)
        {
            List<ulong> output = new List<ulong>();
            for (int i = StartOffset; i <= Data.Length - sizeof(ulong) && Data[i] != ProtocolConstants.NULL_TERMINATOR; i += sizeof(ulong))
            {
                output.Add(BitConverter.ToUInt64(Data, i));
            }
            return output;
        }

       /* public static ulong[] BytesToArray(byte[] Data, int StartOffset)
        {
            ulong[] output = new ulong[Data.Length/sizeof(ulong)];
            for (int i = StartOffset; i <= Data.Length - sizeof(ulong) && Data[i] != ProtocolConstants.NULL_TERMINATOR; i += sizeof(ulong))
            {
                output[i] = BitConverter.ToUInt64(Data, i);
            }
            return output;
        }*/
    }
}
