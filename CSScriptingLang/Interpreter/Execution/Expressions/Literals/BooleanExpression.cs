using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class BooleanExpression : LiteralValueExpression
{
    public bool NativeValue {
        get => (bool) UntypedValue;
        set => UntypedValue = value;
    }

    public Value RTValue => Value.Boolean(NativeValue);

    public override ValueReference Execute(ExecContext ctx) {
        return new ValueReference(ctx, RTValue);
    }
    
    public BooleanExpression(bool value) : base(value) { }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {NativeValue}";
    }

    public override RuntimeType GetRuntimeType() => StaticTypes.Boolean;
}