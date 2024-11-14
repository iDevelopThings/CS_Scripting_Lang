using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.Interpreter.Execution.Expressions;

[ASTNode]
public abstract partial class AccessorExpression : Expression
{
    [VisitableNodeProperty]
    public Expression Object { get; set; }

    protected AccessorExpression(Expression obj) {
        Object = obj;
    }

    public override ValueReference Execute(ExecContext ctx) {
        return base.Execute(ctx);
    }
}

[ASTNode]
public partial class IndexAccessExpression : AccessorExpression, IExecutableNode
{
    [VisitableNodeProperty]
    public Expression Index { get; }

    public override bool IsConstant => Index is {IsConstant: true};

    public IndexAccessExpression(Expression obj, Expression index) : base(obj) {
        Index = index;
    }

    public string GetPath() {
        if (Object is MemberAccessExpression prop) {
            return $"{prop.GetPath()}[{Index}]";
        }

        if (Object is IndexAccessExpression index) {
            return $"{index.GetPath()}[{Index}]";
        }

        return $"[{Index}]";
    }

    public override ValueReference Execute(ExecContext ctx) {
        var objRef   = Object.Execute(ctx);
        var indexRef = Index.Execute(ctx);

        // var indexResult = Execute(node.Index, ctx);
        // var (indexSymbol, index) = indexResult.Get<VariableSymbol, Value>();
        // if (indexSymbol == null && index == null) {
        //     LogError(node.Index, $"Failed to get value from index node");
        //     return result;
        // }

        return ctx.IndexAccessReference(objRef.Value, indexRef.Value);
    }

}

[ASTNode]
public partial class MemberAccessExpression : AccessorExpression, IExecutableNode
{
    [VisitableNodeProperty]
    public IdentifierExpression Identifier { get; set; }

    public MemberAccessExpression(Expression obj, IdentifierExpression name) : base(obj) {
        Identifier = name;
    }

    public override ValueReference Execute(ExecContext ctx) {
        var objRef = Object.Execute(ctx);

        // Temporarily switches our context's module to the module of the declaration
        // using var _ = ctx.SwitchModule(objRef.Value.GetModule());

        // var (objSymbol, obj) = objResult.Get<VariableSymbol, Value>();
        if (objRef.Value != null) {
            return ctx.MemberAccessReference(objRef.Value, Identifier);
        }

        if (ctx.GetVariable(Identifier, out var s)) {
            return ctx.VariableAccessReference(s);
        }

        return base.Execute(ctx);
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (Object == null) {
            throw new FailedToGetRuntimeTypeException(this, $"Object is null; identifier = {Identifier.Name}");
        }

        Identifier.ResolvedTypes = [];
        
        var objType = Object.ResolveAndCacheTypes(ctx, symbol).FirstOrDefault();
        if (objType != null) {
            var objMemberType = objType.GetMember(Identifier.Name);
            if (objMemberType != null) {
                Identifier.ResolvedTypes.Add(objMemberType);
                yield return objMemberType;
                
                yield break;
            }
            
            DiagnosticManager.Diagnostic_Warning().Message($"Failed to resolve member type {Identifier.Name} on object of type {objType.Name}").Range(this).Report();
            yield break;
        }

        DiagnosticManager.Diagnostic_Warning().Message($"Failed to resolve type for {Identifier.Name}").Range(this).Report();
    }

    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        var s = scope ?? Scope;

        var lhs = Object.FindReferences(s);
        var member = lhs.SelectMany(x => x.Type.Members)
           .Where(x => x.Key == Identifier.Name)
           .SelectMany(x => x.Value.NamedSymbols);

        return member;
    }

    public string GetPath() {
        if (Object is MemberAccessExpression prop) {
            return $"{prop.GetPath()}.{Identifier.Name}";
        }

        if (Object is IndexAccessExpression index) {
            return $"{index.GetPath()}[{Identifier.Name}]";
        }

        return Identifier.Name;
    }

}