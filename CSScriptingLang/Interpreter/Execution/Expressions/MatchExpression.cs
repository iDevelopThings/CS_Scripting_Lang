using System.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public partial class MatchExpression : Expression
{
    [VisitableNodeProperty]
    public Expression MatchAgainstExpr { get; set; }

    [VisitableNodeProperty]
    public List<MatchCaseNode> Cases { get; set; } = new();

    public MatchCaseNode DefaultCase { get; set; }

    public override ValueReference Execute(ExecContext ctx) {
        var exprRefValue = MatchAgainstExpr.Execute(ctx);
        var exprValue    = exprRefValue.Value;

        MatchCaseNode matchingCase = null;

        foreach (var caseNode in Cases) {
            var pattern = caseNode.Pattern;

            if (pattern is LiteralPatternNode literalPattern) {
                try {
                    var literalValue = literalPattern.Literal.Execute(ctx);
                    if (literalValue.Value.Operator_Equal(exprValue, true)) {
                        matchingCase = caseNode;
                        break;
                    }
                }
                catch (FormatException) {
                    // Handles cases where we try to convert a string like `hi' to a number
                    continue;
                }
            }

            if (pattern is IdentifierPatternNode variablePattern) {
                var variableValue = variablePattern.Variable.Execute(ctx);
                if (variableValue.Value.Operator_Equal(exprValue, true)) {
                    matchingCase = caseNode;
                    break;
                }
            }

            if (pattern is TypePatternNode typePattern) {
                var type = typePattern.ExpectedType.ResolveType();
                if (type == null) {
                    ctx.LogError(typePattern, "Failed to get type from type pattern");
                    continue;
                }

                if (exprValue.Type == type.ValueType.ForType) {
                    matchingCase = caseNode;
                    break;
                }
            }

        }

        if (matchingCase == null) {
            matchingCase = DefaultCase;
        }

        if (DefaultCase == null) {
            ctx.LogError(this, "No matching case found");
        }

        return matchingCase.Body.Execute(ctx);
    }
}

[ASTNode]
public abstract partial class BasePatternMatchNode : Expression { }

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public partial class DefaultPatternNode : BasePatternMatchNode
{
    public DefaultPatternNode() { }
}

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public partial class LiteralPatternNode : BasePatternMatchNode
{
    [VisitableNodeProperty]
    public Expression Literal { get; set; }

    public LiteralPatternNode(Expression literal) {
        Literal = literal;
    }
}

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public partial class TypePatternNode : BasePatternMatchNode
{
    [VisitableNodeProperty]
    public TypeIdentifierExpression ExpectedType { get; set; }

    public TypePatternNode(TypeIdentifierExpression expectedType) {
        ExpectedType = expectedType;
        EndToken     = expectedType.EndToken;
    }
}

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public partial class IdentifierPatternNode : BasePatternMatchNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Variable { get; set; }

    public IdentifierPatternNode(IdentifierExpression variable) {
        Variable   = variable;
        StartToken = variable.StartToken;
        EndToken   = variable.EndToken;
    }
}

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public partial class MatchCaseNode : BaseNode
{
    [VisitableNodeProperty]
    public BasePatternMatchNode Pattern { get; set; }

    [VisitableNodeProperty]
    public Expression Body { get; set; }
}