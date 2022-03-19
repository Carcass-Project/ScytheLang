using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yoakke.SynKit.Lexer;
using Yoakke.SynKit.Lexer.Attributes;

namespace Scythe
{
    public enum TokenType
    {
        // Text Analysis Tokens. (tok_x)
        [Error]tok_err,
        [Ignore][Regex(Regexes.Whitespace)] tok_ws,
        [End]tok_end,
        [Ignore, Regex(Regexes.LineComment)] tok_cmt,

        // Keyword Tokens. (kw_x)
        [Token("fn")] kw_Function,
        [Token("use")] kw_Use,
        [Token("return")] kw_Return,
        [Token("int")] kw_Int,
        [Token("var")] kw_Var,
        [Token("string")] kw_String,
        [Token("bool")] kw_Bool,
        [Token("float")] kw_Float,
        [Token("char")] kw_Char,
        [Token("uint")] kw_Uint,
        [Token("package")] kw_Package,

        // Literal Tokens. (lit_x)
        [Regex(Regexes.StringLiteral)] lit_String,
        [Regex(Regexes.IeeeFloatLiteral)] lit_Float,
        [Regex(Regexes.IntLiteral)] lit_Int,

        // Mathematical Operators: (op_x)
        [Token(">")] op_Greater,
        [Token("<")] op_Less,
        [Token(">=")] op_GreaterEq,
        [Token("<=")] op_LessEq,
        [Token("==")] op_Equal,
        [Token("!=")] op_NotEqual,
        [Token("+")] op_Add,
        [Token("-")] op_Sub,
        [Token("*")] op_Mul,
        [Token("/")] op_Div,

        // Bitwise Operators: (bit_x)
        [Token("&")] bit_And,
        [Token("|")] bit_Or,
        [Token("~")] bit_Not,
        [Token("^")] bit_Xor,
        [Token("<<")] bit_LShift,
        [Token(">>")] bit_RShift,

        // Logical Operators: (log_x)
        [Token("&&")] log_And,
        [Token("||")] log_Or,

        // Basic Operators: (bas_x)
        [Token("<-")] bas_leftlook,
        [Token("->")] bas_rightlook,
        [Token(":")] bas_colon,
        [Token("$")] bas_arrdecl,

        // Other Stuff: (x)
        [Regex(Regexes.Identifier)] identifier,
        [Token("(")] ParenthesisOpen,
        [Token(")")] ParenthesisClosed,
        [Token("[")] BracketOpen,
        [Token("]")] BracketClose,
        [Token("{")] CurlyOpen,
        [Token("}")] CurlyClose,
    }

    [Lexer(typeof(TokenType))]
    public partial class Lexer
    {
        
    }
}
