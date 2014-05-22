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
#if !DEBUG
            if (args.Length == 0) return;
#endif

            List<string> funcs = new List<string>();
            List<MethodInfo> funcInfo = new List<MethodInfo>();
            List<string> includes = new List<string>();
            Dictionary<string, string> translations = new Dictionary<string, string>();
            MethodInfo db = null;
            object instance = null;

#if !DEBUG
            Assembly a = Assembly.LoadFile(System.IO.Path.GetFullPath(args[0]));
#else
            Assembly a = Assembly.LoadFile(System.IO.Path.GetFullPath("TestBinding.dll"));
#endif
            foreach (Type t in a.GetTypes())
            {
                if (t.Name != "BindingGen")
                {
                    MethodInfo[] methods = t.GetMethods(BindingFlags.Public | BindingFlags.Static);

                    Array.Sort(methods, delegate(MethodInfo methodInfo1, MethodInfo methodInfo2) { return methodInfo1.Name.CompareTo(methodInfo2.Name); });

                    foreach (MethodInfo m in methods)
                    {
                        funcs.Add(t.Namespace + "." + t.Name + "." + m.Name);
                        funcInfo.Add(m);
                    }
                }
                else
                {
                    db = t.GetMethod("GetTranslation");
                    instance = Activator.CreateInstance(t);
                }
            }

            foreach (string s in funcs)
            {
                if (instance != null && db != null)
                {
                    string[] data = (string[])db.Invoke(instance, new object[]{(object)s});
                    includes.Add(data[1]);
                    translations[s] = data[0];
                }
            }

            string cCode = "#include \"" + System.IO.Path.GetFileNameWithoutExtension(a.Location) + ".h\"\n";
            string hCode = "";
            foreach (string include in includes)
            {
                hCode += include + "\n";
            }

            cCode += "\n\n";
            hCode += "\n\n";

            for(int counter = 0; counter < funcs.Count; counter++)
            {
                string funcArgs = "(";
                foreach(ParameterInfo pi in funcInfo[counter].GetParameters())
                {
                    funcArgs += pi.ParameterType.Namespace + "::" + pi.ParameterType.Name + " " + pi.Name + ","; 
                }
                funcArgs = funcArgs.Remove(funcArgs.Length - 1) + ")";

                cCode += funcInfo[counter].ReturnType.Namespace + "::" + funcInfo[counter].ReturnType.Name + " " + funcs[counter].Replace(".", "::") + funcArgs
                    + "{ \n" + translations[funcs[counter]] + funcArgs + "; \n } \n";

               //TODO Generate header
            }
        }
    }
}
