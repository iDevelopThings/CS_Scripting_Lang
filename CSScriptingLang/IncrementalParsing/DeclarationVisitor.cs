using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.IncrementalParsing;

public class DeclarationVisitor : BaseSyntaxNodeVisitor
{
    public Script      Script { get; set; }
    public ExecContext Ctx    { get; set; }

    protected Stack<SyntaxNode> BlockNodeStack = new();

    public DefinitionScope Scope { get; private set; }

    protected UsingCallbackHandle PushBlockNode(SyntaxNode node) {
        BlockNodeStack.Push(node);
        return new UsingCallbackHandle(() => BlockNodeStack.Pop());
    }
    protected UsingCallbackHandle PushScope(SyntaxNode node) {
        BlockNodeStack.Push(node);
        Scope = Scope.Enter(node);

        return new UsingCallbackHandle(() => {
            Scope = Scope.Exit();
            BlockNodeStack.Pop();
        });
    }

    public DeclarationVisitor(ExecContext ctx, Script script) {
        Ctx    = ctx;
        Script = script;

        Scope = DefinitionScope.InitGlobalScope(Ctx);

        Script.SyntaxTree.SyntaxRoot.Scope = Scope;
    }

    public HashSet<ITopLevelDeclarationNode> TopLevelDeclarations => Script.Declarations.TopLevelDeclarations;
    public HashSet<StructPrototype>          StructTypes          => Script.Declarations.StructTypes;
    public HashSet<EnumPrototype>            EnumTypes            => Script.Declarations.EnumTypes;
    public HashSet<SignalPrototype>          SignalTypes          => Script.Declarations.SignalTypes;
    public HashSet<Value>                    FunctionTypes        => Script.Declarations.FunctionTypes;
    public HashSet<FunctionDecl>             DefFunctionDecls     => Script.Declarations.DefFunctionDecls;
    public HashSet<VariableSymbol>           VariableSymbols      => Script.Declarations.VariableDeclarations;

    public HashSet<VariableSymbol> Exports        => Script.Declarations.Exports;
    public HashSet<VariableSymbol> PrivateExports => Script.Declarations.PrivateExports;

    public List<(VariableSymbol varSymbol, SyntaxNode node, DefinitionScope scope)> LazyDeclarations { get; } = new();

    public void FinalizeLazyDeclarations() {
        foreach (var (varSymbol, node, scope) in LazyDeclarations) {
            scope.Declare(varSymbol, node);
        }
    }

    public override void OnVisitAny(SyntaxNode node) {
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

    public override void Visit_Block(Block node) {
        if (BlockNodeStack.Count == 0 && Script.IsWrappedModule || node.Parent is SourceSyntax) {
            base.Visit_Block(node);
            return;
        }

        using var __ = PushScope(node);

        base.Visit_Block(node);
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

    public override void Visit_FunctionDecl(FunctionDecl node) {
        if (node.IsDef) {
            base.Visit_FunctionDecl(node);

            if (BlockNodeStack.Count > 0)
                return;

            DefFunctionDecls.Add(node);
            return;
        }

        using var _ = PushScope(node);

        var (fnValue, symbol) = Ctx.DeclareFunction(node);

        foreach (var param in node.Arguments) {
            var varSymbol = Ctx.Variables.Declare(param.Name, () => new VariableSymbol(param.Name) {
                IsBaseDeclaration = true,
            });

            LazyDeclarations.Add((varSymbol, param, Scope));
            // Scope.LazyDeclare(varSymbol, param);
            // Scope.Declare(varSymbol, param);
        }

        // if (BlockNodeStack.Count == 0) {
        TryExport(symbol);
        FunctionTypes.Add(fnValue);
        // }

        base.Visit_FunctionDecl(node);
    }

    public override void Visit_SignalDecl(SignalDecl node) {
        base.Visit_SignalDecl(node);

        if (BlockNodeStack.Count > 0)
            return;

        var type = node.HandleDeclaration(Ctx) as SignalPrototype;

        SignalTypes.Add(type);
    }

    public override void Visit_VariableDecl(VariableDecl node) {
        base.Visit_VariableDecl(node);

        foreach (var symbol in Ctx.DeclareVariable(node)) {
            VariableSymbols.Add(symbol);

            LazyDeclarations.Add((symbol, node, Scope));
            // Scope.LazyDeclare(symbol, node);
            // Scope.Declare(symbol, node);

            if (BlockNodeStack.Count == 0)
                TryExport(symbol);
        }
    }
    public override void Visit_StructDecl(StructDecl node) {
        using var _ = PushScope(node);

        var type = node.HandleDeclaration(Ctx) as StructPrototype;
        StructTypes.Add(type);

        Scope.Declare(node, type);

        base.Visit_StructDecl(node);
    }
    public override void Visit_TypeDeclMember(TypeDeclMember node) {
        base.Visit_TypeDeclMember(node);
        Scope.Declare(new VariableSymbol(node.Name, null), node);
    }
    public override void Visit_EnumDecl(EnumDecl node) {
        using var _ = PushBlockNode(node);

        base.Visit_EnumDecl(node);

        if (BlockNodeStack.Count > 1)
            return;

        var type = node.HandleDeclaration(Ctx) as EnumPrototype;

        EnumTypes.Add(type);
    }

}