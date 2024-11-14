using System.Globalization;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Prototypes.Types;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;
using SharpX;

namespace CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

[SyntaxNode]
public class LiteralToken(int index, SyntaxTree tree) : SyntaxToken(index, tree)
{
    public string Tag => Kind switch {
        TokenType.Int32  => "i32",
        TokenType.Int64  => "i64",
        TokenType.Float  => "f32",
        TokenType.Double => "f64",
        TokenType.String => "string",
        _                => "literal",
    };
    public override string ToString()     => $"{Tag}({Text})";
    public override string DebugContent() => $"{Tag}({Text})";
}

[SyntaxNode]
public class NumberToken(int index, SyntaxTree tree) : LiteralToken(index, tree)
{
    public bool IsInt32  => Kind == TokenType.Int32;
    public bool IsInt64  => Kind == TokenType.Int64;
    public bool IsFloat  => Kind == TokenType.Float;
    public bool IsDouble => Kind == TokenType.Double;
}

[SyntaxNode]
public partial class LiteralValueExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public LiteralToken Literal => ChildToken<LiteralToken>();

    public override string DebugContent() => Literal.DebugContent();

    public virtual Maybe<ValueReference> Execute(ExecContext ctx) {
        throw new NotImplementedException();
    }
}

[SyntaxNode]
public class BooleanToken(int index, SyntaxTree tree) : LiteralToken(index, tree)
{
    public bool RawValue => bool.Parse(Text);
}

[SyntaxNode]
public partial class BooleanExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new BooleanToken Literal  => ChildToken<BooleanToken>();
    public     bool         RawValue => Literal?.RawValue ?? false;

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<ArrayPrototype>.Get().Ty;
    }
}

[SyntaxNode]
public class NullValueToken(int index, SyntaxTree tree) : LiteralToken(index, tree)
{
    public string RawValue => Text.ToString();
}

[SyntaxNode]
public partial class NullValueExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new NullValueToken Literal  => ChildToken<NullValueToken>();
    public     string         RawValue => Literal?.RawValue ?? "";

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(Value.Null()).ToMaybe();



    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<NullPrototype>.Get().Ty;
    }
}

[SyntaxNode]
public class StringToken(int index, SyntaxTree tree) : LiteralToken(index, tree)
{
    public string RawValue => RepresentText.StripQuotes();
}

[SyntaxNode]
public partial class StringExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new StringToken Literal  => ChildToken<StringToken>();
    public     string      RawValue => Literal?.RawValue ?? "";

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<StringPrototype>.Get().Ty;
    }
}

[SyntaxNode]
public class Int32Token(int index, SyntaxTree tree) : NumberToken(index, tree)
{
    public int RawValue => int.Parse(Text);
}

[SyntaxNode]
public partial class Int32Expr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new Int32Token Literal  => ChildToken<Int32Token>();
    public     int        RawValue => Literal?.RawValue ?? 0;

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();



    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<Int32Prototype>.Get().Ty;
    }
}

[SyntaxNode]
public class Int64Token(int index, SyntaxTree tree) : NumberToken(index, tree)
{
    public long RawValue => long.Parse(Text);
}

[SyntaxNode]
public partial class Int64Expr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new Int64Token Literal  => ChildToken<Int64Token>();
    public     long       RawValue => Literal?.RawValue ?? 0;

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();



    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<Int64Prototype>.Get().Ty;
    }
}

[SyntaxNode]
public class FloatToken(int index, SyntaxTree tree) : NumberToken(index, tree)
{
    public float RawValue => float.Parse(RepresentText.Replace("f", "").Replace("F", ""), CultureInfo.InvariantCulture);
}

[SyntaxNode]
public partial class FloatExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new FloatToken Literal  => ChildToken<FloatToken>();
    public     float      RawValue => Literal?.RawValue ?? 0;

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();



    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<FloatPrototype>.Get().Ty;
    }
}

[SyntaxNode]
public class DoubleToken(int index, SyntaxTree tree) : NumberToken(index, tree)
{
    public double RawValue => double.Parse(Text, CultureInfo.InvariantCulture);
}

[SyntaxNode]
public partial class DoubleExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public new DoubleToken Literal  => ChildToken<DoubleToken>();
    public     double      RawValue => Literal?.RawValue ?? 0;

    public override string DebugContent() => Literal.DebugContent();

    public override Maybe<ValueReference> Execute(ExecContext ctx)
        => ctx.ValReference(RawValue).ToMaybe();

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        yield return TypeAlias<DoublePrototype>.Get().Ty;
    }
}

[SyntaxNode]
public partial class ObjectLiteralExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public IEnumerable<ObjectPropertyExpr> Properties => ChildNodes<ObjectPropertyExpr>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("{")
       .Add(Properties.Select(p => p.DebugContent()).Join())
       .Add("}")
       .ClearTrailingSpace();


    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var obj = Value.Object(ctx);
        
        foreach (var (property, value) in Properties.DoExecute(ctx)) {
            obj[property.Key.RepresentText] = value.Value();
        }
        // var properties = Properties.DoExecute(ctx)
        //    .Select(
        //         r => ((r.element.Key as IdentifierExpr)?.AsString(), r.value.Value())
        //     )
        //    .ToList();


        return ctx.ValReference(obj);
    }

    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        var ty       = Ty.Object();
        
        foreach (var prop in Properties) {
            var val = prop.ResolveAndCacheTypes(ctx, symbol).First();
            ty.SetMember(prop.Key.RepresentText, val);
        }
        
        yield return ty;
    }
}

[SyntaxNode]
public partial class ObjectPropertyExpr(int index, SyntaxTree tree) : ExprSyntax(index, tree), IExecSingle
{
    public SyntaxToken Key   => ChildToken(t => t.HasAny(TokenType.Identifier | TokenType.String | TokenType.Int32 | TokenType.Int64));
    public ExprSyntax  Value => ChildAfter<ExprSyntax>(Key);

    public override string DebugContent() => DataContentBuilder.Create()
       .Add(Key?.DebugContent())
       .Add(":")
       .Add(Value?.DebugContent())
       .ClearTrailingSpace();

    public Maybe<ValueReference> Execute(ExecContext ctx) {
        return Value.DoExecuteSingle(ctx);
    }
    
    public override IEnumerable<Ty> ResolveTypes(ExecContext ctx, DefinitionSymbol symbol) {
        foreach (var resolveType in Value.ResolveAndCacheTypes(ctx, symbol)) {
            yield return resolveType;
        }
    }
}

[SyntaxNode]
public partial class ArrayLiteralExpr(int index, SyntaxTree tree) : LiteralValueExpr(index, tree)
{
    public IEnumerable<ExprSyntax> Elements => ChildNodes<ExprSyntax>();

    public override string DebugContent() => DataContentBuilder.Create()
       .Add("[")
       .Add(Elements.Select(e => e.DebugContent()).Join())
       .Add("]")
       .ClearTrailingSpace();

    public override Maybe<ValueReference> Execute(ExecContext ctx) {
        var elements = Elements.Select(e => e.DoExecuteSingle(ctx).Value().Value);

        var arr = Value.Array(elements);

        return ctx.ValReference(arr);
    }
}