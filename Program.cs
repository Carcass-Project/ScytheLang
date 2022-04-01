using Scythe;
using ClangSharp;
using ClangSharp.Interop;
using LLVMSharp;
using LLVMSharp.Interop;

using Yoakke.SynKit.Lexer;
partial class Program
{

    public static void Main(string[] args)
    {


        if (args.Length >= 3)
        {

            if (args[0] == "build")
            {
                var xz = DateTime.Now;
                Lexer lx = new Lexer(File.ReadAllText(args[1]));
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



                        LLVMModuleRef module = LLVM.ModuleCreateWithName(Helpers.StrToSByte(args[2]));
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

                        string fileName = "out/" + args[2] + ".o";
                        LLVMCodeGenFileType flType = LLVMCodeGenFileType.LLVMObjectFile;

                        sbyte* emitError;

                        LLVM.TargetMachineEmitToFile(tm, module, Helpers.StrToSByte(fileName), flType, &emitError);



                        Console.WriteLine(module.PrintToString());

                        module.WriteBitcodeToFile("out/"+args[2]+".bc");




                    }

                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Something went wrong while parsing your Scythe program");
                    Console.WriteLine("Got: " + program.Error.Got.ToString() + " at Position " + program.Error.Position);
                    Console.ResetColor();
                }

                var yz = DateTime.Now;

                Console.WriteLine((yz - xz).TotalMilliseconds + "ms.");
            }
        }
   }
}
