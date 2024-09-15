using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class StringExpression : LiteralValueExpression
{
    public string NativeValue {
        get => (string) UntypedValue;
        set => UntypedValue = value;
    }
    
    public Value RTValue => Value.String(NativeValue);
    
    public override ValueReference Execute(ExecContext ctx) {
        return new ValueReference(ctx, RTValue);
    }

    public StringExpression(string value) : base(value) {
        NativeValue = value;
        // Strip the quotes from the string
        if (NativeValue.Length >= 2 && (NativeValue[0] == '"' && NativeValue[^1] == '"') || (NativeValue[0] == '\'' && NativeValue[^1] == '\'')) {
            NativeValue = NativeValue[1..^1];
        }
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {NativeValue}";
    }

    public override RuntimeType GetRuntimeType() => StaticTypes.String;
}