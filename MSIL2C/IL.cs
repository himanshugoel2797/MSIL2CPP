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
            Process p = new Process();
            p.StartInfo.FileName = "monodis";
            p.StartInfo.Arguments = "--output=temp.txt \"" + file + "\"";
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.CreateNoWindow = true;
            p.Start();
            
            string toRet = "";
            while (!p.HasExited)
            {
                //toRet += p.StandardOutput.ReadLine();
            }
            toRet = File.ReadAllText("temp.txt");
#if DEBUG
            //File.WriteAllText("temp.txt", toRet);
#endif
            return toRet;
        }
    }
}
