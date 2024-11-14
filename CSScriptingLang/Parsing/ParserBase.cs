using System.Runtime.CompilerServices;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils;
using CSScriptingLang.Core.Logging;

namespace CSScriptingLang.Parsing;

public class NodeScopeStack<T> : Stack<T> where T : BaseNode
{
    public T Current => Count > 0 ? Peek() : default;

    public UsingCallbackHandle Using(T node) {
        Push(node);
        return new UsingCallbackHandle(() => Pop());
    }
}

public enum ParserMode
{
    Default,
    PatternMatching,
}

public class ParserBase : SubParser
{
    protected new Logger Logger = Logs.Get<Parser>(LogLevel.Warning);

    public Stack<ParserMode> ModeStack { get; } = new();
    public ParserMode        Mode      => ModeStack.Count > 0 ? ModeStack.Peek() : ParserMode.Default;

    protected TNodeType ParseLiteral<TNodeType, TValueType>(string name, TokenType type, Func<Token, TValueType> Converter) where TNodeType : LiteralValueExpression {
        var start = EnsureAndConsume(type, $"Expected {name} literal");
        var value = Converter(start);
        var node  = (TNodeType) Activator.CreateInstance(typeof(TNodeType), value);
        if (node != null) {
            node.StartToken = start;
            node.EndToken   = Prev;
            return node;
        }

        throw new Exception("Failed to create node");
    }

    protected NullValueExpression ParseNull()    => ParseLiteral<NullValueExpression, string>("null", TokenType.Null, t => null);
    protected StringExpression    ParseString()  => ParseLiteral<StringExpression, string>("string", TokenType.String, t => t.Value);
    protected BooleanExpression   ParseBoolean() => ParseLiteral<BooleanExpression, bool>("boolean", TokenType.Boolean, t => bool.Parse(t.Value));
    protected LiteralNumberExpression ParseNumber() {
        // if (TryParseExpression(out var n)) {
        //     return (LiteralNumberExpression) n;
        // }
        
        var node = LiteralNumberExpression.CreateFromToken(Token);
        node.StartToken = Token;
        node.EndToken   = Token;
        Advance();
        return node;
    }

    protected ModuleDeclarationNode ParseModuleDeclaration() {
        var start = EnsureAndConsume(Keyword.Module, "Expected 'module' keyword");
        var name  = ParseString();

        AdvanceIfSemicolon();

        return new ModuleDeclarationNode() {
            StartToken = start,
            EndToken   = Prev,
            Name       = name,
        };
    }
    /// <summary>
    /// Parse a list of imports:
    /// <code>
    /// import "module";
    /// import "a.b.c";
    /// </code>
    /// </summary>
    /// <returns></returns>
    protected ImportStatementsNode ParseImportStatements() {
        var importsList = new ImportStatementsNode() {
            StartToken = Token,
        };

        while (Token.IsImportKeyword && !Next.IsEOF) {
            var start = EnsureAndConsume(Keyword.Import, "Expected 'import' keyword");
            var path  = ParseString();
            var end   = EnsureAndConsume(TokenType.Semicolon, "Expected semicolon after import statement");

            importsList.Add(new ImportStatementNode(path) {
                StartToken = start,
                EndToken   = end,
            });
        }

        importsList.EndToken = Prev;

        return importsList;
    }

    protected UsingCallbackHandle UsingMode(ParserMode mode) {
        ModeStack.Push(mode);
        return new UsingCallbackHandle(() => ModeStack.Pop());
    }


}