using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCProtocol
{
    public static class Randomness
    {
        public static UInt64 NextUInt64(this Random rnd)
        {
            var buffer = new byte[sizeof(Int64)];
            rnd.NextBytes(buffer);
            return BitConverter.ToUInt64(buffer, 0);
        }

		public static void SplitToSecretShares(ulong[] elements, ulong[] sharesA, ulong[] sharesB)
		{
			if (elements == null)
            {
				return;
            }

			Random rnd = new Random();
			for(int i = 0; i < elements.Length; i++)
			{
				ulong firstShare = rnd.NextUInt64();
				sharesA[i] = firstShare;
				sharesB[i] = (elements[i] - firstShare + ulong.MaxValue + 1);
			}
		}

		public static string GenerateSessionId()
		{
			return Guid.NewGuid().ToString().Substring(0, 8);
		}

	}
}
