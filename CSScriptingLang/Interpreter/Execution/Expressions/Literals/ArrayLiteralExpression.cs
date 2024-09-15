using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class ArrayLiteralExpression : LiteralValueExpression
{
    [VisitableNodeProperty]
    public List<Expression> Elements { get; } = new();

    public ArrayLiteralExpression(object value = null) : base(value) { }

    public override ValueReference Execute(ExecContext ctx) {
        var elements = Elements.Select(e => {
            var value = e.Execute(ctx).Value;
            return value;
        });

        var arr = Value.Array(elements);
        
        return ctx.ValReference(arr);
    }
}