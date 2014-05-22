using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestBinding
{
    public class Terminal
    {
        public static void Printf(string line)
        {
        }
    }

    public class BindingGen
    {
        static Dictionary<string, string> translations;
        static Dictionary<string, string> includes;
        static BindingGen()
        {
            translations = new Dictionary<string, string>();
            includes = new Dictionary<string, string>();

            translations.Add("TestBinding.Terminal.Printf", "printf");
            includes.Add("TestBinding.Terminal", "#include <stdio.h>");

        }
        public static string[] GetTranslation(string name)
        {
            return new string[] { translations[name], includes[name.Split('.')[0] + "." + name.Split('.')[1]] };    
        }
    }
}
