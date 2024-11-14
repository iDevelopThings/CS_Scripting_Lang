using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class MatchExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax Value => ChildNode<ExprSyntax>();

    public IEnumerable<MatchExprCase> Cases => ChildNodes<MatchExprCase>();
    public MatchExprCase DefaultCase => ChildNode<MatchExprCase>(c => c.Pattern is MatchExprPattern_Default);

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Value?.DebugContent())
       .ClearTrailingSpace();


    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var exprRefValue = Value.DoExecuteSingle(ctx).Value();
        var exprValue    = exprRefValue.Value;

        MatchExprCase matchingCase = null;

        foreach (var caseNode in Cases) {
            var pattern = caseNode.Pattern;

            if (pattern is MatchExprPattern_Literal literalPattern) {
                try {
                    var literalValue = literalPattern.Literal.DoExecuteSingle(ctx).Value();
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

            if (pattern is MatchExprPattern_Identifier variablePattern) {
                var variableValue = variablePattern.Variable.DoExecuteSingle(ctx).Value();
                if (variableValue.Value.Operator_Equal(exprValue, true)) {
                    matchingCase = caseNode;
                    break;
                }
            }

            if (pattern is MatchExprPattern_TypePattern typePattern) {
                var type = typePattern.ExpectedType.ResolveType();
                if (type == null) {
                    Diagnostic_Error_Fatal( "Failed to get type from type pattern");
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
            Diagnostic_Error_Fatal("No matching case found");
        }

        return matchingCase.Body.DoExecuteSingle(ctx).Value();
    }
}

[SyntaxNode]
public partial class MatchExprCase(int index, SyntaxTree tree) : ExprSyntax(index, tree)
{
    public MatchExprPattern Pattern => ChildNode<MatchExprPattern>();
    public Block            Body    => ChildNode<Block>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Pattern?.DebugContent())
       .Add("=>")
       .Add(Body?.DebugContent())
       .ClearTrailingSpace();
}

public abstract class MatchExprPattern(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public override string DebugContent() => DataContentBuilder.Create()
       .ClearTrailingSpace();


    public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
        return ValueReference.Nothing;
    }
}

[SyntaxNode]
public partial class MatchExprPattern_Default(int index, SyntaxTree tree) : MatchExprPattern(index, tree)
{
    public override string DebugContent() => DataContentBuilder.Create()
       .Add("case")
       .Add("Default")
       .ClearTrailingSpace();
}

[SyntaxNode]
public partial class MatchExprPattern_Literal(int index, SyntaxTree tree) : MatchExprPattern(index, tree)
{
    public ExprSyntax Literal => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("case")
       .Add(Literal?.DebugContent())
       .ClearTrailingSpace();
}

[SyntaxNode]
public partial class MatchExprPattern_TypePattern(int index, SyntaxTree tree) : MatchExprPattern(index, tree)
{
    public TypedIdentifierExpr ExpectedType => ChildNode<TypedIdentifierExpr>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("case")
       .Add(ExpectedType?.DebugContent())
       .ClearTrailingSpace();
}

[SyntaxNode]
public partial class MatchExprPattern_Identifier(int index, SyntaxTree tree) : MatchExprPattern(index, tree)
{
    public IdentifierExpr Variable => ChildNode<IdentifierExpr>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("case")
       .Add(Variable?.DebugContent())
       .ClearTrailingSpace();
}