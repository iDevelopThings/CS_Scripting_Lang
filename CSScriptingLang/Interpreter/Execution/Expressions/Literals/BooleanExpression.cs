using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
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

    public override ITypeAlias GetTypeAlias() => TypeAlias<BooleanPrototype>.Get();
    
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<BooleanPrototype>.Get().Ty;
    }
}