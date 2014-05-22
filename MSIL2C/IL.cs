using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SDILReader;

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

        public IL(string file)
        {
            //Use reflection + get method body to simplify the parsing process
            //Use reflection to get namespace, class and methodinfo (arguments and everything)
            //Use SDILReader to convert the method into disassembled MSIL
            //Parse and generate code from it
            Assembly a = Assembly.LoadFile(Path.GetFullPath(file));
            foreach (Type t in a.GetTypes())
            {
                if (t.Name != "BindingGen")
                {
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);

                    Array.Sort(methods, delegate(MethodInfo methodInfo1, MethodInfo methodInfo2) { return methodInfo1.Name.CompareTo(methodInfo2.Name); });

                    foreach (MethodInfo m in methods)
                    {
                        Globals.LoadOpCodes();
                        DirectCodeGenerator g = new DirectCodeGenerator();
                        g.Generate(methods);
                    }
                }
            }
        }


    }
}
