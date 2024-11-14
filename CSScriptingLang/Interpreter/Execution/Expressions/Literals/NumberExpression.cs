using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class LiteralNumberExpression : LiteralValueExpression
{
    public LiteralNumberExpression(object value) : base(value) { }

    public virtual Value RTValue => Value.Number(UntypedValue);

    public override ValueReference Execute(ExecContext ctx) {
        return new ValueReference(ctx, RTValue);
    }

    public static LiteralNumberExpression CreateFromToken(Token token) {
        return token.Type switch {
            TokenType.Int32  => new Int32Expression(token.Value),
            TokenType.Int64  => new Int64Expression(token.Value),
            TokenType.Float  => new FloatExpression(token.Value),
            TokenType.Double => new DoubleExpression(token.Value),
            _                => throw new ArgumentException($"Unsupported token type {token.Type}")
        };
    }

}

public partial class LiteralNumberExpression<T> : LiteralNumberExpression where T : struct
{
    public T NativeValue {
        get => (T) UntypedValue;
        set => UntypedValue = value;
    }
    public override Value RTValue => Value.Number(NativeValue);
    public string StringType {
        get {
            return typeof(T).Name switch {
                "Int32"   => "int",
                "Int64"   => "long",
                "Single"  => "float",
                "Double"  => "double",
                "Decimal" => "decimal",
                _         => typeof(T).Name
            };
        }
    }

    public LiteralNumberExpression(T value) : base(value) { }


}

[ASTNode]
public partial class Int32Expression : LiteralNumberExpression<int>
{
    public Int32Expression(string value) : base(int.Parse(value)) { }
    public Int32Expression(int    value) : base(value) { }

    public override ITypeAlias GetTypeAlias() => TypeAlias<Int32Prototype>.Get();
    
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<Int32Prototype>.Get().Ty;
    }
}

public partial class Int64Expression : LiteralNumberExpression<long>
{
    public Int64Expression(string value) : base(long.Parse(value)) { }
    public Int64Expression(long   value) : base(value) { }
    public override ITypeAlias GetTypeAlias() => TypeAlias<Int64Prototype>.Get();
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<Int64Prototype>.Get().Ty;
    }
}

public partial class FloatExpression : LiteralNumberExpression<float>
{
    public FloatExpression(string value) : base(float.Parse(value.Replace("f", "").Replace("F", ""))) { }
    public FloatExpression(float  value) : base(value) { }
    public override ITypeAlias GetTypeAlias() => TypeAlias<FloatPrototype>.Get();

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<FloatPrototype>.Get().Ty;
    }
}

public partial class DoubleExpression : LiteralNumberExpression<double>
{
    public DoubleExpression(string value) : base(double.Parse(value)) { }
    public DoubleExpression(double value) : base(value) { }
    public override ITypeAlias GetTypeAlias() => TypeAlias<DoublePrototype>.Get();

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<DoublePrototype>.Get().Ty;
    }
}