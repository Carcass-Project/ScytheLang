using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Token = Yoakke.SynKit.Lexer.IToken<Scythe.TokenType>;

namespace Scythe.Nodes
{
    public class Expression
    {
    }

    #region Expressions

    public class CallFunctionExpr : Expression
    {
        public Token name;
        public IReadOnlyList<Expression> arguments;

        public CallFunctionExpr(Token name, IReadOnlyList<Expression> arguments)
        {
            this.name = name;
            this.arguments = arguments;
        }
    }

    public class BinaryExpr : Expression
    { 
        public Expression left;
        public Expression right;
        public Token op;

        public BinaryExpr(Expression left, Expression right, Token op)
        {
            this.left = left;
            this.right = right;
            this.op = op;
        }
    }

    public class VariableExpr : Expression
    {
        public Token name;

        public VariableExpr(Token name)
        {
            this.name = name;
        }
    }

    public class IntLiteralExpr : Expression
    {
        public Token literal;

        public IntLiteralExpr(Token literal)
        {
            this.literal = literal;
        }
    }

    public class FloatLiteralExpr : Expression
    {
        public Token literal;

        public FloatLiteralExpr(Token literal)
        {
            this.literal = literal;
        }
    }

    public class StringLiteralExpr : Expression
    {
        public Token literal;

        public StringLiteralExpr(Token literal)
        {
            this.literal = literal;
        }
    }

    #endregion
}
