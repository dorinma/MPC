using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCDataClient
{
    public class DataService
    {
        static void Main(string[] args)
        {
            string IP = "100.64.182.7"; //eden
            Connect(IP);
            Communication.SendRequest(IP, "Hey bitchessss");
        }
        
        private static void Connect(string IP)
        {
            Communication.Connect(IP);
        }
        
    }
}
