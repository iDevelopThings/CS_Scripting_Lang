using CSScriptingLang.Interpreter.Context;
using SharpX;

namespace CSScriptingLang.Interpreter.Execution;

public interface IExecutableVoid
{
    Maybe<ValueReference> Execute(ExecContext ctx); 


    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        yield return Execute(ctx);
    }
}
public interface IExecutableNode
{
    ValueReference Execute(ExecContext ctx);

    public IEnumerable<ValueReference> ExecuteMulti(ExecContext ctx) {
        yield return Execute(ctx);
    }
}