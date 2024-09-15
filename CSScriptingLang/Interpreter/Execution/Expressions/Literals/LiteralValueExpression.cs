using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class LiteralValueExpression : Expression, IConstantNode
{
    public override bool IsConstant => true;

    public object UntypedValue { get; set; }

    public LiteralValueExpression(object value) {
        UntypedValue = value;
    }

    public virtual RuntimeType GetRuntimeType() {
        throw new FailedToGetRuntimeTypeException(this, "No runtime type specified");
    }
    
}