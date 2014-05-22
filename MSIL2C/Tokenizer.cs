using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIL2C
{
    public class Tokenizer
    {
        public delegate string Token(string line);
        Dictionary<string, Token> Tokens;
        Stack<string> Depth;

        public Tokenizer()
        {
            Tokens = new Dictionary<string, Token>();
            #region Tokens
            Tokens[".namespace"] = (string s) => {
                string name = s.Remove(".namespace").Trim();
                Depth.Push("</namespace>");
                return "<namespace NAME=\"" + name + "\">";
            };
            Tokens[".class"] = (string s) =>
            {
                string[] tmp = s.Split(' ');
                Depth.Push("</class>");
                return "<class NAME=\"" + tmp[tmp.Length - 1] + "\">";
            };
            Tokens[".method"] = (string s) =>
            {
                s = s.Remove(".method").Remove("hidebysig").Trim();
                string[] nxtLine = (s + " " + PeekNextLine().Remove("cil managed").Remove(0, 7).Trim()).Split(' ');
                Depth.Push("</method>");
                string name = "";
                for (int c = 3; c < nxtLine.Length; c++)
                {
                    name += nxtLine[c];
                }
                return "<method VISIBILITY=\"" + nxtLine[0] + "\" RETURN=\"" + nxtLine[2] + "\" SCOPE=\"" + nxtLine[1] + "\" NAME=\"" + name + "\">";
            };
            Tokens["IL_"] = (string s) =>
                {
                    return "<IL instruction=\"" + s.Remove(s.Split(':')[0] + ":").Trim() + "\"></IL>";
                };
            Tokens["}"] = (string s) =>
                {
                    //We're out one level
                    if (Depth.Count != 0) return Depth.Pop();
                    else return "";
                };

            Tokens[".locals init"] = (string s) =>
                {
                    //Get everything on one line
                    string vars = PeekNextLine();
                    while (!vars.EndsWith(")"))
                    {
                        vars += (char)0xff + PeekNextLine().Trim();
                    }

                    //Remove extra things
                    vars = vars.Remove(vars.Length - 1);

                    //Separate all the variable declarations
                    string[] tmpA = vars.Split((char)0xff);

                    //Generate the xml code for all the variable definitions
                    string final = "";
                    for (int c = 0; c < tmpA.Length; c++)
                    {
                        if (tmpA[c].EndsWith(",")) tmpA[c] = tmpA[c].Remove(tmpA[c].Length - 1);
                        final += "<VAR TYPE=\"" + tmpA[c].Split('\t')[0] + "\" NAME=\"" + tmpA[c].Split('\t')[1] + "\"></VAR>\n" ;
                    }

                    return final;
                };
            #endregion
        }


        int lineNum = 0;
        int peekLineNum = 0;
        string[] lines;

        public string PeekNextLine()
        {
            peekLineNum++;
            return lines[lineNum + peekLineNum].Replace("\"", "\\\"").Trim();
        }

        public string Tokenize(string code)
        {
            lines = code.Split('\n');
            StringBuilder final = new StringBuilder();
            Depth = new Stack<string>();

            final.Append("<MSIL>\r\n");

            for (lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                //skip empty lines
                if (lines[lineNum].Trim() != string.Empty)
                {
                    //Remove all whitespace from the string
                    string line = lines[lineNum].Replace("\"", "&quot;").Trim();
                    //Check if the line is of any interest to us
                    foreach (string key in Tokens.Keys)
                    {
                        //Reset the peek variables
                        peekLineNum = 0;

                        //if it is, call the appropriate handler and update the tokens
                        if (line.StartsWith(key))
                        {
                            string f = Tokens[key](line);
                            if(!string.IsNullOrWhiteSpace(f))final.AppendLine(f);
                        }
                    }
                }
            }

            final.Append("</MSIL>");
            return final.ToString();
        }


    }
}
