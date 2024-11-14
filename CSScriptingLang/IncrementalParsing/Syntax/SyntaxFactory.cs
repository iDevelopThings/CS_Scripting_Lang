using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.IncrementalParsing.Tree;
using CSScriptingLang.Interpreter.Execution.Declaration;

namespace CSScriptingLang.IncrementalParsing.Syntax;

public static class SyntaxFactory
{
    public class UnhandledSyntaxKindException(
        SyntaxKind kind,
        string     message
    ) : Exception(message)
    {
        public SyntaxKind Kind { get; } = kind;
    }

    public static SyntaxElement CreateSyntax(int index, SyntaxTree tree) {
        var flags      = tree.GetFlags(index);
        var rawKind    = tree.GetRawKind(index);
        var syntaxKind = (SyntaxKind) rawKind;
        var tokenKind  = (TokenType) rawKind;

        if (flags == NodeFlags.Node) {
            return syntaxKind switch {
                SyntaxKind.Source  => new SourceSyntax(index, tree),
                SyntaxKind.Block   => new Block(index, tree),
                SyntaxKind.Comment => new CommentSyntax(index, tree),
                SyntaxKind.Trivia  => new PlaceholderSyntaxNode(index, tree),

                SyntaxKind.BinaryOpExpression       => new BinaryOpExpr(index, tree),
                SyntaxKind.PrefixUnaryOpExpression  => new UnaryOpExpr(index, tree, false),
                SyntaxKind.PostfixUnaryOpExpression => new UnaryOpExpr(index, tree, true),
                SyntaxKind.BooleanExpression        => new BooleanExpr(index, tree),
                SyntaxKind.NullValueExpression      => new NullValueExpr(index, tree),
                SyntaxKind.StringExpression         => new StringExpr(index, tree),
                SyntaxKind.Int32Expression          => new Int32Expr(index, tree),
                SyntaxKind.Int64Expression          => new Int64Expr(index, tree),
                SyntaxKind.FloatExpression          => new FloatExpr(index, tree),
                SyntaxKind.DoubleExpression         => new DoubleExpr(index, tree),
                SyntaxKind.ObjectLiteralExpression  => new ObjectLiteralExpr(index, tree),
                SyntaxKind.ObjectProperty           => new ObjectPropertyExpr(index, tree),
                SyntaxKind.ArrayLiteralExpression   => new ArrayLiteralExpr(index, tree),
                SyntaxKind.RangeExpression          => new RangeExpr(index, tree),

                SyntaxKind.IdentifierExpression     => new IdentifierExpr(index, tree),
                SyntaxKind.TypeIdentifierExpression => new TypedIdentifierExpr(index, tree),

                SyntaxKind.MemberAccessExpression => new MemberAccessExpr(index, tree),
                SyntaxKind.IndexAccessExpression  => new IndexAccessExpr(index, tree),

                SyntaxKind.TupleExpression => new TupleExpr(index, tree),
                SyntaxKind.CallExpression  => new CallExpr(index, tree),
                SyntaxKind.ArgumentList    => new ArgumentListExpr(index, tree),

                SyntaxKind.VariableDeclaration       => new VariableDecl(index, tree),
                SyntaxKind.ArgumentListDeclaration   => new ArgumentDeclarationList(index, tree),
                SyntaxKind.ArgumentDeclaration       => new ArgumentDeclaration(index, tree),
                SyntaxKind.FunctionDeclaration       => new FunctionDecl(index, tree),
                SyntaxKind.InlineFunctionDeclaration => new FunctionDecl(index, tree, true),

                SyntaxKind.StructDeclaration            => new StructDecl(index, tree),
                SyntaxKind.StructDeclarationField       => new TypeDeclMember(index, tree, TypeDeclMemberType.Field),
                SyntaxKind.StructDeclarationMethod      => new TypeDeclMember(index, tree, TypeDeclMemberType.Method),
                SyntaxKind.StructDeclarationConstructor => new TypeDeclMember(index, tree, TypeDeclMemberType.Constructor),

                SyntaxKind.EnumDeclaration                     => new EnumDecl(index, tree),
                SyntaxKind.EnumMemberConstructor               => new TypeDeclMember(index, tree, TypeDeclMemberType.Constructor),
                SyntaxKind.EnumMemberMethod                    => new TypeDeclMember(index, tree, TypeDeclMemberType.Method),
                SyntaxKind.EnumMemberDeclaration               => new TypeDeclMember(index, tree, TypeDeclMemberType.EnumMember),
                SyntaxKind.EnumMemberDeclaration_WithValue     => new TypeDeclMember(index, tree, TypeDeclMemberType.EnumMember),
                SyntaxKind.EnumMemberDeclaration_WithValueCtor => new TypeDeclMember(index, tree, TypeDeclMemberType.EnumMember),

                SyntaxKind.InterfaceDeclaration => new InterfaceDecl(index, tree),

                SyntaxKind.SignalDeclaration => new SignalDecl(index, tree),

                SyntaxKind.TypeParametersList => new TypeParameterList(index, tree),
                SyntaxKind.TypeParameter      => new TypeParameter(index, tree),

                SyntaxKind.IfStatement => new IfStatement(index, tree),
                SyntaxKind.IfClause    => new IfClause(index, tree),

                SyntaxKind.ForIndexedLoop => new ForIndexedLoop(index, tree),
                SyntaxKind.ForWhileLoop   => new ForWhileLoop(index, tree),
                SyntaxKind.ForRange       => new ForRangeLoop(index, tree),

                SyntaxKind.MatchExpression         => new MatchExpr(index, tree),
                SyntaxKind.MatchCase               => new MatchExprCase(index, tree),
                SyntaxKind.MatchPattern_Default    => new MatchExprPattern_Default(index, tree),
                SyntaxKind.MatchPattern_Literal    => new MatchExprPattern_Literal(index, tree),
                SyntaxKind.MatchPattern_IsType     => new MatchExprPattern_TypePattern(index, tree),
                SyntaxKind.MatchPattern_Identifier => new MatchExprPattern_Identifier(index, tree),

                SyntaxKind.AwaitStatement    => new AwaitStatement(index, tree),
                SyntaxKind.DeferStatement    => new DeferStatement(index, tree),
                SyntaxKind.ReturnStatement   => new ReturnStatement(index, tree),
                SyntaxKind.BreakStatement    => new BreakStatement(index, tree),
                SyntaxKind.ContinueStatement => new ContinueStatement(index, tree),
                SyntaxKind.YieldStatement    => new YieldStatement(index, tree),


                _ =>
                    SyntaxTree.createPlaceholderElements
                        ? new PlaceholderSyntaxNode(index, tree)
                        : throw new UnhandledSyntaxKindException(syntaxKind, "Unhandled syntax kind: " + syntaxKind),
            };
        }

        if ((tokenKind & TokenType.String) != 0)
            return new StringToken(index, tree);
        if ((tokenKind & TokenType.Boolean) != 0)
            return new BooleanToken(index, tree);
        if ((tokenKind & TokenType.Null) != 0)
            return new NullValueToken(index, tree);
        if ((tokenKind & TokenType.Int32) != 0)
            return new Int32Token(index, tree);
        if ((tokenKind & TokenType.Int64) != 0)
            return new Int64Token(index, tree);
        if ((tokenKind & TokenType.Float) != 0)
            return new FloatToken(index, tree);
        if ((tokenKind & TokenType.Double) != 0)
            return new DoubleToken(index, tree);
        if ((tokenKind & TokenType.Identifier) != 0 && (tokenKind & TokenType.Keyword) == 0)
            return new NameToken(index, tree);
        if ((tokenKind & TokenType.Whitespace) != 0)
            return new WhitespaceToken(index, tree);
        if ((tokenKind & TokenType.Operator) != 0)
            return new OperatorToken(index, tree);
        if ((tokenKind & TokenType.NewLine) != 0)
            return new NewLineToken(index, tree);

        return new SyntaxToken(index, tree);
    }

    /*private static bool       IsNode(long       rawKind) => rawKind >> 16 == 1;
    private static SyntaxKind GetSyntaxKind(int rawKind) => (SyntaxKind) (rawKind & 0xFFFF);
    private static TokenType  GetTokenKind(int  rawKind) => (TokenType) (rawKind & 0xFFFF);*/
}