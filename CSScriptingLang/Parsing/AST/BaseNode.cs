using System.Diagnostics;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Parsing.AST;

[ASTNode]
[DebuggerDisplay($"{{{nameof(ToSimpleDebugString)}(),nq}}")]
public abstract partial class BaseNode
{
    protected static Logger Logger = Logs.Get<BaseNode>();

    public Token StartToken { get; set; }
    public Token EndToken   { get; set; }

    public int ScriptId { get; set; }

    public BaseNode Parent   { get; set; }
    public BaseNode Previous { get; set; }
    public BaseNode Next     { get; set; }

    public DefinitionScope Scope { get; set; }

    public Script GetScript() => ModuleResolver.GetScriptById(ScriptId);

    public virtual string ToSimpleDebugString() {
        // var program = (this is ProgramNode ? this : Cursor.First.Parent<ProgramNode>()) as ProgramNode;

        try {
            var script = GetScript();
            if (script == null) {
                return $"Type({GetType().ToShortName()}): No script found.";
            }
            if (StartToken == null || EndToken == null) {
                return $"Type({GetType().ToShortName()}): No token range found.";
            }
            var src = script.Source;

            var (start, end) = FindTokenRange();
            var len = end.Range.End - start.Range.Start;
            // var len = Math.Max(0, end.Range.End - start.Range.Start);
            // len = Math.Min(len, script.Source.Length - start.Range.Start);
            if (end.Range.End > script.Source.Length) {
                src = script.WrappedSource;
            }

            if (start.Range.Start >= src.Length) {
                return $"Type({GetType().ToShortName()}): Start token range out of bounds.";
            }
            if (start.Range.Start + len > src.Length) {
                return $"Type({GetType().ToShortName()}): End token range out of bounds.";
            }

            var strRange = src?.Substring(start.Range.Start, len);

            return $"Type({GetType().ToShortName()}): '{strRange}'";
        }
        catch (FailedToFindTokenRangeException) {
            return $"Type({GetType().ToShortName()}): Failed to find token range.";
        }
    }
    public virtual void Accept(IAstVisitor visitor) { }
    public virtual IEnumerable<BaseNode> AllNodes() {
        yield break;
    }

    public BaseNode SetStartToken(Token start) {
        StartToken = start;
        return this;
    }
    public BaseNode SetEndToken(Token end) {
        EndToken = end;
        return this;
    }

    public List<Ty> ResolvedTypes { get; set; }
    public Ty       ResolvedType  => ResolvedTypes?.FirstOrDefault();

    public virtual IEnumerable<Ty> ResolveAndCacheTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if(ResolvedTypes != null)
            return ResolvedTypes;

        var resolved = ResolveTypes(ctx, symbol);
        ResolvedTypes = resolved.ToList();
        
        return ResolvedTypes;
    }

    public virtual IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        DiagnosticManager.Diagnostic_Warning().Message($"Failed to resolve type for node of type {GetType().ToFullLinkedName()}").Range(this).Report();
        Logger.Warning($"Failed to resolve type for node of type {GetType().ToFullLinkedName()}");
        // throw new FailedToGetRuntimeTypeException(this, $"Failed to resolve type for node of type {GetType().ToFullLinkedName()}");
        yield break;
    }

    public virtual IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        yield break;
    }

    public virtual ITypeAlias GetTypeAlias()
        => throw new FailedToGetRuntimeTypeException(this, $"Failed to get type alias for node of type {GetType().ToFullLinkedName()}");

    public class FailedToFindTokenRangeException : Exception
    {
        public FailedToFindTokenRangeException(string message) : base(message) { }
    }

    // Find the first node of type T in the tree.


}
