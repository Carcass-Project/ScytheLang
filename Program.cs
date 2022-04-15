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
            var SymbolTable = new Dictionary<string, Symbol>();
            var xz = DateTime.Now;
            Lexer lx = new Lexer(File.ReadAllText(extraOptions[0]));
            Parser ps = new Parser(lx);
            var program = ps.ParseProgram();

            if (program.IsOk)
            {
                unsafe
                {
                    Console.WriteLine("Success!");

                    Directory.CreateDirectory("out");

                    var triple = LLVM.GetDefaultTargetTriple();

                    LLVM.InitializeAllTargetInfos();
                    LLVM.InitializeAllTargets();
                    LLVM.InitializeAllTargetMCs();
                    LLVM.InitializeAllAsmParsers();
                    LLVM.InitializeAllAsmPrinters();

                    sbyte* Error;



                    LLVMModuleRef module = LLVM.ModuleCreateWithName(Helpers.StrToSByte(extraOptions[1]));
                    LLVMBuilderRef builder = LLVM.CreateBuilder();
                
               
                if (extraOptions.Length > 2)
                    {
                        if (extraOptions[2] == "--modules")
                        {
                            foreach (var x in extraOptions)
                            {
                                if (x.EndsWith(".bc"))
                                {
                                    LLVMOpaqueMemoryBuffer* mem;
                                    sbyte* error;

                                    if (LLVM.CreateMemoryBufferWithContentsOfFile(Helpers.StrToSByte(x), &mem, &error) == 1)
                                        Console.WriteLine(Helpers.SByteToStr(error));  

                      

                                    LLVMOpaqueModule* newModule;
                                    sbyte* error2;

                                    if (LLVM.ParseBitcode(mem, &newModule, &error2) == 1)
                                    {
                                        Console.WriteLine("Parsing bitcode for module failed.");
                                    }

                                    var F = LLVM.GetFirstFunction(newModule);

                                    while(F != null)
                                    {

                                            string name = Helpers.SByteToStr(LLVM.GetValueName(F));
                                            Console.WriteLine(name);
                                            SymbolTable.Add(name, new Scythe.Symbols.FunctionSymbol(name, Helpers.GetSYDataType(LLVM.TypeOf(F))));

                                            F = LLVM.GetNextFunction(F);
                                        
                                    }
                                    Console.WriteLine(Helpers.SByteToStr(LLVM.PrintModuleToString(newModule)));
                                }
                            }
                        }
                    }

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

                    string fileName = "out/" + extraOptions[1] + ".o";
                    LLVMCodeGenFileType flType = LLVMCodeGenFileType.LLVMObjectFile;

                    sbyte* emitError;

   

                    LLVM.TargetMachineEmitToFile(tm, module, Helpers.StrToSByte(fileName), flType, &emitError);


                    //LLVM.ParseBitcode2
                    try
                    {
                        Console.WriteLine(module.PrintToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                    module.PrintToFile("out/" + extraOptions[1] + ".ll");
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
