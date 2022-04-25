using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCProtocol
{
    public static class Randomness
    {
        public static ulong NextUInt64(this Random rnd)
        {
            var buffer = new byte[sizeof(ulong)];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

		public static void SplitToSecretShares(ulong[] elements, out ulong[] sharesA, out ulong[] sharesB)
		{
			if (elements == null)
            {
                sharesA = null;
                sharesB = null;
				return;
            }

            sharesA = new ulong[elements.Length];
            sharesB = new ulong[elements.Length];

			Random rnd = new Random();
			for(int i = 0; i < elements.Length; i++)
			{ 
                // element = shareA - shareB
                // shareB = shareA - element
				ulong firstShare = rnd.NextUInt64();
				sharesA[i] = firstShare;
				sharesB[i] = firstShare - elements[i];
				//sharesB[i] = (elements[i] - firstShare + ulong.MaxValue + 1);
			}
		}

        public static ulong[] CreateRandomMasks(int count)
        {
            Random rnd = new Random();
            ulong[] masks = new ulong[count];
            for (int i = 0; i < count; i++)
            {
                masks[i] = rnd.NextUInt64();
            }
            return masks;
        }

        public static string GenerateSessionId()
		{
			return Guid.NewGuid().ToString().Substring(0, 8);
		}

	}
}
