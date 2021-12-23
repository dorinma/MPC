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
            string IP = "172.16.205.108"; //eden 100.64.182.7
            Connect(IP);
            Communication.SendRequest("Hey bitchessss");
            while(true) {
                if (Console.Read() == 'q')
                    break;
            }
        }
        
        private static void Connect(string IP)
        {
            Communication.Connect(IP);
        }
        
    }
}
