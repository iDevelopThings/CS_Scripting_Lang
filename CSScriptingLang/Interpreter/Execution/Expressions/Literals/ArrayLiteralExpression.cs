using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class ArrayLiteralExpression : LiteralValueExpression
{
    [VisitableNodeProperty]
    public List<Expression> Elements { get; } = new();

    public ArrayLiteralExpression(object value = null) : base(value) { }

    public override ITypeAlias GetTypeAlias() => TypeAlias<ArrayPrototype>.Get();

    public override ValueReference Execute(ExecContext ctx) {
        var elements = Elements.Select(e => {
            var value = e.Execute(ctx).Value;
            return value;
        });

        var arr = Value.Array(elements);

        return ctx.ValReference(arr);
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<ArrayPrototype>.Get().Ty;
    }
}