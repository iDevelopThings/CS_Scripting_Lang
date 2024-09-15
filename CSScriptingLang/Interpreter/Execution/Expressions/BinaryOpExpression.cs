using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class BinaryOpExpression : Expression, IExecutableNode
{
    [VisitableNodeProperty]
    public Expression Left { get; }

    public OperatorType Operator { get; }

    [VisitableNodeProperty]
    public Expression Right { get; }

    public BinaryOpExpression(Expression left, OperatorType op, Expression right) {
        Left     = left;
        Operator = op;
        Right    = right;
    }

    public override ValueReference Execute(ExecContext ctx) {

        var leftRef  = ctx.ExecuteLValue(Left.Execute);
        var rightRef = ctx.ExecuteRValue(Right.Execute);

        if (leftRef.Value == null || rightRef.Value == null) {
            throw new InterpreterException("Null reference in binary operation", this);
        }

        if (leftRef.Value.Is.Null && rightRef.Value.Is.Null) {
            throw new FatalInterpreterException("Both values are null", this);
        }

        switch (Operator) {
            case OperatorType.Assignment: {
                leftRef.SetValue(rightRef.Value);

                return leftRef;
            }
            default: {
                try {
                    var resultValue = leftRef.Value.Operator(Operator, rightRef.Value);

                    return new ValueReference(ctx, resultValue);
                }
                catch (InterpreterRuntimeException e) {
                    throw new FatalInterpreterException(e.Message, this);
                }
            }
        }
    }
}