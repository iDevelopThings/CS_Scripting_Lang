using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution.Statements;


[ASTNode]
public partial class Statement : BaseNode, IExecutableVoid
{
    public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
        throw new FatalInterpreterException($"Statement.Execute not implemented for {GetType().ToFullLinkedName()}", this);
    }

    public virtual IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        yield return Execute(ctx);
    }
}