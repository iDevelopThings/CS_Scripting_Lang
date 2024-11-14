using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

namespace CSScriptingLang.Interpreter.Context;

public class Frame
{
    public string Name { get; set; }

    public Frame Parent { get; set; }
    public int   Depth  { get; set; }

    public Dictionary<string, VariableSymbol> Locals { get; set; } = new();


    // The node where this frame was invoked
    public CallExpression ReturnExpression { get; set; }
    public CallExpr       CallExpression   { get; set; }

    public List<Expression> DeferExpressions { get; set; } = new();

    public Frame(
        CallExpression returnExpression = null,
        Frame          parent           = null,
        string         name             = null,
        int            depth            = 0
    ) {
        ReturnExpression = returnExpression;
        Parent           = parent;
        Name             = name ?? (parent?.Name ?? "global");
        Depth            = depth;
    }

    public void AddLocal(VariableSymbol symb) {
        Locals[symb.Name] = symb;
    }

    public bool TryGetLocal(string name, out VariableSymbol variable) {
        if (Locals.TryGetValue(name, out variable)) {
            return true;
        }

        if (Parent != null) {
            return Parent.TryGetLocal(name, out variable);
        }

        return false;
    }
}