using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scythe.Symbols
{
    public enum DataType
    {
        Int,
        Float,
        Uint,
        Bool,
        String
    }

    public class FunctionSymbol : Symbol
    {
        public string Name;
        public Nodes.BlockStatement Body;
        
        public DataType returnType;
    }
}
