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
		public List<UInt16> serverAList { get; set; }
		public List<UInt16> serverBList { get; set; }
		public int randomRange;

		public DataService()
		{
			this.serverAList = new List<UInt16>();
			this.serverBList = new List<UInt16>();
			this.randomRange = 65536;
		}


		public void generateSecretShares(List<UInt16> input)
		{
			Console.WriteLine("Start generating secret shares");
			Random rnd = new Random();

			foreach (UInt16 i in input)
			{
				UInt16 shareA = (UInt16)rnd.Next(0, this.randomRange+1); // creates a number between 1 and 12
				this.serverAList.Add(shareA);
				UInt16 shareB = (UInt16)(i + this.randomRange - shareA);
				this.serverBList.Add(shareB);
			}
		}
	}
}
