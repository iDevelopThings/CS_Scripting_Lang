using System.Reflection;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.IncrementalParsing.Tree;
using CSScriptingLang.IncrementalParsing.Tree.Green;
using CSScriptingLang.IncrementalParsing.Tree.Red;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;
using SharpX.Extensions;

namespace CSScriptingLang.IncrementalParsing.Syntax;

public class SyntaxTree
{
    private static Logger Logger = Logs.Get<SyntaxTree>();

    public const bool createPlaceholderElements = true;

    private List<RedNode>                  RedNodes       { get; }
    private Dictionary<int, SyntaxElement> SyntaxElements { get; } = new();
    public  Script                         Script         { get; }
    public  List<Diagnostic>               Diagnostics    { get; }

    // When false, this will be a script, when true, it's a function with our script inside
    public bool IsWrappedScript { get; set; }

    public SourceSyntax SyntaxRoot => (GetElement(0) as SourceSyntax)!;
    
    public IEnumerable<T> GetElements<T>() where T : SyntaxElement {
        return SyntaxRoot.Descendants.OfType<T>();
    }
    public T GetElement<T>() where T : SyntaxElement
        => GetElements<T>().FirstOrDefault();
    public T GetElement<T>(int idx) where T : SyntaxElement
        => GetElements<T>().ElementAtOrDefault(idx);
    public T GetElement<T>(Func<T, bool> predicate) where T : SyntaxElement
        => GetElements<T>().FirstOrDefault(predicate);

    public static SyntaxTree ParseText(string src, bool isModule) {
        var script = ModuleResolver.CreateVirtualScript(src, true);
        return Create(script, isModule);
    }
    public static SyntaxTree ParseText(Script script, bool isModule) {
        return Create(script, isModule);
    }

    public static SyntaxTree Create(Script script, bool isModule) {
        SyntaxTree syntaxTree = null;

        var parser = new IncrementalParser(script);

        {
            using var _ = TimedScope.Scoped_Print("SyntaxTree::Create");

            var greenBuilder = new GreenTreeBuilder(parser);
            var (root, diagnostics, count) = greenBuilder.Build();

            var redTreeBuilder = new RedTreeBuilder(parser);
            var redNodes       = redTreeBuilder.Build(root, count);

            syntaxTree = new SyntaxTree(script, redNodes, diagnostics) {
                IsWrappedScript = isModule,
            };
        }

        // var debugGreen  = syntaxTree.SyntaxRoot.DebugGreenInspect();
        // var debugSyntax = syntaxTree.SyntaxRoot.DebugSyntaxInspect(true);

        {
            using var _ = TimedScope.Scoped_Print("Dump syntax tree");

            if (syntaxTree.SyntaxRoot.Descendants.OfType<PlaceholderSyntaxNode>().Any()) {
                Console.WriteLine('-'.Replicate(80));
                Console.WriteLine("Syntax Tree has Placeholder elements: ");
                syntaxTree.SyntaxRoot.Descendants.OfType<PlaceholderSyntaxNode>().ForEach(
                    node => {
                        var debugSyntaxFull = node.DebugSyntaxInspect(true, true, true);
                        Console.WriteLine(debugSyntaxFull);
                    }
                );
                Console.WriteLine(@"Implement them inside: F:\c#\CSScriptingLang\CSScriptingLang\IncrementalParsing\Syntax\SyntaxFactory.cs:17");
                Console.WriteLine('-'.Replicate(80));
                var debugSyntaxFull = syntaxTree.SyntaxRoot.DebugSyntaxInspect();
                Console.WriteLine(debugSyntaxFull);
                Console.WriteLine('-'.Replicate(80));
            }

            Console.WriteLine($"Syntax Tree ->\n{syntaxTree.SyntaxRoot.DebugCompactInspect(true)}");
            // Console.WriteLine($"Syntax Tree ->\n{syntaxTree.SyntaxRoot.DebugSyntaxInspect(true)}");
        }

        foreach (var diagnostic in syntaxTree.Diagnostics) {
            Logger.Error($"Diagnostic: {diagnostic}", diagnostic.SourceCallerInfo);
        }

        return syntaxTree;
    }


    private SyntaxTree(Script script, List<RedNode> redNodes, List<Diagnostic> diagnostics) {
        Script      = script;
        RedNodes    = redNodes;
        Diagnostics = diagnostics;
        InitNodes();
    }

    private void InitNodes() {
        // TokenAnalyzer.Analyze(RedNodes.Count, this);
        // BinderAnalyzer.Analyze(SyntaxRoot, this);
    }

    public SyntaxElement GetElement(int elementId) {
        if (elementId < 0 || elementId >= RedNodes.Count)
            return null;

        if (SyntaxElements.TryGetValue(elementId, out var syntaxElement))
            return syntaxElement;

        SyntaxElements[elementId] = SyntaxFactory.CreateSyntax(elementId, this);

        return SyntaxElements[elementId];
    }

    public bool GetRedNode(int elementId, out RedNode redNode) {
        if (elementId < 0 || elementId >= RedNodes.Count) {
            redNode = RedNode.Empty;
            return false;
        }

        redNode = RedNodes[elementId];
        return true;
    }

    public long GetRawKind(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.RawKind : 0;

    public NodeFlags GetFlags(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.Flag : NodeFlags.None;

    public bool IsNode(int elementId)
        => GetRedNode(elementId, out var redNode) && redNode.Flag == NodeFlags.Node;

    public bool IsToken(int elementId)
        => GetRedNode(elementId, out var redNode) && redNode.Flag == NodeFlags.Token;

    public TokenType GetTokenType(int elementId)
        => GetFlags(elementId) == NodeFlags.Token ? (TokenType) GetRawKind(elementId) : TokenType.None;
    /*{
        if (elementId < 0 || elementId >= RedNodes.Count)
            return TokenType.None;

        var flag = GetFlags(elementId);
        if (flag == NodeFlags.Token)
            return (TokenType) GetRawKind(elementId);

        return TokenType.None;
    }*/

    public SyntaxKind GetSyntaxKind(int elementId)
        => GetFlags(elementId) == NodeFlags.Node ? (SyntaxKind) GetRawKind(elementId) : SyntaxKind.None;
    /*{
        if (elementId < 0 || elementId >= RedNodes.Count)
            return SyntaxKind.None;

        var flag = GetFlags(elementId);
        if (flag == NodeFlags.Node)
            return (SyntaxKind) GetRawKind(elementId);
        return SyntaxKind.None;
    }*/

    public int GetParent(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.Parent : -1;

    public SourceRange GetSourceRange(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.SourceRange : SourceRange.Empty;

    public int GetChildStart(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.ChildStart : -1;

    public int GetChildEnd(int elementId)
        => GetRedNode(elementId, out var redNode) ? redNode.ChildEnd : -1;

    public void PushDiagnostic(Diagnostic diagnostic) {
        Diagnostics.Add(diagnostic);
    }

}