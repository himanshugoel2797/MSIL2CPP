using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSIL2C
{
    public class CodeGenerator
    {

        Stack<string> DepthF, DepthH, ArgStack;
        public delegate string IL2C(string line);
        Dictionary<string, IL2C> ILTranslators;
        Dictionary<string, string> Vars;
        StreamWriter f, h;
        int tmpCount = 0;

        public CodeGenerator()
        {
            ILTranslators = new Dictionary<string, IL2C>();

            #region IL Translators
            ILTranslators["nop"] = (string s) =>
            {
                return string.Empty;
            };
            ILTranslators["call"] = (string s) =>
            {
                #region Call handler
                string function = s.Remove("call").Trim();
                string[] args = null;
                bool earlyReturn = false;

                //remove the library name
                if (function.Contains('[') && function.Contains(']')) function = function.Remove(function.IndexOf('['), function.IndexOf(']') - function.IndexOf('[') + 1).Trim();
                function = function.Replace(".", "::");

                //If the call is an object instance call
                if (function.Split(' ')[0].Trim() == "instance")
                {
                    function.Remove("instance");

                    //We need a temporary variable to store the temporary return value
                    if (function.Split(' ')[1].Trim() != "void") function = function.Split(' ')[1] + " v_" + tmpCount.ToString() + " = ";
                    function += ArgStack.Pop() + "." + s.Split(':')[2];
                    ArgStack.Push("v_" + tmpCount.ToString());
                    tmpCount++;
                    earlyReturn = true;
                }

                //Separate the args from the rest of the function call for separate handling
                args = function.Substring(function.IndexOf('('), function.IndexOf(')') - function.IndexOf('(') + 1).Trim().Remove("(").Remove(")").Split(',');
                function = function.Remove(function.IndexOf('('), function.IndexOf(')') - function.IndexOf('(') + 1).Trim();

                //Add the arguments from the stack
                var tmpArgStack = new Stack<string>(ArgStack);  //MSIL seems to reverse the argument order from the stack
                var argList = new List<string>(ArgStack);       //We need a temporary list to make sure we don't mess up anything

                function += "(";    //Setup opening bracket
                if (args.Length > 0 && args[0] != "")
                {
                    //Setup the arguments if any
                    for (int counter = 0; counter < args.Length; counter++)
                    {
                        string arg = tmpArgStack.Pop();
                        argList.Remove(arg);    //Remove the argument from the original stack too
                        function += arg + ",";
                    }
                    ArgStack = new Stack<string>(argList);  //rebuild the stack so we don't miss anything
                }
                else
                {
                    function += ",";    //Just append a random character
                }
                function = function.Remove(function.Length - 1) + ");";

                //Return without setting up return values
                if (earlyReturn) return function;

                //Setup the return value cast
                function = function.Replace(function.Split(' ')[0], "(" + function.Split(' ')[0] + ")");

                //Remove extra data
                function = function.Remove("class").Trim();

                return function;
                #endregion
            };
            ILTranslators["ldstr"] = (string s) =>
            {
                ArgStack.Push(s.Remove("ldstr").Trim());
                return string.Empty;
            };
            ILTranslators["stloc"] = (string s) =>
            {
                return Vars["V_" + s.Remove("stloc.").Trim()] + " V_" + s.Remove("stloc.").Trim() + " = " + ArgStack.Pop() + ";";
            };
            ILTranslators["ldloc"] = (string s) =>
            {
                if (s.StartsWith("ldloca.s"))
                {
                    ArgStack.Push("V_" + s.Remove("ldloca.s").Trim());
                }
                else if(s.StartsWith("ldloc."))
                {
                    ArgStack.Push("V_" + s.Remove("ldloc.").Trim());
                }
                    return string.Empty;
            };
            ILTranslators["ldc.i4.s"] = (string s) =>
            {
                ArgStack.Push(s.Remove("ldc.i4.s").Trim());
                return string.Empty;
            };
            #endregion
        }

        public string GenerateCode(string xml)
        {
            DepthF = new Stack<string>();
            DepthH = new Stack<string>();
            ArgStack = new Stack<string>();

            StringBuilder final = new StringBuilder();

            f = null;
            h = null;

            string @namespace = "";
            string @class = "";

            #region Usings and stuff
            using (MemoryStream ms = new MemoryStream(Encoding.ASCII.GetBytes(xml)))
            {
                using (XmlReader doc = XmlReader.Create(ms))
                {
                    while (doc.Read())
                    {
                        if (doc.IsStartElement())
                        {
            #endregion

                            switch (doc.Name)
                            {
                                case "namespace":
                                    #region Files
                                    if (!Directory.Exists("src")) Directory.CreateDirectory("src");
                                    Array.ForEach(Directory.GetFiles(@"src"), delegate(string path) { File.Delete(path); });
                                    f = new StreamWriter(Path.Combine("src", doc["NAME"] + ".cpp"));
                                    h = new StreamWriter(Path.Combine("src", doc["NAME"] + ".h"));
                                    #endregion

                                    f.WriteLine("#include \"" + doc["NAME"] + ".h\"");
                                    @namespace = doc["NAME"];
                                    h.WriteLine("namespace " + doc["NAME"] + "{");
                                    DepthH.Push("}");

                                    break;
                                case "class":
                                    h.WriteLine("class " + doc["NAME"] + "{");
                                    @class = doc["NAME"];
                                    DepthH.Push("}");
                                    Vars = new Dictionary<string, string>();
                                    break;
                                case "method":
                                    h.WriteLine(doc["VISIBILITY"] + ":\n\t" + doc["SCOPE"] + " " + doc["RETURN"] + " " + doc["NAME"] + ";");

                                    f.WriteLine(doc["RETURN"] + " " + @namespace + "::" + @class + "::" + doc["NAME"] + "{");
                                    DepthF.Push("}");
                                    break;
                                case "VAR":
                                    Vars.Add(doc["NAME"], doc["TYPE"]);
                                    break;
                                case "IL":
                                    foreach (string key in ILTranslators.Keys)
                                    {
                                        //if it is, call the appropriate handler and update the tokens
                                        if (doc["instruction"].StartsWith(key))
                                        {
                                            string output = ILTranslators[key](doc["instruction"]);
                                            if (!string.IsNullOrWhiteSpace(output)) f.WriteLine(output);
                                        }
                                    }
                                    break;
                            }

                            #region Usings and stuff
                        }
                    }
                }
            }
                            #endregion

            #region Write all the ending braces
            while (DepthF.Count > 0)
            {
                f.WriteLine(DepthF.Pop());
            }

            while (DepthH.Count > 0)
            {
                h.WriteLine(DepthH.Pop());
            }
            #endregion

            f.Dispose();
            h.Dispose();

            Process.Start("AStyle", "--style=allman --recursive  src/*.cpp  src/*.h");

            return final.ToString();
        }
    }
}
