using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scythe.Nodes;
using Yoakke;
using Yoakke.Collections.Values;
using Yoakke.SynKit.Parser;
using Yoakke.SynKit.Parser.Attributes;
using Token = Yoakke.SynKit.Lexer.IToken<Scythe.TokenType>;

namespace Scythe
{
    [Parser(typeof(TokenType))]
    public partial class Parser
    {
        [Rule("program: stmt*")]
        private static IReadOnlyList<Statement> ProgramParse(IReadOnlyList<Statement> parsed)
        {
            return parsed;
        }

        [Rule("stmt: kw_Package bas_rightlook lit_String")]
        private static Statement DeclarePackage(Token notneeded, Token notneeded2, Token name)
        {
            return new PackageDeclStatement(name);
        }

        [Rule("stmt: kw_Use kw_Package bas_rightlook lit_String")]
        private static Statement UsePackage(Token notneededtoo, Token notneeded, Token notneeded2, Token name)
        {
            return new PackageUseStatement(name);
        }

        [Rule("expr: identifier'('(expr(',' expr) *) ? ')'")]
        public static Expression CallFunctionEx(Token name, Token _2, Punctuated<Expression, Token> arguments, Token _4)
        {
            return new CallFunctionExpr(name, arguments.Values.Select(t => t).ToList().ToValue());
        }

        [Rule("block_stmt: '{' stmt* '}'")]
        private static BlockStatement Block(Token notneeded, IReadOnlyList<Statement> stmts, Token notneeded2)
        {
            return new BlockStatement(stmts);
        }

        [Rule("type_identifier: (kw_Int|kw_String|kw_Float|kw_Bool|kw_Char|kw_Uint)")]
        private static Token typeident(Token ident)
        {
            return ident;
        }

        [Rule("stmt: expr")]
        public static Statement ExpressionStatementF(Expression expr)
        {
            return new ExpressionStatement(expr);
        }

        [Rule("stmt: kw_Function identifier '(' ((identifier ':' type_identifier) (',' (identifier ':' type_identifier))*)? ')' bas_rightlook type_identifier block_stmt")]
        private static Statement FunctionDeclaration(Token _1, Token name, Token _3, Punctuated<(Token Ident, Token Colon, Token Type), Token> parameters, Token _4, Token _5, Token type, BlockStatement body)
        {
            return new FunctionStatement(parameters, body, name, type);
        }

        [Rule("stmt: kw_Var identifier bas_leftlook expr ':' type_identifier")]
        private static Statement VarDeclaration(Token _1, Token name, Token _2, Expression value, Token _3, Token type)
        {
            return new VariableDeclStatement(name, value, type);
        }

        [Rule("stmt: kw_Return expr")]
        public static Statement ReturnStmt(Token _1, Expression expr)
        {
            return new ReturnStatement(expr);
        }

        [Right("^")]
        [Left("*", "/", "%")]
        [Left("+", "-")]
        [Left(">=", "<=", ">", "<")]
        [Left("==")]
        [Rule("expr")]
        public static Expression BinOp(Expression a, Token op, Expression b)
        {
            return new BinaryExpr(a, b, op);
        }

        [Rule("expr: '(' expr ')'")]
        public static Expression Grouping(Token _1, Expression n, Token _2) => n;

        [Rule("expr: lit_Int")]
        public static Expression IntLiteral(Token literal)
        {
            return new IntLiteralExpr(literal);
        }

        [Rule("expr: lit_String")]
        public static Expression StrLiteral(Token literal)
        {
            return new StringLiteralExpr(literal);
        }

        [Rule("expr: identifier")]
        public static Expression VariableExpr(Token varf)
        {
            return new VariableExpr(varf);
        }
        [Rule("expr: lit_Float")]
        public static Expression FloatLiteral(Token literal)
        {
            return new FloatLiteralExpr(literal);
        }

   
    }
}
