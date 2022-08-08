using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scythe
{
    public enum ScopeKind
    {
        Local,
        Global,
        Function
    }

    public class Scope
    {
        public Scope? parent;
        public ScopeKind kind;
        public Dictionary<string, Symbol> symbols = new Dictionary<string, Symbol>();

        public Symbol? Lookup(string name)
        {
            if (symbols.ContainsKey(name))
                return symbols[name];

            if (parent != null)
                return parent.Lookup(name);

            return null;
        }

        public Scope(ScopeKind kind)
        {
            this.kind = kind;
        }
    }
}
