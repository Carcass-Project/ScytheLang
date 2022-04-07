using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yoakke;
using Yoakke.SynKit.Parser;
using Token = Yoakke.SynKit.Lexer.IToken<Scythe.TokenType>;

namespace Scythe.Nodes
{
    public enum StatementKind
    {
        PACKAGE_DECL,
        USE_PACKAGE,
        BLOCK,
        FUNCTION,
        RETURN,
        VARIABLE
    }

    public class Statement
    {
        public StatementKind Kind { get; set; }
    }

    #region Statements
    public class PackageDeclStatement : Statement
    {
        public Token name;

        public PackageDeclStatement(Token name)
        {
            this.name = name;
        }
    }

    public class VariableDeclStatement : Statement
    {
        public Token name;
        public Expression value;
        public Token type;

        public VariableDeclStatement(Token name, Expression value, Token type)
        {
            this.name = name;
            this.value = value;
            this.type = type;
        }
    }

    public class PackageUseStatement : Statement
    {
        public Token name;

        public PackageUseStatement(Token name)
        {
            this.name = name;
        }
    }

    public class BlockStatement : Statement
    {
        public IReadOnlyList<Statement> statements;

        public BlockStatement(IReadOnlyList<Statement> statements)
        {
            this.statements = statements;
        }
    }

    public class ExpressionStatement : Statement
    {
        public Expression Expression;

        public ExpressionStatement(Expression expression)
        {
            Expression = expression;
        }
    }

    public class FunctionStatement : Statement
    {
        public Punctuated<(Token Ident, Token Colon, Token Type), Token> parameters;
        public BlockStatement body;
        public Token name;
        public Token type;

        public FunctionStatement(Punctuated<(Token Ident, Token Colon, Token Type), Token> parameters, BlockStatement body, Token name, Token type)
        {
            this.parameters = parameters;
            this.body = body;
            this.name = name;
            this.type = type;
        }
    }

    public class InlineAsmStatement : Statement
    {
        public Expression asm;

        public InlineAsmStatement(Expression asm)
        {
            this.asm = asm;
        }
    }

    public class ReturnStatement : Statement
    {
        public Expression value;

        public ReturnStatement(Expression value)
        {
            this.value = value;
        }
    }
    #endregion
}


