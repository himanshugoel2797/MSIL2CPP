#define TESTS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIL2C
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = "";
#if DEBUG && TESTS
            code = IL.GetMSIL("Tests.dll");
#elif DEBUG
            code = File.ReadAllText("tests.txt");
#endif
            Tokenizer t = new Tokenizer();
            string xml = t.Tokenize(code);
            File.WriteAllText("output.txt", xml);
#if DEBUG && TESTS
            Process.Start("notepad.exe", "temp.txt");
            Process.Start("notepad.exe", "output.txt");
#endif
            CodeGenerator gen = new CodeGenerator();
            string final = gen.GenerateCode(xml);
            File.WriteAllText("final.txt", final);
#if DEBUG
            Process.Start("notepad.exe", "final.txt");
#endif
        }
    }
}
