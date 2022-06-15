using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPCTools
{
    public static class MPCFiles
    {
        public static void writeToFile(uint[] res, String fileName)
        {
            fileName = Path.Combine(@"..\\..\\..\\Results", fileName);
            var csv = new StringBuilder();
            for (int i = 0; i < res.Length; i++)
            {
                csv.Append(res[i]);
                csv.Append("\n");
            }
            using (StreamWriter sw = File.CreateText(fileName)) { 
              
                sw.Write(csv.ToString());
            }
        }
    }
}
