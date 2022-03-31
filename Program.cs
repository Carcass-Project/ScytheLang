using Scythe;
using Yoakke.SynKit.Lexer;
var xz = DateTime.Now;
try
{



    Lexer lx = new Lexer(File.ReadAllText("test.sy"));
    Parser ps = new Parser(lx);
    var program = ps.ParseProgram();

    if (program.IsOk)
    {
        Console.WriteLine("Success!");
        foreach (var x in new Binder().Bind(program.Ok.Value.ToList()))
        {
            var CodeGenVisit = new Scythe.CodeGen.CodeGenVisitor(new LLVMSharp.LLVMModuleRef(), new LLVMSharp.LLVMBuilderRef());
            var y = CodeGenVisit.Visit(x);

        }
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Something went wrong while parsing your Scythe program");
        Console.WriteLine("Got: " + program.Error.Got.ToString() + " at Position " + program.Error.Position);
        Console.ResetColor();
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}
var yz = DateTime.Now;

Console.WriteLine((yz-xz).TotalMilliseconds+"ms.");
