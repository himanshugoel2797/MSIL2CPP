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
        Stack<int> backupOffset;
        Stack<bool> @else;
        StreamWriter c, h;
        StringBuilder cs, hs;
        public delegate string[] IL2C(string line);
        Dictionary<string, IL2C> Translators;
        Dictionary<string, string> Vars;
        int lineNum = 0,tmpVarCount = 0;
        long curOffset = 0;
        string[] lines;

        public DirectCodeGenerator() 
        {
            RecurC = new Stack<string>();
            RecurH = new Stack<string>();
            Args = new Stack<string>();
            Vars = new Dictionary<string, string>();
            Translators = new Dictionary<string, IL2C>();
            backupOffset = new Stack<int>();
            @else = new Stack<bool>();
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
                if (s.StartsWith("stloc.s"))
                {
                    s = "stloc." + s.Remove("stloc.s");
                }

                string toRet = Vars["V_" + s.Remove("stloc.").Trim()] + " V_" + s.Remove("stloc.").Trim() + " = " + Args.Pop() + ";";
                Vars["V_" + s.Remove("stloc.").Trim()] = "";
                return new string[] {toRet, string.Empty};
            };

            Translators["call"] = (string s) =>
            {
                string func = s.Remove("call").Remove(")").Trim();
                string retType = func.Split(' ')[0];
                func = func.Replace(".", "::").Split(' ')[1];

                List<string> tmp = Args.ToList();
                tmp.Reverse();
                while(tmp.Count > 0){
                    func += tmp[0] + ",";
                    tmp.RemoveAt(0);
                }
                Args = new Stack<string>(tmp);

                func = func.Remove(func.Length - 1) + ");";

                if (retType != "System.Void")
                {
                    func = retType + " v_" + tmpVarCount.ToString() + " = " + func;
                    Args.Push("v_" + tmpVarCount);
                    tmpVarCount++;
                }

                return new string[] {func, string.Empty};
            };

            Translators["ldloc"] = (string s) =>
            {
                if (s.StartsWith("ldloca.s"))
                {
                    Args.Push("V_" + s.Remove("ldloca.s").Trim());
                }
                else if (s.StartsWith("ldloc.s")) Args.Push("V_" + s.Remove("ldloc.s").Trim());
                else if (s.StartsWith("ldloc."))
                {
                    Args.Push("V_" + s.Remove("ldloc.").Trim());
                }
                return Empty;
            };

            Translators["ldc.i4."] = (string s) =>
            {
                s = s.Remove("ldc.i4.");

                if (s.StartsWith("s")) Args.Push(s.Remove("s").Trim());
                else if (s.StartsWith("m1")) Args.Push("-1");
                else Args.Push(s);
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

            Translators["cgt"] = (string s) =>
            {
                string tmp = "bool v_" + tmpVarCount + " = (" + Args.Pop() + " < " + Args.Pop() + "); ";
                Args.Push("v_" + tmpVarCount);
                tmpVarCount++;
                return new string[] { tmp, string.Empty };
            };

            Translators["ceq"] = (string s) =>
            {

                string tmp = "bool v_" + tmpVarCount + " = (" + Args.Pop() + " == " + Args.Pop() + "); ";
                Args.Push("v_" + tmpVarCount);
                tmpVarCount++;
                return new string[] { tmp, string.Empty };
            };

            Translators["brtrue"] = (string s) =>
            {
                s = s.Remove("brtrue");
                if (s.StartsWith(".s"))
                {
                    backupOffset.Push(lineNum + 1);
                    @else.Push(true);
                    lineNum = GetIndexOfOffset(long.Parse(s.Remove(".s").Trim())) - 1;
                }

                RecurC.Push("}");
                return new string[] {"if (" + Args.Pop() + " != 0){", string.Empty};
            };
            #endregion
        }

        public int GetIndexOfOffset(long offset)
        {
            for (int tmp = 0; tmp < lines.Length; tmp++)
            {
                if (lines[tmp].StartsWith(offset.ToString("D4"))) return tmp;
            }
            return -1;
        }

        public void Generate(MethodInfo[] mi)
        {

            string @namespace = mi[0].DeclaringType.Namespace;
            string @class = mi[0].DeclaringType.Name;

            c = new StreamWriter(Path.Combine("src", @namespace + "_" + @class + ".cpp"), false);
            h = new StreamWriter(Path.Combine("src", @namespace + "_" + @class + ".h"), false);

            cs = new StringBuilder();
            hs = new StringBuilder();

            //Must include headers - translate framework types and functions
            hs.Append("#include \"..\\Framework\\types.h\"\n");

            hs.Append("namespace " + @namespace + " { \n class " + @class + "{ \n");
            RecurH.Push("}");
            RecurH.Push("};");

            cs.AppendLine("#include \"" + @namespace + "_" + @class + ".h\"");

            for (lineNum = 0; lineNum < mi.Length; lineNum++)
            {
                MethodInfo m = mi[lineNum];
                MethodBodyReader r = new MethodBodyReader(m);

                //Save the temporary MSIL
                string methodname = m.Name;
                lines = r.GetBodyCode().Split('\n');
                File.WriteAllText(Path.Combine("src/MSIL", @namespace + "_" + @class + "_" + methodname + ".MSIL"), r.GetBodyCode());

                //Parse and setup function parameters
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

                //Setup function declaration in header
                hs.AppendFormat("{0} {1}({2});", m.ReturnType.Namespace + "::" + m.ReturnType.Name, m.Name, par);
                
                //Setup function definition in source
                cs.AppendLine(m.ReturnType.Namespace + "::" + m.ReturnType.Name + " " + @namespace + "::" + @class + "::" + m.Name + "(" + par + "){" );
                RecurC.Push("}");

                //Define all variables
                foreach (LocalVariableInfo locals in m.GetMethodBody().LocalVariables)
                {
                    Vars.Add("V_" + locals.LocalIndex, locals.LocalType.Namespace + "::" + locals.LocalType.Name);
                    cs.AppendLine(locals.LocalType.Namespace + "::" + locals.LocalType.Name + " " + "V_" + locals.LocalIndex + ";");
                    Vars["V_" + locals.LocalIndex] = "";
                }

                bool exit = false;
                #region Generate code
                lineNum = 0;
                do
                {
                    for (; lineNum < lines.Length; lineNum++)
                    {
                        //skip empty lines
                        if (lines[lineNum].Trim() != string.Empty)
                        {
                            //Remove all whitespace from the string
                            string line = lines[lineNum].Trim();

                            //Get the offset
                            curOffset = long.Parse(line.Split(':')[0]);
                            line = line.Remove(line.Split(':')[0] + ":").Trim();

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

                    if (backupOffset.Count > 0)
                    {
                        lineNum = backupOffset.Pop();
                    }
                    else
                    {
                        exit = true;
                    }

                    if (@else.Count > 0)
                    {
                        if (@else.Pop())
                        {
                            cs.AppendLine(RecurC.Pop() + " else { ");
                        }
                    }
                } while (!exit);
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
