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
            IL tmp;
#if DEBUG && TESTS
            tmp = new IL("Tests.dll");
#else
              tmp = new IL(args[0]);
#endif
            return;
        }
    }
}
