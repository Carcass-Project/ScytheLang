using Scythe;
using System.Linq;
using ClangSharp;
using ClangSharp.Interop;
using LLVMSharp;
using LLVMSharp.Interop;

using Yoakke.SynKit.Lexer;
partial class Program
{
    public unsafe static void ScytheCLI(string option, params string[] extraOptions)
    {
        if (option == "build")
        {
            bool JIT = false;
            bool Intrp = false;
            var SymbolTable = new Dictionary<string, Symbol>();

            List<string> linkByteCodeFiles = new List<string>();

            var xz = DateTime.Now;
            Lexer lx = new Lexer(File.ReadAllText(extraOptions[0]));
            Parser ps = new Parser(lx);
            var program = ps.ParseProgram();

            if (program.IsOk)
            {
                unsafe
                {
                    string fileName = "";
                    if (extraOptions.Length > 2)
                    {
                        if (extraOptions.Contains("--modules"))
                        {
                            /*Console.WriteLine(extraOptions.ToList().IndexOf("--modules"));
                            for(int f = 0; f < extraOptions.ToList().IndexOf("--modules"); f++)
                            {
                                var x = extraOptions[f];
                                Console.WriteLine(x);
                                if (x.EndsWith(".bc"))
                                {
                                    linkByteCodeFiles.Add(x);
                                }
                            }*/

                            int sz = extraOptions.ToList().IndexOf("--modules");
                            int i = sz+1;

                            while (extraOptions.Length > i)
                            {
                                if (extraOptions[i].EndsWith(".bc"))
                                {
                                    linkByteCodeFiles.Add(extraOptions[i]);
                                }
                                i++;
                            }


                        }

                        if (extraOptions.Contains("--jit"))
                        {
                            JIT = true;
                        }

                        if (extraOptions.Contains("--int"))
                        {
                            Intrp = true;
                        }

                        if (extraOptions.Contains("--wasm"))
                        {
                            LLVM.InitializeWebAssemblyTargetInfo();
                            LLVM.InitializeWebAssemblyTarget();
                            LLVM.InitializeWebAssemblyTargetMC();
                            LLVM.InitializeWebAssemblyAsmParser();
                            LLVM.InitializeWebAssemblyAsmPrinter();
                            fileName = "out/" + extraOptions[1] + ".wasm";
                        }

                        if(extraOptions.Contains("--nat"))
                        {
                            LLVM.InitializeAllTargetInfos();
                            LLVM.InitializeAllTargets();
                            LLVM.InitializeAllTargetMCs();
                            LLVM.InitializeAllAsmParsers();
                            LLVM.InitializeAllAsmPrinters();
                        }
                    }
                    else
                    {
                        LLVM.InitializeAllTargetInfos();
                        LLVM.InitializeAllTargets();
                        LLVM.InitializeAllTargetMCs();
                        LLVM.InitializeAllAsmParsers();
                        LLVM.InitializeAllAsmPrinters();
                    }

                    fileName = "out/" + extraOptions[1] + ".o";
                    Directory.CreateDirectory("out");

                    var triple = LLVM.GetDefaultTargetTriple();



                    sbyte* Error;

                    LLVMModuleRef module = LLVM.ModuleCreateWithName(Helpers.StrToSByte(extraOptions[1]));

                    if (linkByteCodeFiles.Count > 0)
                    {
                        foreach (var lbc in linkByteCodeFiles)
                        {
                            LLVMOpaqueModule* l_mod;
                            LLVMOpaqueMemoryBuffer* l_mbf;
                            sbyte* outErr;

                            LLVM.CreateMemoryBufferWithContentsOfFile(Helpers.StrToSByte(lbc), &l_mbf, &outErr);
                            //LLVM.GetBitcodeModule2(l_mbf, &l_mod);
                            sbyte* outErrBC;
                            LLVM.ParseBitcode(l_mbf, &l_mod, &outErrBC);
                            LLVM.LinkModules2(module, l_mod);
                        }
                    }

                    LLVMBuilderRef builder = LLVM.CreateBuilder();

                    var CodeGenVisit = new Scythe.CodeGen.CodeGenVisitor(module, builder, SymbolTable);
                    foreach (var x in new Binder().Bind(program.Ok.Value.ToList()))
                    {
                        var y = CodeGenVisit.Visit(x);
                    }

                    LLVMTarget* target;

                    var _Target = LLVM.GetTargetFromTriple(triple, &target, &Error);

                    if (target == null)
                    {
                        Console.WriteLine("[NATGEN]: Error while creating target, target is null.\nError Code(Sbyte*):");
                        Console.WriteLine(*Error);
                        return;
                    }

                    var tm = LLVM.CreateTargetMachine(target, triple, Helpers.StrToSByte("generic"), Helpers.StrToSByte(""), LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

                    LLVM.SetDataLayout(module, LLVM.CopyStringRepOfTargetData(LLVM.CreateTargetDataLayout(tm)));

                    LLVM.SetTarget(module, triple);

                   

                    LLVMCodeGenFileType flType = LLVMCodeGenFileType.LLVMObjectFile;


                    if (JIT)
                    {
                        sbyte* err;
                        var execJIT = module.CreateMCJITCompiler();                      
                        var mainFn = execJIT.FindFunction("main");
                        LLVM.ExecutionEngineGetErrMsg(execJIT, &err);
                        if(err != null)
                            Console.WriteLine("[FATAL][JIT]: " + Helpers.SByteToStr(err));
                        execJIT.RunFunction(mainFn, new LLVMGenericValueRef[] { });
                    }

                    else if(Intrp)
                    {
                        sbyte* err;
                        var execINT = module.CreateInterpreter();
                        var mainFn = execINT.FindFunction("main");
                        LLVM.ExecutionEngineGetErrMsg(execINT, &err);
                        if (err != null)
                            Console.WriteLine("[FATAL][INTERPRETER]: " + Helpers.SByteToStr(err));
                        execINT.RunFunction(mainFn, new LLVMGenericValueRef[] { });
                    }

                    else
                    {
                        module.PrintToFile("out/" + extraOptions[1] + ".ll");
                    }

                    

                    Console.WriteLine("\nDone in " + (DateTime.Now - xz).TotalMilliseconds + " ms.");
                }
            }
        }
        if(option == "new")
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("Creating a new Scythe project...");
            Console.ResetColor();
            var project = new ScytheProject(extraOptions[0], "0.0.1", 1.0f, new[] { "p1", "p2", "p3" }, "x86");
            project.GenerateProjectFile();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Created Project "+extraOptions[0]+"!");
            Console.ResetColor();
        }
    }

    public static void Main(string[] args)
    {
        if (args.Length >= 1)
        {
            /// ArgsList contains all of the args, then firstElement is set to the first element, and it is then removed from argsList so it's just the rest of the elements.
            List<string> argsList = args.ToList();
            string firstElement = argsList.First();
            argsList.Remove(firstElement);

            ScytheCLI(firstElement, argsList.ToArray());
        }
    }
}
