using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class LiteralValueExpression : Expression, IConstantNode
{
    public override bool IsConstant => true;

    public object UntypedValue { get; set; }

    public LiteralValueExpression(object value) {
        UntypedValue = value;
    }
    
}


[ASTNode]
public partial class NullValueExpression : LiteralValueExpression
{
    public Value RTValue => Value.Null();

    public override ITypeAlias GetTypeAlias() => TypeAlias<NullPrototype>.Get();
    
    public override ValueReference Execute(ExecContext ctx) {
        return new ValueReference(ctx, RTValue);
    }
    public NullValueExpression(object value) : base(value) {
        if(value != null) {
            throw new ArgumentException("NullValueExpression must have a null value");
        }
    }
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<NullPrototype>.Get().Ty;
    }
}