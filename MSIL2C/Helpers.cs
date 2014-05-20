using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSIL2C
{
    public static class Helpers
    {
        public static string Remove(this string s, string toRemove)
        {
            return s.Replace(toRemove, string.Empty);
        }
    }
}
