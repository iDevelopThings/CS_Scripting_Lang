using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
public partial class LiteralValueNode : BaseNode, IConstantNode, IExpressionNode
{
    public object UntypedValue { get; set; }

    public LiteralValueNode(object value) {
        UntypedValue = value;
    }
}

[ASTNode]
public partial class LiteralNumberNode : LiteralValueNode
{
    public LiteralNumberNode(object value) : base(value) { }
    
    

    public static LiteralNumberNode CreateFromToken(Token token) {
        return token.Type switch {
            TokenType.Int32  => new Int32Node(token.Value),
            TokenType.Int64  => new Int64Node(token.Value),
            TokenType.Float  => new FloatNode(token.Value),
            TokenType.Double => new DoubleNode(token.Value),
            _                => throw new ArgumentException($"Unsupported token type {token.Type}")
        };
    }
    public static LiteralNumberNode CreateFromRawValue(object value) {
        return value switch {
            int i    => new Int32Node(i),
            long l   => new Int64Node(l),
            float f  => new FloatNode(f),
            double d => new DoubleNode(d),
            _        => throw new ArgumentException($"Unsupported type {value.GetType().Name}")
        };
    }
}

public partial class LiteralNumberNode<T> : LiteralNumberNode where T : struct
{
    public T Value {
        get => (T) UntypedValue;
        set => UntypedValue = value;
    }

    public string Type {
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

    public LiteralNumberNode(T value) : base(value) { }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}(native: {Type}): {Value}";
    }
}

[ASTNode]
public partial class Int32Node : LiteralNumberNode<int>
{
    public Int32Node(string value) : base(int.Parse(value)) { }
    public Int32Node(int value) : base(value) { }
}

public partial class Int64Node : LiteralNumberNode<long>
{
    public Int64Node(string value) : base(long.Parse(value)) { }
    public Int64Node(long value) : base(value) { }
}

public partial class FloatNode : LiteralNumberNode<float>
{
    public FloatNode(string value) : base(float.Parse(value.Replace("f", "").Replace("F", ""))) { }
    public FloatNode(float value) : base(value) { }
}

public partial class DoubleNode : LiteralNumberNode<double>
{
    public DoubleNode(string value) : base(double.Parse(value)) { }
    public DoubleNode(double value) : base(value) { }
}

[ASTNode]
public partial class NumberNode : LiteralValueNode
{
    public double Value {
        get => (double) UntypedValue;
        set => UntypedValue = value;
    }

    public NumberNode(double value) : base(value) { }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Value}";
    }
}

[ASTNode]
public partial class BooleanNode : LiteralValueNode
{
    public bool Value {
        get => (bool) UntypedValue;
        set => UntypedValue = value;
    }

    public BooleanNode(bool value) : base(value) { }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Value}";
    }
}

[ASTNode]
public partial class StringNode : LiteralValueNode
{
    public string Value {
        get => (string) UntypedValue;
        set => UntypedValue = value;
    }

    public StringNode(string value) : base(value) {
        Value = value;
        // Strip the quotes from the string
        if (Value.Length >= 2 && (Value[0] == '"' && Value[^1] == '"') || (Value[0] == '\'' && Value[^1] == '\'')) {
            Value = Value[1..^1];
        }
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Value}";
    }
}