using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Context;

public class Frame
{
    public Frame Parent { get; set; }

    public string Name { get; set; }

    // The node where this frame was invoked
    public CallExpression ReturnExpression { get; set; }

    public List<Expression> DeferExpressions { get; set; } = new();

    public Frame(
        CallExpression returnExpression = null,
        Frame            parent     = null,
        string           name       = null
    ) {
        ReturnExpression = returnExpression;
        Parent     = parent;
        Name       = name ?? (parent?.Name ?? "global");
    }

}