using System.Collections;
using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

public interface IDeclaration
{
    Prototype Prototype { get; set; }
    Prototype HandleDeclaration(ExecContext ctx);
}

[SyntaxNode]
public partial class VariableDecl(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecMulti, INamedSymbolProvider
{
    public bool IsTupleVar => HasChildElement<TupleExpr>();
    public bool IsRange    => HasKeywordToken(Keyword.Range);

    // If it's a tuple type, we should have `var (x, y) = (1, 2);`
    // Ie, if we have tuple var, we should have a tuple value
    public TupleExpr TupleVar =>
        ChildNodesBeforeToken<TupleExpr>(t => t is OperatorToken {Operator: OperatorType.Assignment}).FirstOrDefault();
    // ChildNode<TupleExpr>();
    public ExprSyntax TupleValue =>
        ChildNodesAfterToken<ExprSyntax>(t => t is OperatorToken {Operator: OperatorType.Assignment}).FirstOrDefault()
     ?? ChildNodesAfterToken<TupleExpr>(t => t is OperatorToken {Operator : OperatorType.Assignment}).FirstOrDefault();
    // ChildAfterOrNull<TupleExpr>(TupleVar);

    public IEnumerable<IdentifierExpr> Vars => TupleVar != null
        ? TupleVar.Elements.OfType<IdentifierExpr>()
        : ChildNodesBeforeToken<IdentifierExpr>(t => t is OperatorToken {Operator: OperatorType.Assignment});

    /*IsTupleVar
    ? TupleVar.Elements.OfType<IdentifierExpr>()
    : ChildNodesBeforeToken<IdentifierExpr>(t => t is OperatorToken {Operator: OperatorType.Assignment});*/

    public IEnumerable<ExprSyntax> Values => TupleValue is TupleExpr tupleExpr
        ? tupleExpr.Elements
        : ChildNodesAfterToken<ExprSyntax>(t => t is OperatorToken {Operator: OperatorType.Assignment});
    /*IsTupleVar
    ? TupleValue.Elements
    : ChildNodesAfterToken<ExprSyntax>(t => t is OperatorToken {Operator: OperatorType.Assignment});*/

    public SyntaxToken Assign => ChildToken(TokenType.Assignment);

    public IEnumerable<(IdentifierExpr var, ExprSyntax value)> VarValuePairs {
        get {
            foreach (var (var, value) in Vars.Zip(Values)) {
                yield return (var, value);
            }
        }
    }

    public override string DebugContent() {
        if (IsTupleVar) {
            return DataContentBuilder.Create($"VarDecl(Tuple{(IsRange ? " Range" : "")})")
               .Add(TupleVar.DebugContent())
               .Add("=")
               .AddIf(IsRange, "range")
               .Add(TupleValue.DebugContent())
               .ClearTrailingSpace();
        }

        return DataContentBuilder.Create($"VarDecl{(IsRange ? "(Range)" : "")}")
           .Add(Vars.Select(v => v.DebugContent()).Join())
           .Add("=")
           .AddIf(IsRange, "range")
           .Add(Values.Select(e => e.DebugContent()).Join())
           .ClearTrailingSpace();
    }

    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        if (Values.Any(v => v is RangeExpr)) {
            var rangeExpr = Values.OfType<RangeExpr>().First();
            var range     = ctx.ExecuteRValue(rangeExpr.DoExecuteSingle);

            yield return range;

            yield break;
        }

        foreach (var (var, val) in VarValuePairs) {
            var name = var.Name;

            if (ctx.Variables.Get(name, out var symbol)) {
                if (!symbol.IsBaseDeclaration) {
                    ctx.LogError(this, $"Variable '{name}' already declared");
                }
            }

            if (val is LiteralValueExpr literal) {
                var valueResult = literal.DoExecuteSingle(ctx).Value();
                var varSymbol   = ctx.Variables.Declare(name, valueResult.Value);

                ctx.CurrentCallFrame?.AddLocal(varSymbol);

                yield return ctx.VariableAccessReference(varSymbol).ToMaybe();
            } else {
                var valueResult = ctx.ExecuteRValue(val.DoExecuteSingle);
                var varSymbol   = ctx.Variables.Set(name, valueResult.Value?.GetOrClone() ?? Value.Null());

                ctx.CurrentCallFrame?.AddLocal(varSymbol);

                yield return ctx.VariableAccessReference(varSymbol).ToMaybe();
            }

        }
    }
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        foreach (var expr in Values) {
            foreach (var t in expr.ResolveAndCacheTypes(ctx, symbol)) {
                yield return t;
            }
        }
    }

    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        var s = scope ?? Scope;
        return VarValuePairs.SelectMany(x => x.var.FindReferences(s));
    }
    
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
        foreach (var (var, value) in VarValuePairs) {
            yield return new NamedSymbolInformation(var, var.Name, NamedSymbolKind.Variable, var);
        }
    }
}

[SyntaxNode]
public partial class ArgumentDeclarationList(int index, SyntaxTree tree) : ExprSyntax(index, tree), IEnumerable<ArgumentDeclaration>
{
    public IEnumerable<ArgumentDeclaration> Arguments => ChildNodes<ArgumentDeclaration>();

    public override string DebugContent() => $"({Arguments.Select(e => e.DebugContent()).Join()})";

    public IEnumerator<ArgumentDeclaration> GetEnumerator() => Arguments.GetEnumerator();
    IEnumerator IEnumerable.                GetEnumerator() => GetEnumerator();
}

[SyntaxNode]
public partial class ArgumentDeclaration(int index, SyntaxTree tree) : ExprSyntax(index, tree), INamedSymbolProvider
{
    public bool                IsVarArg => ChildToken(TokenType.DotDotDot) != null;
    public TypedIdentifierExpr Type     => ChildNode<TypedIdentifierExpr>();
    public IdentifierExpr      Name     => Type != null ? ChildAfter<IdentifierExpr>(Type) : null;

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add(IsVarArg ? "..." : "")
           .Add(Type?.DebugContent())
           .Add(Name?.DebugContent())
           .ClearTrailingSpace();
    }
    
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        var type = Type.ResolveAndCacheTypes(ctx, symbol);

        return type;
    }
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
        yield return new NamedSymbolInformation(this, Name, NamedSymbolKind.Variable, Name);
    }
    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        Debugger.Launch();
        var s = scope ?? Scope;
        if (s.TryGetSymbol(Name, out var def)) {
            return def.Named;
        }
        return ResolvedTypes.SelectMany(type => type.NamedSymbols);
    }
}

[SyntaxNode]
public partial class FunctionDecl : ExprSyntax, IExecSingle, INamedSymbolProvider
{
    private static int _idCounter = 0;

    private bool _isDef;
    private bool _isAsync;
    private bool _isSeq;
    private bool _isCoroutine;
    private int  _fnId;

    public bool IsDef {
        get => _isDef || HasKeywordToken(Keyword.Def);
        set => _isDef = value;
    }
    public bool IsAsync {
        get => _isAsync || HasKeywordToken(Keyword.Async);
        set => _isAsync = value;
    }
    public bool IsSeq {
        get => _isSeq || HasKeywordToken(Keyword.Seq);
        set => _isSeq = value;
    }
    public bool IsCoroutine {
        get => _isCoroutine || HasKeywordToken(Keyword.Coroutine);
        set => _isCoroutine = value;
    }
    public bool IsInlineFn { get; set; }

    public IdentifierExpr          NameIdentifier => ChildNode<IdentifierExpr>();
    public ArgumentDeclarationList Arguments      => ChildNode<ArgumentDeclarationList>();
    public TypedIdentifierExpr     ReturnType     => ChildAfter<TypedIdentifierExpr>(Arguments);
    public Block                   Body           => ChildNode<Block>();

    public string Name => NameIdentifier?.Name?.RepresentText ?? $"__anon_fn_{_fnId}";

    public FunctionDecl(
        int        index,
        SyntaxTree tree,
        bool       isInlineFn = false
    ) : base(index, tree) {
        IsInlineFn = isInlineFn;
        _fnId      = _idCounter++;
    }

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add(IsDef ? "def" : "")
           .Add(Name, false)
           .Add(Arguments?.DebugContent())
           .Add(ReturnType?.DebugContent())
           .Add(Body?.DebugContent() ?? ";")
           .ClearTrailingSpace();
    }

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var (val, symbol) = ctx.DeclareFunction(this);

        return ctx.VariableAccessReference(symbol);
    }
    public IEnumerable<NamedSymbolInformation> GetNamedSymbols() {
        if (NameIdentifier != null)
            yield return new NamedSymbolInformation(this, Name, NamedSymbolKind.Function, NameIdentifier);
    }
}

[SyntaxNode]
public partial class AttributeDecl(int index, SyntaxTree tree) : SyntaxNode(index, tree)
{
    public IdentifierExpr          Name      => ChildNode<IdentifierExpr>();
    public IEnumerable<ExprSyntax> Arguments => ChildNodes<ExprSyntax>();

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("[")
           .Add(Name.DebugContent())
           .Add(Arguments.Select(a => a.DebugContent()).Join())
           .Add("]")
           .ClearTrailingSpace();
    }
}