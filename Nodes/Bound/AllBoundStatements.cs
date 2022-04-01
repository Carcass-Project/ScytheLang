using Scythe.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scythe.Nodes.Bound
{
    public enum BoundStatementKind
    {
        PACKAGE_DECL,
        USE_PACKAGE,
        BLOCK,
        FUNCTION,
        RETURN,
        VARIABLE
    }

    public class BoundStatement
    {
        public BoundStatementKind Kind;
    }

    #region BoundStatement

    public class BoundPackageDeclStatement : BoundStatement
    {
        public string Name;

        public BoundPackageDeclStatement(string name)
        {
            Name = name;
        }
    }

    public class BoundUsePackageStatement : BoundStatement
    {
        public string Name;

        public BoundUsePackageStatement(string name)
        {
            Name = name;
        }
    }

    public class BoundBlockStatement : BoundStatement
    {
        public IReadOnlyList<BoundStatement> Body;

        public BoundBlockStatement(IReadOnlyList<BoundStatement> body)
        {
            Body = body;
        }
    }

    public class BoundExpressionStatement : BoundStatement
    {
        public BoundExpression expr;

        public BoundExpressionStatement(BoundExpression expr)
        {
            this.expr = expr;
        }
    }

    public class Parameter
    {
        public Scythe.Symbols.DataType DataType;
        public string Name;

        public Parameter(DataType dataType, string name)
        {
            DataType = dataType;
            Name = name;
        }
    }

    public class BoundFunctionStatement : BoundStatement
    {
        public List<Parameter> Parameters;
        public string Name;
        public Scythe.Symbols.DataType Type;
        public BoundBlockStatement Body;

        public BoundFunctionStatement(List<Parameter> parameters, string name, DataType type, BoundBlockStatement body)
        {
            Parameters = parameters;
            Name = name;
            Type = type;
            Body = body;
        }
    }

    public class BoundVariableDeclStatement : BoundStatement
    {
        public string Name;
        public DataType Type;
        public BoundExpression Value;

        public BoundVariableDeclStatement(string name, DataType type, BoundExpression value)
        {
            Name = name;
            Type = type;
            Value = value;
        }
    }

    public class BoundReturnStatement : BoundStatement
    {
        public BoundExpression Value;

        public BoundReturnStatement(BoundExpression value)
        {
            Value = value;
        }
    }

    #endregion
}
