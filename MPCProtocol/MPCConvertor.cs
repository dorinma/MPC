using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCProtocol
{
    public class MPCConvertor
    {
        public static List<UInt16> BytesToList(byte[] Data, int StartOffset)
        {
            List<UInt16> output = new List<UInt16>();
            for (int i = StartOffset; i <= Data.Length - sizeof(UInt16) && Data[i] != ProtocolConstants.NULL_TERMINATOR; i += sizeof(UInt16))
            {
                output.Add(BitConverter.ToUInt16(Data, i));
            }
            return output;
        }
    }
}
