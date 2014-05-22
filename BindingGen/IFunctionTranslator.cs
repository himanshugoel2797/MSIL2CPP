using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BindingGen
{
    public interface IFunctionTranslator
    {
        string GetTranslation(string funcName);
    }
}
