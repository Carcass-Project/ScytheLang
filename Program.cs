using Scythe;
using Yoakke.SynKit.Lexer;

Lexer lx = new Lexer(File.ReadAllText("test.sy"));
Parser ps = new Parser(lx);
var program = ps.ParseProgram();

if (program.IsOk)
{
    Console.WriteLine("Success!");
    foreach(var x in program.Ok.Value)
    {
        Console.WriteLine(x);
        if(x.GetType() == typeof(Scythe.Nodes.FunctionStatement))
        {
            foreach(var y in (x as Scythe.Nodes.FunctionStatement).body.statements)
                Console.WriteLine(y);
        }
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Something went wrong while parsing your Scythe program");
    Console.WriteLine("Got: " + program.Error.Got.ToString() + " at Position " + program.Error.Position);
    Console.ResetColor();
}
