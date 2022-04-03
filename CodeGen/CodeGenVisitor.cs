using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scythe.Symbols;
using Scythe.Nodes.Bound;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Scythe.CodeGen
{
    public class CodeGenVisitor
    {
        private readonly LLVMValueRef nullValue = new LLVMValueRef(IntPtr.Zero);

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly Dictionary<string, Pointer<LLVMOpaqueValue>> namedValues = new Dictionary<string, Pointer<LLVMOpaqueValue>>();

        private readonly Stack<Pointer<LLVMOpaqueValue>> valueStack = new Stack<Pointer<LLVMOpaqueValue>>();

        public CodeGenVisitor(LLVMModuleRef module, LLVMBuilderRef builder)
        {
            this.module = module;
            this.builder = builder;
        }

        public Stack<Pointer<LLVMOpaqueValue>> ResultStack { get { return valueStack; } }

        public object Visit(BoundExpression expr) => expr switch
        {
            BoundIntLiteralExpr => VisitIntegerNumber(expr as BoundIntLiteralExpr),
            BoundFloatLiteralExpr => VisitFloatNumber(expr as BoundFloatLiteralExpr),
            BoundVariableExpr => VisitVariableExpr(expr as BoundVariableExpr),
            BoundCallFunctionExpr => VisitCallFExpr(expr as BoundCallFunctionExpr),
            BoundBinaryExpr => VisitBinaryExpr(expr as BoundBinaryExpr),
            BoundStringLiteralExpr => VisitStringLiteral(expr as BoundStringLiteralExpr),
            _ => throw new Exception("[FATAL]: Expression that was attempted to be visited is invalid/unknown. '" + expr + "'."),
        };

        public object Visit(BoundStatement stmt) => stmt switch
        {
            BoundFunctionStatement => this.VisitFunction(stmt as BoundFunctionStatement),
            BoundExpressionStatement => this.VisitExprStmt(stmt as BoundExpressionStatement),
            BoundReturnStatement => this.VisitReturn(stmt as BoundReturnStatement),
            
            _ => throw new Exception("[FATAL]: Statement that was attempted to be visited is invalid/unknown. '"+stmt+"'."),
        };

        public void ClearResultStack()
        {
            valueStack.Clear();
        }

        public BoundStatement VisitExprStmt(BoundExpressionStatement stmt)
        {
            this.Visit(stmt.expr);
            return stmt;
        }

        public unsafe BoundExpression VisitIntegerNumber(BoundIntLiteralExpr expr)
        {
            Console.WriteLine("Test: " + expr.Literal);
            valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), (ulong)expr.Literal, 1));
            return expr;
        }

        public unsafe BoundExpression VisitFloatNumber(BoundFloatLiteralExpr expr)
        {
            Console.WriteLine("Test: " + expr.Literal);
            valueStack.Push(LLVM.ConstReal(LLVM.FloatType(), expr.Literal));
            return expr;
        }

        public unsafe BoundExpression VisitStringLiteral(BoundStringLiteralExpr expr)
        {
            Console.WriteLine("Test: " + expr.Literal);
            valueStack.Push(LLVM.ConstString(StrToSByte(expr.Literal), (uint)expr.Literal.Length, 0));
            return expr;
        }

        public unsafe BoundExpression VisitVariableExpr(BoundVariableExpr expr)
        {
            Pointer<LLVMOpaqueValue> value;

            if (namedValues.TryGetValue(expr.Name, out value))
            {
                valueStack.Push(value);
            }
            else
            {
                throw new Exception("Invalid/Unknown Variable Name.");
            }

            return expr;
        }

        public unsafe sbyte* StrToSByte(string str)
        {
            IntPtr ptr = Marshal.StringToHGlobalAnsi(str);
            sbyte* sby = (sbyte*)ptr;
            return sby;
        }

        public unsafe LLVMOpaqueType* ParameterType(Parameter mtr)
        {
            switch(mtr.DataType)
            {
                case DataType.Bool:
                    return LLVM.Int1Type();
                case DataType.Int:
                    return LLVM.Int32Type();
                case DataType.Float:
                    return LLVM.FloatType();
                case DataType.String:
                    return LLVM.PointerType(LLVM.Int8Type(), 0);
            }
            return LLVM.Int32Type();
        }

        public unsafe LLVMOpaqueType* DataTyToType(DataType mtr)
        {
            switch (mtr)
            {
                case DataType.Bool:
                    return LLVM.Int1Type();
                case DataType.Int:
                    return LLVM.Int32Type();
                case DataType.Float:
                    return LLVM.FloatType();
                case DataType.String:
                    return LLVM.PointerType(LLVM.Int8Type(), 0);
            }
            return LLVM.VoidType();
        }

        public unsafe LLVMOpaqueType*[] ParameterTypes(List<Parameter> mtrs)
        {
            var paramTypes = new LLVMOpaqueType*[mtrs.Count];

            for (int i = 0; i < mtrs.Count; i++)
                paramTypes[i] = ParameterType(mtrs[i]);

            return paramTypes;
        }

        public unsafe BoundExpression VisitBinaryExpr(BoundBinaryExpr expr)
        {
            this.Visit(expr.a);
            this.Visit(expr.b);

            LLVMOpaqueValue* r = valueStack.Pop();
            LLVMOpaqueValue* l = valueStack.Pop();

            LLVMOpaqueValue* n;
            sbyte* s = null;
            switch (expr.op)
            {
                case Operator.PLUS:
                    n = LLVM.BuildAdd(builder, l, r, s = StrToSByte("AddTMP"));
                    break;
                case Operator.MINUS:
                    n = LLVM.BuildSub(builder, l, r, s = StrToSByte("SubTMP"));
                    break;
                case Operator.MULTI:
                    n = LLVM.BuildMul(builder, l, r, s = StrToSByte("MulTMP"));
                    break;
                case Operator.DIV:
                    n = LLVM.BuildSDiv(builder, l, r, s = StrToSByte("DivTMP"));
                    break;
                default:
                    throw new Exception("Invalid Operator in Binary Expression.");
            }
            Marshal.FreeHGlobal((IntPtr)s);
            valueStack.Push(n);
            return expr;
        }

        public unsafe BoundExpression VisitCallFExpr(BoundCallFunctionExpr expr)
        {
            var callee = LLVM.GetNamedFunction(module, StrToSByte(expr.Name));
            if((IntPtr)callee == IntPtr.Zero)
            {
                throw new Exception("Unknown Function Called.");
            }
            if(LLVM.CountParams(callee) != expr.Arguments.Count)
            {
                throw new Exception("Incorrect number of arguments passed.");
            }

            var argCount = (uint)expr.Arguments.Count;
            var argsV = new LLVMOpaqueValue*[Math.Max(argCount, 1)];

            for(int i = 0; i < argCount; ++i)
            {
                this.Visit(expr.Arguments[i]);
                argsV[i] = valueStack.Pop();
            }

            fixed(LLVMOpaqueValue** pptr = argsV)
                valueStack.Push(LLVM.BuildCall(builder, callee, pptr, argCount, StrToSByte("CallTMP")));

            return expr;
        }

        public unsafe BoundStatement VisitFunction(BoundFunctionStatement node)
        {
            this.namedValues.Clear();



            
            var argumentCount = (uint)node.Parameters.Count;
            var arguments = new LLVMTypeRef[Math.Max(argumentCount, 1)];

            LLVMOpaqueValue* function = LLVM.GetNamedFunction(this.module, StrToSByte(node.Name));
            

            if (function != null)
            {
      
                if (LLVM.CountBasicBlocks(function) != 0)
                {
                    throw new Exception("redefinition of function.");
                }

    
                if (LLVM.CountParams(function) != argumentCount)
                {
                    throw new Exception("redefinition of function with different # args");
                }
            }
            else
            {
                for (int i = 0; i < argumentCount; ++i)
                {
                    arguments[i] = LLVM.DoubleType();
                }
                fixed (LLVMOpaqueType** pptr = ParameterTypes(node.Parameters))
                {
                    function = LLVM.AddFunction(this.module, StrToSByte(node.Name), LLVM.FunctionType(DataTyToType(node.Type), pptr, argumentCount, 0));
                }
                LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
            }

            for (int i = 0; i < argumentCount; ++i)
            {
                string argumentName = node.Parameters[i].Name;

                LLVMOpaqueValue* param = LLVM.GetParam(function, (uint)i);
                LLVM.SetValueName(param, StrToSByte(argumentName));

                this.namedValues[argumentName] = param;
            }

            this.valueStack.Push(function);


            LLVMOpaqueValue* function2 = this.valueStack.Pop();

            LLVM.PositionBuilderAtEnd(this.builder, LLVM.AppendBasicBlock(function2, StrToSByte("entry")));

            try
            {
                foreach (var x in node.Body.Body)
                    this.Visit(x);
            }
            catch (Exception)
            {
                LLVM.DeleteFunction(function2);
                throw;
            }

            

            LLVM.VerifyFunction(function2, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            this.valueStack.Push(function2);

            return node;
        }

        public unsafe BoundStatement VisitReturn(BoundReturnStatement stmt)
        {
            this.Visit(stmt.Value);
            LLVM.BuildRet(this.builder, this.valueStack.Pop());
            return stmt;
        }
    }
}
