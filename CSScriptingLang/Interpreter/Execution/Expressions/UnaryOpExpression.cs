using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class UnaryOpExpression : Expression
{
    public OperatorType Operator { get; }

    [VisitableNodeProperty]
    public Expression Operand { get; }

    public bool IsPostfix { get; } // True if the operation is postfix (e.g. i++ vs ++i)

    public UnaryOpExpression(OperatorType op, Expression operand, bool isPostfix = false) {
        Operator  = op;
        Operand   = operand;
        IsPostfix = isPostfix;
    }

    public override ValueReference Execute(ExecContext ctx) {
        var resultRef = Operand.Execute(ctx);
        var value     = resultRef.Value;

        switch (Operator) {
            case OperatorType.Not: {
                var opRes  = value.Operator(Operator, null);
                return ctx.ValReference(opRes);
            }
            case OperatorType.Increment:
            case OperatorType.Decrement: {
                var opRes = value.Operator(Operator, Value.Number(1));
                return ctx.ValReference(opRes);
            }
            case OperatorType.Minus: {
                var opRes = value.Operator(OperatorType.Multiply, Value.Int32(-1));
                return ctx.ValReference(opRes);                
            }
            default:
                throw new NotImplementedException($"Unhandled operator type: {Operator}");
        }
    }
}