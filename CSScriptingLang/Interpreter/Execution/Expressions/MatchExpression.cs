using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Values;

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

        MatchCaseNode matchingCase = null;

        foreach (var caseNode in Cases) {
            var pattern = caseNode.Pattern;

            if (pattern is LiteralPatternNode literalPattern) {
                try {
                    var literalValue = literalPattern.Literal.Execute(ctx);
                    if (literalValue.Value.Operator_Equal(exprRefValue)) {
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
                if (variableValue.Value.Operator_Equal(exprRefValue)) {
                    matchingCase = caseNode;
                    break;
                }
            }

            if (pattern is TypePatternNode typePattern) {
                var type = typePattern.ExpectedType.Get();
                if (type == null) {
                    ctx.LogError(typePattern, "Failed to get type from type pattern");
                    continue;
                }

                if (exprRefValue.Value.Type == type.Type) {
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
public partial class DefaultPatternNode : BasePatternMatchNode
{
    public DefaultPatternNode() { }
}

[ASTNode]
public partial class LiteralPatternNode : BasePatternMatchNode
{
    [VisitableNodeProperty]
    public Expression Literal { get; set; }

    public LiteralPatternNode(Expression literal) {
        Literal = literal;
    }
}

[ASTNode]
public partial class TypePatternNode : BasePatternMatchNode
{
    public TypeReference ExpectedType { get; set; }

    public TypePatternNode(string typeName) {
        ExpectedType = new TypeReference(this, typeName);
    }
}

[ASTNode]
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
public partial class MatchCaseNode : BaseNode
{
    [VisitableNodeProperty]
    public BasePatternMatchNode Pattern { get; set; }

    [VisitableNodeProperty]
    public Expression Body { get; set; }
}