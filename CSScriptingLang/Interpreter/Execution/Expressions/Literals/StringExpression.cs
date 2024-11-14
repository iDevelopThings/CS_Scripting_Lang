using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
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

    public override ITypeAlias GetTypeAlias() => TypeAlias<StringPrototype>.Get();
    
    public override ValueReference Execute(ExecContext ctx) {
        return new ValueReference(ctx, RTValue);
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<StringPrototype>.Get().Ty;
    }
    
    public StringExpression(string value) : base(value) {
        NativeValue = value;
        // Strip the quotes from the string
        if (NativeValue.Length >= 2 && (NativeValue[0] == '"' && NativeValue[^1] == '"') || (NativeValue[0] == '\'' && NativeValue[^1] == '\'')) {
            NativeValue = NativeValue[1..^1];
        }
    }
}