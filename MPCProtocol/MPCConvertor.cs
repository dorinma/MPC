using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCTools
{
    public class MPCConvertor
    {
        public static List<uint> BytesToList(byte[] Data, int StartOffset)
        {
            List<uint> output = new List<uint>();
            for (int i = StartOffset; i <= Data.Length - sizeof(uint) && Data[i] != ProtocolConstants.NULL_TERMINATOR; i += sizeof(uint))
            {
                output.Add(BitConverter.ToUInt32(Data, i));
            }
            return output;
        }

       /* public static uint[] BytesToArray(byte[] Data, int StartOffset)
        {
            uint[] output = new uint[Data.Length/sizeof(uint)];
            for (int i = StartOffset; i <= Data.Length - sizeof(uint) && Data[i] != ProtocolConstants.NULL_TERMINATOR; i += sizeof(uint))
            {
                output[i] = BitConverter.ToUInt64(Data, i);
            }
            return output;
        }*/
    }
}
