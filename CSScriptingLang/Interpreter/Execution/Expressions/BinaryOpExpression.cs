using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

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
            DiagnosticManager.Diagnostic_Error_Fatal().Message("Null reference in binary operation").Range(this).Report();
        }

        if (leftRef.Value!.Is.Null && rightRef.Value!.Is.Null) {
            DiagnosticManager.Diagnostic_Error_Fatal().Message("Both values are null").Range(this).Report();
        }

        switch (Operator) {
            case OperatorType.MinusEquals:
            case OperatorType.PlusEquals: {
                var rightValueRef = Value.Reference(rightRef);
                var resultValue = leftRef.Value.Operator(Operator, rightValueRef);
                
                leftRef.SetValue(resultValue, true);

                return leftRef;
            }
            
            case OperatorType.Assignment: {
                leftRef.SetValue(rightRef.Value, true);

                return leftRef;
            }
            default: {
                var resultValue = leftRef.Value.Operator(Operator, rightRef.Value);

                return new ValueReference(ctx, resultValue);
                /*try {
                    var resultValue = leftRef.Value.Operator(Operator, rightRef.Value);

                    return new ValueReference(ctx, resultValue);
                }
                catch (InterpreterRuntimeException e) {
                    throw new FatalInterpreterException(e.Message, this);
                }*/
            }
        }
    }
}