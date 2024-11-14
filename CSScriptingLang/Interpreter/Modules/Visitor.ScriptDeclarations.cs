using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using TypeDeclaration = CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes.TypeDeclaration;

namespace CSScriptingLang.Interpreter.Modules;

public record DefinitionSymbol
{
    public VariableSymbol Symbol { get; init; }
    public string         Name   => Symbol.Name;

    public List<NamedSymbolInformation> Named { get; init; }
    public List<Ty>                     Types { get; set; } = new();
}

public class DefinitionScope : IDisposable
{
    private ExecContext Ctx { get; set; }

    public Dictionary<string, DefinitionSymbol>              Symbols          { get; } = new();


    public static DefinitionScope GlobalScope;

    public static DefinitionScope InitGlobalScope(ExecContext ctx) {
        return GlobalScope = new DefinitionScope(ctx, null);
    }

    public DefinitionScope ParentScope { get; private set; }

    public DefinitionScope(ExecContext ctx, DefinitionScope parentScope) {
        Ctx         = ctx;
        ParentScope = parentScope;
    }

    public DefinitionScope Enter(BaseNode node) {
        Ctx.PushScope();
        var scope = new DefinitionScope(Ctx, this);
        node.Scope = scope;
        return scope;
    }
    public DefinitionScope Enter(SyntaxNode node) {
        Ctx.PushScope();
        var scope = new DefinitionScope(Ctx, this);
        node.Scope = scope;
        return scope;
    }
    public DefinitionScope Exit() {
        Ctx.PopScope();

        return ParentScope;
    }

    public bool TryGetSymbol(string name, out DefinitionSymbol symbol) {
        if (Symbols.TryGetValue(name, out symbol)) {
            return true;
        }
        return ParentScope?.TryGetSymbol(name, out symbol) ?? false;
    }

    public void Dispose() { }

    public void Declare(VariableSymbol symbol, BaseNode node, Func<DefinitionSymbol, DefinitionSymbol> onDeclare = null) {
        node.Scope = this;

        var s = new DefinitionSymbol() {
            Symbol = symbol,
            Named  = new(),
        };

        s.Types = node.ResolveAndCacheTypes(Ctx, s).ToList();

        if (node is INamedSymbolProvider provider) {
            var i = 0;
            foreach (var ns in provider.GetNamedSymbols()) {
                var namedSymbol = ns;
                if (s.Types.Count > i) {
                    namedSymbol.Type = s.Types[i];
                    s.Types[i].NamedSymbols.Add(namedSymbol);
                }
                s.Named.Add(namedSymbol);
                i++;
            }
        }

        s = onDeclare?.Invoke(s) ?? s;

        Symbols[symbol.Name] = s;

    }
    public void Declare(VariableSymbol symbol, SyntaxNode node, Func<DefinitionSymbol, DefinitionSymbol> onDeclare = null) {
        node.Scope = this;

        var s = new DefinitionSymbol() {
            Symbol = symbol,
            Named  = new(),
        };

        s.Types = node.ResolveAndCacheTypes(Ctx, s).ToList();

        if (node is INamedSymbolProvider provider) {
            var i = 0;
            foreach (var ns in provider.GetNamedSymbols()) {
                var namedSymbol = ns;
                if (s.Types.Count > i && s.Types[i] != null) {
                    namedSymbol.Type = s.Types[i];
                    s.Types[i].NamedSymbols.Add(namedSymbol);
                }
                s.Named.Add(namedSymbol);
                i++;
            }
        }

        s = onDeclare?.Invoke(s) ?? s;

        Symbols[symbol.Name] = s;

    }
    public void Declare(TypeDeclaration node, Prototype type) {
        node.Scope = this;

        var s = new DefinitionSymbol() {
            Symbol = new VariableSymbol(node.Name, type.Proto),
            Named  = new(),
        };

        s.Types = node.ResolveAndCacheTypes(Ctx, s).ToList();

        if (node is INamedSymbolProvider provider) {
            var i = 0;
            foreach (var ns in provider.GetNamedSymbols()) {
                var namedSymbol = ns;
                if (s.Types.Count > i) {
                    namedSymbol.Type = s.Types[i];
                    s.Types[i].NamedSymbols.Add(namedSymbol);
                }
                s.Named.Add(namedSymbol);
                i++;
            }
        }

        Symbols[s.Symbol.Name] = s;
    }
}

public class ScriptDeclarationVisitor : BaseAstVisitor
{
    public Script      Script { get; set; }
    public ExecContext Ctx    { get; set; }

    protected Stack<BaseNode> BlockNodeStack = new();

    public DefinitionScope Scope { get; private set; }

    protected UsingCallbackHandle PushBlockNode(BaseNode node) {
        BlockNodeStack.Push(node);
        return new UsingCallbackHandle(() => BlockNodeStack.Pop());
    }
    protected UsingCallbackHandle PushScope(BaseNode node) {
        BlockNodeStack.Push(node);
        Scope = Scope.Enter(node);

        return new UsingCallbackHandle(() => {
            Scope = Scope.Exit();
            BlockNodeStack.Pop();
        });
    }

    public ScriptDeclarationVisitor(ExecContext ctx, Script script) {
        Ctx    = ctx;
        Script = script;

        Scope                = DefinitionScope.InitGlobalScope(Ctx);
        Script.Program.Scope = Scope;
    }

    public HashSet<ITopLevelDeclarationNode> TopLevelDeclarations => Script.Declarations.TopLevelDeclarations;
    public HashSet<StructPrototype>          StructTypes          => Script.Declarations.StructTypes;

    public HashSet<SignalPrototype>             SignalTypes             => Script.Declarations.SignalTypes;
    public HashSet<Value>                       FunctionTypes           => Script.Declarations.FunctionTypes;
    public HashSet<DefDeclaration_FunctionNode> DefFunctionDeclarations => Script.Declarations.DefFunctionDeclarations;
    public HashSet<VariableSymbol>              VariableSymbols         => Script.Declarations.VariableDeclarations;

    public HashSet<VariableSymbol> Exports        => Script.Declarations.Exports;
    public HashSet<VariableSymbol> PrivateExports => Script.Declarations.PrivateExports;

    public override void OnVisitAny(BaseNode node) {
        base.OnVisitAny(node);

        if (node.Scope == null)
            node.Scope = Scope;

        if (node is ITopLevelDeclarationNode decl) {
            TopLevelDeclarations.Add(decl);

            decl.DeclarationContext.Set(Script);
        }

        if (node is INamedSymbolProvider provider) {
            ModuleResolver.NamedSymbols.Add(provider);
        }
    }

    public override void VisitBlockExpression(BlockExpression node) {
        if (BlockNodeStack.Count == 0 && Script.IsWrappedModule) {
            base.VisitBlockExpression(node);
            return;
        }

        using var __ = PushScope(node);

        base.VisitBlockExpression(node);
    }

    public override void VisitDefDeclaration_FunctionNode(DefDeclaration_FunctionNode node) {
        base.VisitDefDeclaration_FunctionNode(node);

        if (BlockNodeStack.Count > 0)
            return;

        DefFunctionDeclarations.Add(node);
    }

    public bool IsValidExportName(string name) {
        return char.IsUpper(name[0]);
    }
    public void TryExport(VariableSymbol symbol) {
        if (IsValidExportName(symbol.Name)) {
            Exports.Add(symbol);
        } else {
            PrivateExports.Add(symbol);
        }
    }

    public override void VisitFunctionDeclaration(FunctionDeclaration node) {

        using var _ = PushScope(node);

        var (fnValue, symbol) = Ctx.DeclareFunction(node);

        foreach (var param in node.Parameters) {
            var varSymbol = Ctx.Variables.Declare(param.Name, () => new VariableSymbol(param.Name) {
                IsBaseDeclaration = true,
            });

            Scope.Declare(varSymbol, param);
        }

        if (BlockNodeStack.Count == 0) {
            TryExport(symbol);
            FunctionTypes.Add(fnValue);
        }

        base.VisitFunctionDeclaration(node);
        // if (BlockNodeStack.Count > 0)
        // return;

    }

    public override void VisitSignalDeclaration(SignalDeclaration node) {
        base.VisitSignalDeclaration(node);

        if (BlockNodeStack.Count > 0)
            return;

        var type = node.HandleDeclaration(Ctx);

        SignalTypes.Add(type);
    }

    public override void VisitVariableDeclarationNode(VariableDeclarationNode node) {
        base.VisitVariableDeclarationNode(node);

        // if (BlockNodeStack.Count > 0)
        // return;

        foreach (var symbol in Ctx.DeclareVariable(node)) {
            VariableSymbols.Add(symbol);

            Scope.Declare(symbol, node);

            if (BlockNodeStack.Count == 0)
                TryExport(symbol);
        }
    }
    public override void VisitStructDeclaration(StructDeclaration node) {
        using var _ = PushBlockNode(node);

        base.VisitStructDeclaration(node);
        if (BlockNodeStack.Count > 1)
            return;

        var type = node.HandleDeclaration(Ctx);

        StructTypes.Add(type);
    }
    public override void VisitEnumDeclaration(EnumDeclaration node) {
        using var _ = PushBlockNode(node);

        base.VisitEnumDeclaration(node);

        if (BlockNodeStack.Count > 1)
            return;

        var type = node.HandleDeclaration(Ctx);

        // StructTypes.Add(type);
    }

}