using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.Modules;

public class ScriptDeclarationVisitor : BaseAstVisitor
{
    public Script      Script { get; set; }
    public ExecContext Ctx    { get; set; }

    protected Stack<BaseNode> BlockNodeStack = new();

    protected UsingCallbackHandle PushBlockNode(BaseNode node) {
        BlockNodeStack.Push(node);
        return new UsingCallbackHandle(() => BlockNodeStack.Pop());
    }

    public ScriptDeclarationVisitor(ExecContext ctx, Script script) {
        Ctx    = ctx;
        Script = script;
    }

    public HashSet<ITopLevelDeclarationNode>    TopLevelDeclarations    => Script.Declarations.TopLevelDeclarations;
    public HashSet<RuntimeTypeInfo_Struct>      StructTypes             => Script.Declarations.StructTypes;
    public HashSet<RuntimeType>                 InterfaceTypes          => Script.Declarations.InterfaceTypes;
    public HashSet<RuntimeTypeInfo_Signal>      SignalTypes             => Script.Declarations.SignalTypes;
    public HashSet<Value>                       FunctionTypes           => Script.Declarations.FunctionTypes;
    public HashSet<DefDeclaration_FunctionNode> DefFunctionDeclarations => Script.Declarations.DefFunctionDeclarations;
    public HashSet<VariableSymbol>              VariableSymbols         => Script.Declarations.VariableDeclarations;


    public override void OnVisitAny(BaseNode node) {
        base.OnVisitAny(node);

        if (node is ITopLevelDeclarationNode decl) {
            TopLevelDeclarations.Add(decl);

            decl.DeclarationContext.Set(Script);
        }
    }


    public override void VisitBlockExpression(BlockExpression node) {
        using var _ = PushBlockNode(node);
        base.VisitBlockExpression(node);
    }

    public override void VisitDefDeclaration_FunctionNode(DefDeclaration_FunctionNode node) {
        base.VisitDefDeclaration_FunctionNode(node);

        if (BlockNodeStack.Count > 0)
            return;

        DefFunctionDeclarations.Add(node);
    }

    public override void VisitFunctionDeclaration(FunctionDeclaration node) {
        base.VisitFunctionDeclaration(node);
        if (BlockNodeStack.Count > 0)
            return;

        var (fnValue, symbol) = Ctx.DeclareFunction(node);

        FunctionTypes.Add(fnValue);
    }

    public override void VisitSignalDeclarationNode(SignalDeclarationNode node) {
        base.VisitSignalDeclarationNode(node);

        if (BlockNodeStack.Count > 0)
            return;

        var type = Ctx.DeclareSignal(node);

        SignalTypes.Add(type);
    }

    public override void VisitVariableDeclarationNode(VariableDeclarationNode node) {
        base.VisitVariableDeclarationNode(node);

        if (BlockNodeStack.Count > 0)
            return;

        foreach (var symbol in Ctx.DeclareVariable(node)) {
            VariableSymbols.Add(symbol);
        }
    }
    public override void VisitStructDeclaration(StructDeclaration node) {
        using var _ = PushBlockNode(node);

        base.VisitStructDeclaration(node);
        if (BlockNodeStack.Count > 1)
            return;

        var type = Ctx.DeclareType(node) as RuntimeTypeInfo_Struct;
        StructTypes.Add(type);
    }
    public override void VisitInterfaceDeclarationNode(InterfaceDeclarationNode node) {
        base.VisitInterfaceDeclarationNode(node);

    }
}