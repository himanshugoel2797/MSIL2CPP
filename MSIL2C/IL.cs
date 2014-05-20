using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIL2C
{
    public class IL
    {
        public static string GetMSIL(string file)
        {
            Process.Start("monodis", "--output=temp.txt \"" + file + "\"");
            string toRet = File.ReadAllText("temp.txt");
#if !DEBUG
            File.Delete("temp.txt");
#endif
            return toRet;
        }
    }
}
