using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Scythe
{
    public static class Helpers
    {
        public static unsafe sbyte* StrToSByte(string str)
        {
            IntPtr ptr = Marshal.StringToHGlobalAnsi(str);
            sbyte* sby = (sbyte*)ptr;
            return sby;
        }
    }
}
