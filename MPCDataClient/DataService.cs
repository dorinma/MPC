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
		public List<ulong> serverAList { get; set; }
		public List<ulong> serverBList { get; set; }

		public DataService()
		{
			this.serverAList = new List<ulong>();
			this.serverBList = new List<ulong>();
		}


		/*public void GenerateSecretShares(List<ulong> input)
		{
			//Console.WriteLine("Start generating secret shares");
			if (input == null) return;

			Random rnd = new Random();

			foreach (ulong i in input)
			{
				ulong shareA = (ulong)rnd.Next(0, this.randomRange+1); // creates a number between 1 and 12
				this.serverAList.Add(shareA);
				ulong shareB = (ulong)(i + this.randomRange - shareA);
				this.serverBList.Add(shareB);
			}
		}*/
	}
}
