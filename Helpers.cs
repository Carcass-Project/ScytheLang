using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Scythe.Symbols;
using LLVMSharp.Interop;

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

        public static unsafe string SByteToStr(sbyte* str)
        {
            string ptr = Marshal.PtrToStringUTF8((IntPtr)str);
           
            return ptr;
        }

        public static unsafe DataType GetSYDataType(LLVMOpaqueType* type)
        {
         
           if(type == LLVM.Int1Type())
            return DataType.Bool;
           if (type == LLVM.Int32Type())
            return DataType.Int;
           if (type == LLVM.FloatType())
            return DataType.Float;
           if (type == LLVM.PointerType(LLVM.Int8Type(), 0))
            return DataType.String;

            return DataType.Void;
        }
    }
}
