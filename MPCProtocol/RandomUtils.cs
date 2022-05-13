using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCProtocol
{
    public static class RandomUtils
    {
        public static uint NextUInt32(this Random rnd)
        {
            var buffer = new byte[sizeof(uint)];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static void SplitToSecretShares(uint[] elements, out uint[] sharesA, out uint[] sharesB)
		{
			if (elements == null)
            {
                sharesA = null;
                sharesB = null;
				return;
            }

            sharesA = new uint[elements.Length];
            sharesB = new uint[elements.Length];

			Random rnd = new Random();
			for(int i = 0; i < elements.Length; i++)
			{ 
                // element = shareA - shareB
                // shareB = shareA - element
				uint firstShare = rnd.NextUInt32();
				sharesA[i] = firstShare;
				sharesB[i] = firstShare - elements[i];
				//sharesB[i] = (elements[i] - firstShare + uint.MaxValue + 1);
			}
		}

        public static uint[] CreateRandomMasks(int count)
        {
            Random rnd = new Random();
            uint[] masks = new uint[count];
            for (int i = 0; i < count; i++)
            {
                masks[i] = rnd.NextUInt32();
            }
            return masks;
        }

        public static string GenerateSessionId()
		{
			return Guid.NewGuid().ToString().Substring(0, 8);
		}

	}
}
