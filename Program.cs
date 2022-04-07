using Scythe;
using System.Linq;
using ClangSharp;
using ClangSharp.Interop;
using LLVMSharp;
using LLVMSharp.Interop;

using Yoakke.SynKit.Lexer;
partial class Program
{
    public static void ScytheCLI(string option, params string[] extraOptions)
    {
        if (option == "build")
        {
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

                    var CodeGenVisit = new Scythe.CodeGen.CodeGenVisitor(module, builder);
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

                    Console.WriteLine(module.PrintToString());

                    module.WriteBitcodeToFile("out/" + extraOptions[1] + ".bc");
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
