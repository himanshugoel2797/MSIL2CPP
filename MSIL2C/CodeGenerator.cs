using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace MSIL2C
{
    public class CodeGenerator
    {
        public CodeGenerator() { }

        Stack<string> DepthF, DepthH;

        public string GenerateCode(string xml)
        {
            DepthF = new Stack<string>();
            DepthH = new Stack<string>();

            StringBuilder final = new StringBuilder();

            StreamWriter f = null, h = null;

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
                                    if(!Directory.Exists("src"))Directory.CreateDirectory("src");
                                    Array.ForEach(Directory.GetFiles(@"src"), delegate(string path) { File.Delete(path); });
                                    f = new StreamWriter(Path.Combine("src", doc["NAME"] + ".cpp"));
                                    h = new StreamWriter(Path.Combine("src", doc["NAME"] + ".h"));
                                    #endregion

                                    f.WriteLine("#include \"" + doc["NAME"] + ".h\"");
                                    f.WriteLine("namespace " + doc["NAME"] + "{");
                                    DepthF.Push("}");

                                    h.WriteLine("namespace " + doc["NAME"] + "{");
                                    DepthH.Push("}");

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

            return final.ToString();
        }
    }
}
