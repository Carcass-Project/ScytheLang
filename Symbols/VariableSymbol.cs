using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scythe.Symbols
{
    public class VariableSymbol : Symbol
    {
        public string Name = "";
        public object Value = null;
        public bool isConst = false;
    }
}
