using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public class NameToken : SyntaxToken
{
    public new string RepresentText => Text.ToString().Trim();

    public NameToken(int index, SyntaxTree tree) : base(index, tree) {
    }
    
    public static implicit operator string(NameToken expr)
        => expr.RepresentText ?? string.Empty;
}

[SyntaxNode]
public class WhitespaceToken(int index, SyntaxTree tree) : SyntaxToken(index, tree);

[SyntaxNode]
public class NewLineToken(int index, SyntaxTree tree) : SyntaxToken(index, tree);

[SyntaxNode]
public class OperatorToken(int index, SyntaxTree tree) : SyntaxToken(index, tree)
{
    public OperatorType               Operator     => OperatorTypes.TokenToOperatorType[RepresentText.Trim()];
    public OperatorTypes.OperatorInfo OperatorInfo => OperatorTypes.Info[Operator];

    public override string DebugContent() => DataContentBuilder.Create()
       .Add($"{Operator.ToString()}({OperatorInfo.Token})")
       .ClearTrailingSpace();
}