using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MPCDataClient
{
	
	public class DataService
	{
		public List<UInt16> ServerAList;
		public List<UInt16> ServerBList;
		public int operation;
		public int randomRange;

		public DataService()
		{
			this.ServerAList = new List<UInt16>();
			this.ServerBList = new List<UInt16>();
			this.randomRange = 65535;
		}


		public void generateSecretShares(List<UInt16> input)
		{
			Random rnd = new Random();

			foreach (UInt16 i in input)
			{
				UInt16 shareA = (UInt16)rnd.Next(0, this.randomRange+1); // creates a number between 1 and 12
				this.ServerAList.Add(shareA);
				UInt16 shareB = (UInt16)(i + this.randomRange - shareA);
				this.ServerBList.Add(shareB);
			}
		}
	}
}
