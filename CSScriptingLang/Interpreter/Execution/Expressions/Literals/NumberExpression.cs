using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
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
    public static LiteralNumberExpression CreateFromRawValue(object value) {
        return value switch {
            int i    => new Int32Expression(i),
            long l   => new Int64Expression(l),
            float f  => new FloatExpression(f),
            double d => new DoubleExpression(d),
            _        => throw new ArgumentException($"Unsupported type {value.GetType().Name}")
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

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}(native: {StringType}): {NativeValue}";
    }
}

[ASTNode]
public partial class Int32Expression : LiteralNumberExpression<int>
{
    public Int32Expression(string value) : base(int.Parse(value)) { }
    public Int32Expression(int    value) : base(value) { }

    public override RuntimeType GetRuntimeType() => StaticTypes.Int32;
}

public partial class Int64Expression : LiteralNumberExpression<long>
{
    public Int64Expression(string value) : base(long.Parse(value)) { }
    public Int64Expression(long   value) : base(value) { }

    public override RuntimeType GetRuntimeType() => StaticTypes.Int64;
}

public partial class FloatExpression : LiteralNumberExpression<float>
{
    public FloatExpression(string value) : base(float.Parse(value.Replace("f", "").Replace("F", ""))) { }
    public FloatExpression(float  value) : base(value) { }

    public override RuntimeType GetRuntimeType() => StaticTypes.Float;
}

public partial class DoubleExpression : LiteralNumberExpression<double>
{
    public DoubleExpression(string value) : base(double.Parse(value)) { }
    public DoubleExpression(double value) : base(value) { }

    public override RuntimeType GetRuntimeType() => StaticTypes.Double;
}