using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BindingGen
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;

            List<string> funcs = new List<string>();
            MethodInfo db;
            object instance;

            Assembly a = Assembly.Load(args[0]);
            foreach (Type t in a.GetTypes())
            {
                if (t.Name != "BindingGen")
                {
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public);

                    Array.Sort(methods, delegate(MethodInfo methodInfo1, MethodInfo methodInfo2) { return methodInfo1.Name.CompareTo(methodInfo2.Name); });

                    foreach (MethodInfo m in methods)
                    {
                        funcs.Add(t.Namespace + "." + t.Name + "." + m.Name);
                    }
                }
                else
                {
                    db = t.GetMethod("GetTranslation");
                    instance = Activator.CreateInstance(t);
                }
            }
        }
    }
}
