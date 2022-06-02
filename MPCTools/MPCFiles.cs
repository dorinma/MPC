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
            var csv = new StringBuilder();
            for (int i = 0; i < res.Length; i++)
            {
                csv.Append(res[i]);
            }

            File.WriteAllText(fileName, csv.ToString());
        }
    }
}
