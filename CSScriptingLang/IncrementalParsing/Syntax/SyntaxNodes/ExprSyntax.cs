using System.Collections;
using System.Diagnostics;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public partial class ExprSyntax(int index, SyntaxTree tree) : SyntaxNode(index, tree)
{
    // public IEnumerable<CommentSyntax> Comments => Tree.BinderData?.GetComments(this) ?? [];
}

[SyntaxNode]
public partial class IdentifierExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public NameToken Name => ChildToken<NameToken>();

    public override string DebugContent() => Name?.RepresentText ?? "null";

    public static implicit operator string(IdentifierExpr expr)
        => expr.Name?.RepresentText;

    public string AsString() => this;
    public Value  AsValue()  => AsString();


    private Value cachedStringVal;
    public static implicit operator Value(IdentifierExpr ident) {
        return ident.cachedStringVal ?? (ident.cachedStringVal = Value.String(ident.Name));
    }
    public Maybe<ValueReference> Execute(ExecContext ctx) {
        if (ctx.TryGetVariable(Name, out var variable)) {
            return ctx.VariableAccessReference(variable);
        }

        if (ctx.ModuleResolver.TryGet(Name, out var module)) {
            Diagnostic_Error_Fatal($"Not implemented: Module access '{Name}'");
            return ValueReference.Nothing;
        }

        Diagnostic_Error_Fatal($"Variable '{Name}' not found");
        return ValueReference.Nothing;
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (Scope.TryGetSymbol(Name, out var def)) {
            foreach (var t in def.Types) {
                yield return t;
            }

            yield break;
        }

        Diagnostic_Warning($"Could not resolve type for {Name}");
    }


    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        var s = scope ?? Scope;

        if (s.TryGetSymbol(Name, out var def)) {
            return def.Named;
        }
        return ResolvedTypes.SelectMany(type => type.NamedSymbols);
    }
}

[SyntaxNode]
public partial class TypedIdentifierExpr(int index, SyntaxTree tree) : IdentifierExpr(index, tree)
{
    public TypeParameterList TypeParameterList => ChildNode<TypeParameterList>();

    public string TypeName {
        get {
            var n = Name.RepresentText;
            if (TypeParameterList != null) {
                n += "<";
                n += TypeParameterList.Select(t => t.Name.RepresentText).Join(", ");
                n += ">";
            }
            return n;
        }
    }

    public override string DebugContent() {
        var s = $"{Name?.RepresentText ?? "null"}";
        if (TypeParameterList != null) {
            s += TypeParameterList.DebugContent();
        }
        return s;
    }
    public ITypeAlias ResolveType() {
        return TypeAlias.Get(Name);
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (TypesTable.GetPrototypeTypeByName(Name, out var type)) {
            yield return type.PrototypeInstance.Ty;
            yield break;
        }

        Diagnostic_Warning($"Could not resolve type for {Name}");
    }
}

[SyntaxNode]
public partial class TupleExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree)
{
    public IEnumerable<ExprSyntax> Elements => ChildNodes<ExprSyntax>();

    public override string DebugContent() => $"Tuple({Elements.Select(e => e.DebugContent()).Join()})";
}

[SyntaxNode]
public partial class TypeParameter(int index, SyntaxTree tree) : IdentifierExpr(index, tree);

public class TypeParameterList(int index, SyntaxTree tree) : ExprSyntax(index, tree), IEnumerable<TypeParameter>
{
    public IEnumerable<TypeParameter> Types => ChildNodes<TypeParameter>();

    public override string DebugContent() => $"<{Types.Select(e => e.DebugContent()).Join()}>";


    public IEnumerator<TypeParameter> GetEnumerator() => Types.GetEnumerator();
    IEnumerator IEnumerable.          GetEnumerator() => GetEnumerator();
}

[SyntaxNode]
public partial class MemberAccessExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax     Value  => ChildNode<ExprSyntax>();
    public IdentifierExpr Member => ChildNode<IdentifierExpr>(Value);

    public override string DebugContent() {
        var str = "";
        if (Value != null) {
            str += Value.DebugContent();
            str += ".";
        }

        if (Member != null) {
            str += Member.DebugContent();
        }

        return str;
    }

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var objRef = Value.DoExecuteSingle(ctx).Value();

        if (objRef.Value != null) {
            var r  = ctx.MemberAccessReference(objRef.Value, Member);
            var rr = r.Value;
            return r;
        }

        if (ctx.GetVariable(Member, out var s)) {
            return ctx.VariableAccessReference(s);
        }

        Diagnostic_Error_Fatal($"Member access failed: {Member}");
        return ValueReference.Nothing;
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        if (Value == null) {
            Diagnostic_Error_Fatal($"Object is null; identifier = {Member.Name}");
            yield break;
        }

        Member.ResolvedTypes = [];

        var objType = Value.ResolveAndCacheTypes(ctx, symbol).FirstOrDefault();
        if (objType != null) {
            var objMemberType = objType.GetMember(Member.Name);
            if (objMemberType != null) {
                Member.ResolvedTypes.Add(objMemberType);
                yield return objMemberType;

                yield break;
            }

            Diagnostic_Warning($"Failed to resolve member type {Member.Name} on object of type {objType.Name}");
            yield break;
        }

        Diagnostic_Warning($"Failed to resolve type for {Member.Name}");
    }

    public override IEnumerable<NamedSymbolInformation> FindReferences(DefinitionScope scope = null) {
        var s = scope ?? Scope;
        Debugger.Launch();

        var lhs = Value.FindReferences(s);
        var member = lhs.SelectMany(x => x.Type.Members)
           .Where(x => x.Key == Member.Name)
           .SelectMany(x => x.Value.NamedSymbols);

        return member;
    }

}

[SyntaxNode]
public partial class IndexAccessExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax Value => ChildNode<ExprSyntax>();
    public ExprSyntax Index => ChildAfter<ExprSyntax>(Value);

    public override string DebugContent() => $"{Value?.DebugContent()}[{Index?.DebugContent()}]";

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var objRef   = Value.DoExecuteSingle(ctx).Value();
        var indexRef = Index.DoExecuteSingle(ctx).Value();

        return ctx.IndexAccessReference(objRef.Value, indexRef.Value);

    }
}

[SyntaxNode]
public partial class CallExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax        Name         => ChildNode<ExprSyntax>();
    public ArgumentListExpr  ArgumentList => ChildNode<ArgumentListExpr>();
    public TypeParameterList TypeParams   => ChildNode<TypeParameterList>() ?? new TypeParameterList(-1, Tree);

    public string FunctionName => Name switch {
        IdentifierExpr ident    => ident.Name,
        MemberAccessExpr member => member.Member.Name,
        _                       => throw new Exception("Invalid function name")
    };

    public override string DebugContent() => $"{(Name?.DebugContent() ?? "null")}({(ArgumentList?.DebugContent() ?? "null")})";

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        using var _ = ctx.SetCaller(this);

        var fnInstRef = Name.DoExecuteSingle(ctx).Value();
        var fn        = fnInstRef.Value;
        var inst      = fnInstRef.Object;

        // var (fn, inst) = TryGetFunctionValue(this, ctx);
        if (fn == null) {
            ctx.LogError(this, $"Function '{FunctionName}' not found");
            return ctx.ValReference(Value.Null());
        }

        if (!fn.Is.Function) {
            if (inst?.DataObject is Script script) {
                if (script.Declarations.IsExport(FunctionName)) {
                    ctx.LogError(this, $"Value '{FunctionName}' is not callable. It doesn't appear to be exported?");
                    return ctx.ValReference(Value.Null());
                }
            }

            ctx.LogError(this, $"Value '{FunctionName}' is not callable. It is of type '{fn.Type}'");
            return ctx.ValReference(Value.Null());
        }

        var args = ArgumentList.DoExecuteMulti(ctx).Select(v => v.Value().Value).ToArray();

        var returnValue = ctx.Call(
            fn,
            inst,
            args
        );

        return ctx.ValReference(returnValue);
    }
}

[SyntaxNode]
public partial class ArgumentListExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecMulti, IEnumerable<ExprSyntax>
{
    public IEnumerable<ExprSyntax> Arguments => ChildNodes<ExprSyntax>();

    public override string DebugContent() => $"({Arguments.Select(e => e.DebugContent()).Join()})";

    public IEnumerable<Maybe<ValueReference>> ExecuteMulti(ExecContext ctx) {
        return Arguments.Select(e => e.DoExecuteSingle(ctx));
    }

    public IEnumerator<ExprSyntax> GetEnumerator() => Arguments.GetEnumerator();
    IEnumerator IEnumerable.       GetEnumerator() => GetEnumerator();
}

[SyntaxNode]
public partial class BinaryOpExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax    Left  => ChildNode<ExprSyntax>();
    public OperatorToken Op    => ChildToken<OperatorToken>();
    public ExprSyntax    Right => ChildAfter<ExprSyntax>(Op);

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Left?.DebugContent())
       .Add(Op?.DebugContent())
       .Add(Right?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var leftRef  = ctx.ExecuteLValue(Left.DoExecuteSingle);
        var rightRef = ctx.ExecuteRValue(Right.DoExecuteSingle);

        if (leftRef.Value == null || rightRef.Value == null) {
            Diagnostic_Error_Fatal("Null reference in binary operation");
            return ValueReference.Nothing;
        }

        if (leftRef.Value!.Is.Null && rightRef.Value!.Is.Null) {
            Diagnostic_Error_Fatal("Both values are null");
            return ValueReference.Nothing;
        }

        switch (Op.Operator) {
            case OperatorType.MinusEquals:
            case OperatorType.PlusEquals: {
                var rightValueRef = Value.Reference(rightRef);
                var resultValue   = leftRef.Value.Operator(Op.Operator, rightValueRef);

                leftRef.SetValue(resultValue, true);

                return leftRef;
            }

            case OperatorType.Assignment: {
                leftRef.SetValue(rightRef.Value, true);

                return leftRef;
            }
            default: {
                var resultValue = leftRef.Value.Operator(Op.Operator, rightRef.Value);

                return ctx.ValReference(resultValue);
            }
        }
    }
}

[SyntaxNode]
public partial class UnaryOpExpr(
    int        index,
    SyntaxTree tree,
    bool       isPostfix
) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax    Operand   => ChildNode<ExprSyntax>();
    public OperatorToken Op        => ChildToken<OperatorToken>();
    public bool          IsPostfix { get; set; } = isPostfix;

    public override string DebugContent() => DataContentBuilder.Create()
       .Choice(
            IsPostfix,
            b => b.Add(Operand?.DebugContent()).Add(Op?.DebugContent()),
            b => b.Add(Op?.DebugContent()).Add(Operand?.DebugContent())
        )
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var resultRef = Operand.DoExecuteSingle(ctx);
        var value     = resultRef.Value().Value;

        switch (Op.Operator) {
            // var a = !true;
            case OperatorType.Not: {
                var opRes = value.Operator(Op.Operator, null);
                return ctx.ValReference(opRes);
            }
            // var a = a++; or var a = --a;
            case OperatorType.Increment:
            case OperatorType.Decrement: {
                var opRes = value.Operator(Op.Operator, Value.Number(1));
                return ctx.ValReference(opRes);
            }
            // var a = -1;
            case OperatorType.Minus: {
                var opRes = value.Operator(OperatorType.Multiply, Value.Int32(-1));
                return ctx.ValReference(opRes);
            }
            // var a = +1;
            case OperatorType.Plus: {
                var opRes = value.Operator(OperatorType.Multiply, Value.Int32(1));
                return ctx.ValReference(opRes);
            }
            default: {
                Diagnostic_Error_Fatal($"Unhandled operator type: {Op.Operator}");
                return ValueReference.Nothing;
            }
        }
    }
}

[SyntaxNode]
public partial class RangeExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public ExprSyntax Expr => ChildNode<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Expr?.DebugContent())
       .ClearTrailingSpace();


    public Maybe<ValueReference> Execute(ExecContext ctx) {
        var value = Expr.DoExecuteSingle(ctx).Value();

        if (!value.Value.IsEnumerable) {
            Diagnostic_Error_Fatal("Range expression must be used on an enumerable value");
            return ValueReference.Nothing;
        }

        return value;

        /*var obj = Value.Object(ctx);
        obj["min"] = Value.Number(0);
        obj["max"] = Expr.DoExecuteSingle(ctx).Value().Value;

        return ctx.ValReference(obj);*/
    }
}