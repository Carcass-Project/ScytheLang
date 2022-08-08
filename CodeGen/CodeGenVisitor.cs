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

        private readonly Dictionary<string, Symbol> symbolTable;

        public CodeGenVisitor(LLVMModuleRef module, LLVMBuilderRef builder, Dictionary<string, Symbol> symbolTable)
        {
            this.module = module;
            this.builder = builder;
            this.symbolTable = symbolTable;
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
            BoundInlineAsmStatement => this.VisitInlineAsm(stmt as BoundInlineAsmStatement),
            BoundVariableDeclStatement => this.VisitVariableDecl(stmt as BoundVariableDeclStatement),
            BoundUsePackageStatement => this.VisitImportStmt(stmt as BoundUsePackageStatement),
            BoundVariableSetStatement => this.VisitVarSet(stmt as BoundVariableSetStatement),
            BoundExternFunctionStatement => this.VisitFunctionExtern(stmt as BoundExternFunctionStatement),
            BoundCastStatement => this.VisitCast(stmt as BoundCastStatement),
            BoundIfStatement => this.VisitIfStmt(stmt as BoundIfStatement),
            _ => throw new Exception("[FATAL]: Statement that was attempted to be visited is invalid/unknown. '"+stmt+"'."),
        };

        public void ClearResultStack()
        {
            valueStack.Clear();
        }

        /*
            public unsafe BoundStatement VisitVariableDecl(BoundVariableDeclStatement stmt)
        {
            var alloca_v = LLVM.BuildAlloca(builder, DataTyToType(stmt.Type), StrToSByte(stmt.Name));
            
            this.Visit(stmt.Value);
            LLVM.BuildStore(builder, valueStack.Pop(), alloca_v);

            this.Visit(stmt.Value);
            namedValues.Add(stmt.Name, valueStack.Pop());

            return stmt;
        }
         */

        public BoundStatement VisitVarSet(BoundVariableSetStatement stmt)
        {
            if(namedValues.ContainsKey(stmt.a))
            {
                this.Visit(stmt.b);
                namedValues[stmt.a] = valueStack.Pop();
            }
            else
            {
                throw new Exception("The variable name you are to set to, " + stmt.a + " was never declared.");
            }
            return stmt;
        }

        public BoundStatement VisitExprStmt(BoundExpressionStatement stmt)
        {
            this.Visit(stmt.expr);
            return stmt;
        }

        public unsafe BoundExpression VisitIntegerNumber(BoundIntLiteralExpr expr)
        {

            valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), (ulong)expr.Literal, 1));
            return expr;
        }

        public unsafe BoundExpression VisitFloatNumber(BoundFloatLiteralExpr expr)
        {

            valueStack.Push(LLVM.ConstReal(LLVM.FloatType(), expr.Literal));
            return expr;
        }

        public unsafe BoundExpression VisitStringLiteral(BoundStringLiteralExpr expr)
        {

            int ln = expr.Literal.Length;

            //valueStack.Push(LLVM.BuildGEP2(builder, LLVM.PointerType(LLVM.Int8Type(), 0), LLVM.ConstString(StrToSByte(expr.Literal), (uint)ln, 0), pptr, (uint)ln, StrToSByte("GEPStr")));
            valueStack.Push(LLVM.BuildGlobalStringPtr(builder, StrToSByte(expr.Literal), StrToSByte("strtmp")));
            
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

        public unsafe LLVMIntPredicate CDTOperator(Operator op) => op switch
        {
            Operator.GT => LLVMIntPredicate.LLVMIntSGT,
            Operator.LT => LLVMIntPredicate.LLVMIntSLT,
            Operator.GQ => LLVMIntPredicate.LLVMIntSGE,
            Operator.LQ => LLVMIntPredicate.LLVMIntSLT,
            Operator.NOT => LLVMIntPredicate.LLVMIntNE,
            Operator.EQ => LLVMIntPredicate.LLVMIntEQ,
            _ => throw new Exception("Unknown Operator.")
        };

        public unsafe BoundStatement VisitIfStmt(BoundIfStatement stmt)
        {
            BoundBinaryExpr cond = stmt.cond as BoundBinaryExpr;

            this.Visit(cond.a); this.Visit(cond.b);

            LLVMOpaqueValue* r = valueStack.Pop();
            LLVMOpaqueValue* l = valueStack.Pop();

            //var val = LLVM.BuildFCmp(builder, LLVMRealPredicate.LLVMRealPredicateTrue, l, r, Helpers.StrToSByte("ifcond"));

            var val = LLVM.BuildICmp(builder, CDTOperator(cond.op), l, r, Helpers.StrToSByte("ifcond"));

            var func = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(builder));

            LLVMOpaqueBasicBlock* thenBB = LLVM.AppendBasicBlock(func, Helpers.StrToSByte("then"));
            LLVMOpaqueBasicBlock* elseBB = LLVM.AppendBasicBlock(func, Helpers.StrToSByte("else"));
            LLVMOpaqueBasicBlock* mergeBB = LLVM.AppendBasicBlock(func, Helpers.StrToSByte("ifcont"));

            LLVM.BuildCondBr(builder, val, thenBB, mergeBB);

            LLVM.PositionBuilderAtEnd(builder, elseBB);

            LLVM.BuildBr(builder, mergeBB);

            LLVM.PositionBuilderAtEnd(builder, thenBB);

            LLVM.BuildBr(builder, mergeBB);

            LLVM.PositionBuilderAtEnd(this.builder, mergeBB);



            return stmt;
        }

        public unsafe BoundStatement VisitImportStmt(BoundUsePackageStatement stmt)
        {
            var importedFile = File.ReadAllText(stmt.Name.Replace("\"", ""));

            var Lexer = new Lexer(importedFile);
            var Parser = new Parser(Lexer);

            var AST = Parser.ParseProgram();

            if(AST.IsOk)
            {
                var CGV = new CodeGenVisitor(module, builder, new Dictionary<string, Symbol>());

                foreach (var statement in new Binder().Bind(AST.Ok.Value.ToList()))
                {
                    Console.WriteLine(statement);
                    CGV.Visit(statement);
                }
            }
            else
            {
                Console.WriteLine("Failed to Parse Scythe Package File " + stmt.Name+".");
            }

            return stmt;
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
                case DataType.Void:
                    return LLVM.VoidType();
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
                    if(expr.a.GetType() == typeof(BoundStringLiteralExpr) && expr.b.GetType() == typeof(BoundStringLiteralExpr))
                    {
                        n = LLVM.BuildGlobalStringPtr(builder, StrToSByte((expr.a as BoundStringLiteralExpr).Literal+(expr.b as BoundStringLiteralExpr).Literal), StrToSByte("strtmp"));
                        break;
                    }

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

        public unsafe BoundStatement VisitInlineAsm(BoundInlineAsmStatement stmt)
        {
            LLVM.AppendModuleInlineAsm(module, StrToSByte((stmt.asm as BoundStringLiteralExpr).Literal), (UIntPtr)(stmt.asm as BoundStringLiteralExpr).Literal.Length);
            //LLVM.ConstInlineAsm(LLVM.FunctionType(LLVM.VoidType(), null, 0, 0), StrToSByte((stmt.asm as BoundStringLiteralExpr).Literal), StrToSByte(""), 0, 1);
            return stmt;
        }

        public unsafe BoundStatement VisitFunctionExtern(BoundExternFunctionStatement stmt)
        {
            var mod = builder.InsertBlock.Parent.GlobalParent;
            LLVMOpaqueValue* callee;
            var namedF = mod.GetNamedFunction(stmt.Name);
            if (namedF != null)
            {
                callee = namedF;
            }
            else
            {

                fixed (LLVMOpaqueType** pptr = ParameterTypes(stmt.Parameters))
                    LLVM.AddFunction(mod, StrToSByte(stmt.Name), LLVM.FunctionType(DataTyToType(stmt.Type), pptr, (uint)stmt.Parameters.Count, 1));
            }
            return stmt;
        }

        public unsafe BoundExpression VisitCallFExpr(BoundCallFunctionExpr expr)
        {
            var callee = LLVM.GetNamedFunction(module, StrToSByte(expr.Name));
            var mod = builder.InsertBlock.Parent.GlobalParent;
            if((IntPtr)callee == IntPtr.Zero)
            {
                // namedValues.Add(expr.Name, LLVM.AddFunction(module, StrToSByte(expr.Name), DataTyToType((symbolTable[expr.Name] as FunctionSymbol).returnType)));
                //valueStack.Push(LLVM.ConstInt(LLVM.Int32Type(), 0, 1));
                if(expr.Name == "print")
                {
                    var namedF = mod.GetNamedFunction("printf");
                    if (namedF != null)
                    {
                        callee = namedF;
                    }
                    else
                    {

                        fixed (LLVMOpaqueType** pptr = new LLVMOpaqueType*[] { LLVM.PointerType(LLVM.Int8Type(), 0) })
                            callee = LLVM.AddFunction(mod, StrToSByte("printf"), LLVM.FunctionType(LLVM.Int32Type(), pptr, 1, 1));
                    }
                }
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

            fixed (LLVMOpaqueValue** pptr = argsV)
            {
             
                    valueStack.Push(LLVM.BuildCall(builder, callee, pptr, argCount, StrToSByte("CallTMP")));
                 
                
            }

            return expr;
        }

        public unsafe BoundStatement VisitCast(BoundCastStatement stmt)
        {
            this.Visit(stmt.Expr);
            var x = valueStack.Pop();

            if (LLVM.TypeOf(x.Value) == LLVM.Int32Type() && stmt.Type == DataType.Float)
                LLVM.BuildCast(builder, LLVMOpcode.LLVMSIToFP, x.Value, DataTyToType(stmt.Type), StrToSByte("CastSIFP"));
            return stmt;
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
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                LLVM.DeleteFunction(function2);
                throw;
            }

            

            LLVM.VerifyFunction(function2, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            this.valueStack.Push(function2);

            return node;
        }

        public unsafe BoundStatement VisitVariableDecl(BoundVariableDeclStatement stmt)
        {
            var alloca_v = LLVM.BuildAlloca(builder, DataTyToType(stmt.Type), StrToSByte(stmt.Name));
            
            this.Visit(stmt.Value);
            LLVM.BuildStore(builder, valueStack.Pop(), alloca_v);

            this.Visit(stmt.Value);
            namedValues.Add(stmt.Name, valueStack.Pop());

            return stmt;
        }

        public unsafe BoundStatement VisitReturn(BoundReturnStatement stmt)
        {
            this.Visit(stmt.Value);
            Pointer<LLVMOpaqueValue> popped;
            if(this.valueStack.TryPop(out popped))
                LLVM.BuildRet(this.builder, popped);
            else
            {
                LLVM.BuildRet(this.builder, LLVM.ConstInt(LLVM.Int32Type(), 1, 1));
            }
            return stmt;
        }
    }
}
