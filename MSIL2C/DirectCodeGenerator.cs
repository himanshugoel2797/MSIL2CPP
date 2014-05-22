using SDILReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MSIL2C
{
    public class DirectCodeGenerator
    {
        static string[] Empty = new string[] {string.Empty, string.Empty};

        Stack<string> RecurC, RecurH, Args;
        StreamWriter c, h;
        StringBuilder cs, hs;
        public delegate string[] IL2C(string line);
        Dictionary<string, IL2C> Translators;
        Dictionary<string, string> Vars;
        int lineNum = 0;

        public DirectCodeGenerator() 
        {
            RecurC = new Stack<string>();
            RecurH = new Stack<string>();
            Args = new Stack<string>();
            Vars = new Dictionary<string, string>();
            Translators = new Dictionary<string, IL2C>();
            #region Translators
            Translators["nop"] = (string s) =>
            {
                return Empty;
            };

            Translators["ldstr"] = (string s) =>
            {
                Args.Push(s.Remove("ldstr").Trim());
                return Empty;
            };
            
            Translators["stloc"] = (string s) =>
            {
                string toRet = Vars["V_" + s.Remove("stloc.").Trim()] + " V_" + s.Remove("stloc.").Trim() + " = " + Args.Pop() + ";";
                Vars["V_" + s.Remove("stloc.").Trim()] = "";
                return new string[] {toRet, string.Empty};
            };

            Translators["ldloc"] = (string s) =>
            {
                if (s.StartsWith("ldloca.s"))
                {
                    Args.Push("V_" + s.Remove("ldloca.s").Trim());
                }
                else if (s.StartsWith("ldloc."))
                {
                    Args.Push("V_" + s.Remove("ldloc.").Trim());
                }
                return Empty;
            };

            Translators["ldc.i4.s"] = (string s) =>
            {
                Args.Push(s.Remove("ldc.i4.s").Trim());
                return Empty;
            };

            Translators["ldc.i4.m1"] = (string s) =>
            {
                Args.Push("-1");
                return Empty;
            };

            Translators["add"] = (string s) =>
            {
                Args.Push(Args.Pop() + " + " + Args.Pop());
                return Empty;
            };

            Translators["sub"] = (string s) =>
            {
                Args.Push("-" + Args.Pop() + " + " + Args.Pop());
                return Empty;
            };

            Translators["mul"] = (string s) =>
            {
                Args.Push(Args.Pop() + " * " + Args.Pop());
                return Empty;
            };

            Translators["rem"] = (string s) =>
            {
                Args.Push(Args.Pop() + " % " + Args.Pop());
                return Empty;
            };

            Translators["div"] = (string s) =>
            {
                Args.Push("(1/" + Args.Pop() + ") * " + Args.Pop());
                return Empty;
            };

            Translators["dup"] = (string s) =>
            {
                string tmp = Args.Pop();
                Args.Push(tmp);
                Args.Push(tmp);
                return Empty;
            };
            #endregion
        }

        public void Generate(MethodInfo[] mi)
        {

            string @namespace = mi[0].DeclaringType.Namespace;
            string @class = mi[0].DeclaringType.Name;

            c = new StreamWriter(Path.Combine("src", @namespace + "_" + @class + ".cpp"), false);
            h = new StreamWriter(Path.Combine("src", @namespace + "_" + @class + ".h"), false);

            cs = new StringBuilder();
            hs = new StringBuilder();

            hs.Append("namespace " + @namespace + " { \n class " + @class + "{ \n");
            RecurH.Push("}");
            RecurH.Push("}");

            for (lineNum = 0; lineNum < mi.Length; lineNum++)
            {
                MethodInfo m = mi[lineNum];
                MethodBodyReader r = new MethodBodyReader(m);

                string methodname = m.Name;
                string[] lines = r.GetBodyCode().Split('\n');
                File.WriteAllText(Path.Combine("src/MSIL", @namespace + "_" + @class + ".MSIL"), r.GetBodyCode());

                ParameterInfo[] @params = m.GetParameters();
                string par = "";
                if (@params.Length != 0)
                {
                    par = @params[0].ParameterType.Name + " v_0";
                    Vars["v_0"] = @params[0].ParameterType.Name;
                    for (int tmp = 1; tmp < @params.Length; tmp++)
                    {
                        par += ", " + @params[tmp].ParameterType.Name + " v_" + tmp.ToString();
                        Vars["v_" + tmp.ToString()] = @params[tmp].ParameterType.Name;
                    }
                }
                hs.AppendFormat("{0} {1}({2});", m.ReturnType.Name, m.Name, par);
                
                cs.AppendLine(m.ReturnType.Name + " " + @namespace + "::" + @class + "::" + m.Name + "(" + par + "){" );
                RecurC.Push("}");

                foreach (LocalVariableInfo locals in m.GetMethodBody().LocalVariables)
                {
                    Vars.Add("V_" + locals.LocalIndex, locals.LocalType.Namespace + "::" + locals.LocalType.Name);
                    cs.AppendLine(locals.LocalType.Namespace + "::" + locals.LocalType.Name + " " + "V_" + locals.LocalIndex + ";");
                }

                #region Generate code
                for (lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    //skip empty lines
                    if (lines[lineNum].Trim() != string.Empty)
                    {
                        //Remove all whitespace from the string
                        string line = lines[lineNum].Trim();
                        //Check if the line is of any interest to us
                        foreach (string key in Translators.Keys)
                        {
                            //if it is, call the appropriate handler and update the tokens
                            if (line.StartsWith(key))
                            {
                                string[] f = Translators[key](line);
                                if (!string.IsNullOrWhiteSpace(f[0])) cs.AppendLine(f[0]);
                                if (!string.IsNullOrWhiteSpace(f[1])) hs.AppendLine(f[1]);
                            }
                        }
                    }
                }
                #endregion

                #region Header Recursion tree
                while (RecurH.Count > 0)
                {
                    hs.AppendLine(RecurH.Pop());
                }
                h.Write(hs);
                h.Flush();
                #endregion

                #region Code Recursion tree
                while (RecurC.Count > 0)
                {
                    cs.AppendLine(RecurC.Pop());
                }
                c.Write(cs);
                c.Flush();
                #endregion


                Process.Start("AStyle", "--style=allman --recursive  src/*.cpp  src/*.h");
            }


        }
    }
}
