using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Execution.Statements;

[ASTNode]
public partial class AwaitStatement : Statement
{
    [VisitableNodeProperty]
    public BaseNode Value { get; set; }

    public AwaitStatement() { }
    public AwaitStatement(BaseNode value) {
        Value = value;
    }

    public override string ToString(int indent = 0) {
        return $"{new string(' ', indent)}{GetType().Name}: {Value?.ToString(0)}";
    }
}