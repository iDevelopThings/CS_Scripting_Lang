using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class SignalDecl(int index, SyntaxTree tree) : SyntaxNode(index, tree), IExecSingle, IDeclaration
{
    public Prototype Prototype { get; set; }

    public IdentifierExpr          Name      => ChildNode<IdentifierExpr>();
    public ArgumentDeclarationList Arguments => ChildNode<ArgumentDeclarationList>();

    public override string DebugContent() {
        return DataContentBuilder.Create()
           .Add("signal")
           .Add(Name?.DebugContent())
           .Add(Arguments?.DebugContent())
           .ClearTrailingSpace();
    }

    public Prototype HandleDeclaration(ExecContext ctx)
        => Prototype ??= TypesTable.DeclareSignal(ctx, this);

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        HandleDeclaration(ctx);

        var type = Prototype.ValueType;

        var ctor = type.GetConstructorFn();
        var val  = ctx.Call(ctor, type);

        var variable = ctx.Variables.Declare(Prototype.Symbol.Name, val);

        return ctx.VariableAccessReference(variable).ToMaybe();
    }

}