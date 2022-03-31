using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scythe.Symbols;
using Scythe.Nodes.Bound;
using LLVMSharp;

namespace Scythe.CodeGen
{
    public class CodeGenVisitor
    {
        private readonly LLVMValueRef nullValue = new LLVMValueRef(IntPtr.Zero);

        private readonly LLVMModuleRef module;

        private readonly LLVMBuilderRef builder;

        private readonly Dictionary<string, LLVMValueRef> namedValues = new Dictionary<string, LLVMValueRef>();

        private readonly Stack<LLVMValueRef> valueStack = new Stack<LLVMValueRef>();

        public CodeGenVisitor(LLVMModuleRef module, LLVMBuilderRef builder)
        {
            this.module = module;
            this.builder = builder;
        }

        public Stack<LLVMValueRef> ResultStack { get { return valueStack; } }

        public object Visit(BoundExpression expr) => expr switch
        {
            BoundIntLiteralExpr => VisitIntegerNumber(expr as BoundIntLiteralExpr),
            BoundFloatLiteralExpr => VisitFloatNumber(expr as BoundFloatLiteralExpr),
            BoundVariableExpr => VisitVariableExpr(expr as BoundVariableExpr),
            BoundCallFunctionExpr => VisitCallFExpr(expr as BoundCallFunctionExpr),
            _ => throw new Exception("[FATAL]: Expression that was attempted to be visited is invalid/unknown. '" + expr + "'."),
        };

        public object Visit(BoundStatement stmt) => stmt switch
        {
            BoundFunctionStatement => this.VisitFunction(stmt as BoundFunctionStatement),
            _ => throw new Exception("[FATAL]: Statement that was attempted to be visited is invalid/unknown. '"+stmt+"'."),
        };

        public void ClearResultStack()
        {
            valueStack.Clear();
        }

        public unsafe BoundExpression VisitIntegerNumber(BoundIntLiteralExpr expr)
        {
            valueStack.Push(LLVM.ConstReal(LLVM.Int32Type(), expr.Literal));
            return expr;
        }

        public unsafe BoundExpression VisitFloatNumber(BoundFloatLiteralExpr expr)
        {
            valueStack.Push(LLVM.ConstReal(LLVM.FloatType(), expr.Literal));
            return expr;
        }

        public unsafe BoundExpression VisitVariableExpr(BoundVariableExpr expr)
        {
            LLVMValueRef value;

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
            byte[] x = Encoding.ASCII.GetBytes(str);
            fixed (byte* p = x)
            {
                sbyte* sp = (sbyte*)p;
                return sp;
            }
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
                    return LLVM.DoubleType();
                case DataType.String:
                    return LLVM.String
            }    
        }

        public unsafe LLVMOpaqueType** ParameterTypes(List<Parameter> mtrs)
        {

        }

        public unsafe BoundExpression VisitBinaryExpr(BoundBinaryExpr expr)
        {
            this.Visit(expr.a);
            this.Visit(expr.b);

            LLVMValueRef r = valueStack.Pop();
            LLVMValueRef l = valueStack.Pop();

            LLVMValueRef n;

            switch (expr.op)
            {
                case Operator.PLUS:
                    n = LLVM.BuildFAdd(builder, l, r, "AddTMP");
                    break;
                case Operator.MINUS:
                    n = LLVM.BuildFSub(builder, l, r, "SubTMP");
                    break;
                case Operator.MULTI:
                    n = LLVM.BuildFMul(builder, l, r, "MulTMP");
                    break;
                case Operator.DIV:
                    n = LLVM.BuildFDiv(builder, l, r, "DivTMP");
                    break;
                default:
                    throw new Exception("Invalid Operator in Binary Expression.");
            }

            valueStack.Push(n);
            return expr;
        }

        public unsafe BoundExpression VisitCallFExpr(BoundCallFunctionExpr expr)
        {
            var callee = LLVM.GetNamedFunction(module, expr.Name);
            if(callee.Pointer == IntPtr.Zero)
            {
                throw new Exception("Unknown Function Called.");
            }
            if(LLVM.CountParams(callee) != expr.Arguments.Count)
            {
                throw new Exception("Incorrect number of arguments passed.");
            }

            var argCount = (uint)expr.Arguments.Count;
            var argsV = new LLVMValueRef[Math.Max(argCount, 1)];

            for(int i = 0; i < argCount; ++i)
            {
                this.Visit(expr.Arguments[i]);
                argsV[i] = valueStack.Pop();
            }

            valueStack.Push(LLVM.BuildCall(builder, callee, argsV, "CallTMP"));

            return expr;
        }

        public unsafe BoundStatement VisitFunction(BoundFunctionStatement node)
        {
            this.namedValues.Clear();



            
            var argumentCount = (uint)node.Parameters.Count;
            var arguments = new LLVMTypeRef[Math.Max(argumentCount, 1)];

            LLVMValueRef function = LLVM.GetNamedFunction(this.module, StrToSByte(node.Name));
            

            if (function.Pointer != IntPtr.Zero)
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

                function = LLVM.AddFunction(this.module, node.Name, LLVM.FunctionType(LLVM.DoubleType(), LLVM.Type, argumentCount, 0));
                LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
            }

            for (int i = 0; i < argumentCount; ++i)
            {
                string argumentName = node.Parameters[i].Name;

                LLVMValueRef param = LLVM.GetParam(function, (uint)i);
                LLVM.SetValueName(param, StrToSByte(argumentName));

                this.namedValues[argumentName] = param;
            }

            this.valueStack.Push(function);


            LLVMValueRef function2 = this.valueStack.Pop();

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

            LLVM.BuildRet(this.builder, this.valueStack.Pop());

            LLVM.VerifyFunction(function2, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            this.valueStack.Push(function2);

            return node;
        }
    }
}
