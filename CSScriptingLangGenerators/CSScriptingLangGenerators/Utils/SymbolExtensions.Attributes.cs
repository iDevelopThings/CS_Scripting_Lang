using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Utils;

public static partial class SymbolExtensions
{
    public static bool HasAttribute(this ISymbol symbol, string name, bool onlyInProjectNS = true) =>
        symbol.GetAttributes().HasAttribute(name, onlyInProjectNS);

    public static bool HasAttribute(this ImmutableArray<AttributeData> attributes, string name, bool onlyInProjectNS = true) =>
        attributes.TryGetAttribute(name, out _, onlyInProjectNS);

    public static AttributeData GetAttribute(this ISymbol symbol, string name, bool onlyInProjectNS = true) =>
        symbol.GetAttributes().GetAttribute(name, onlyInProjectNS);

    public static AttributeData GetAttribute(this ImmutableArray<AttributeData> attributes, string name, bool onlyInProjectNS = true) =>
        attributes.TryGetAttribute(name, out var value, onlyInProjectNS) ? value : null;

    public static bool TryGetAttribute(this ISymbol symbol, string name, out AttributeData attribute, bool onlyInProjectNS = true) =>
        symbol.GetAttributes().TryGetAttribute(name, out attribute, onlyInProjectNS);
    public static bool TryGetAttribute(this ISymbol symbol, IEnumerable<string> names, out AttributeData attribute, bool onlyInProjectNS = true) =>
        symbol.GetAttributes().TryGetAttribute(names, out attribute, onlyInProjectNS);

    public static bool TryGetAttribute(this ImmutableArray<AttributeData> attributes, string name, out AttributeData attribute, bool onlyInProjectNS = true) {
        attribute = attributes.SingleOrDefault(a => {
            if (onlyInProjectNS && !Constants.AllNamespaces.Contains(a.AttributeClass?.ContainingNamespace.ToDisplayString()))
                return false;
            if (a.AttributeClass?.Name == name)
                return true;
            if (a.AttributeClass?.Name == name[..^9])
                return true;

            return false;
        });
        return attribute != null;
    }

    public static IEnumerable<AttributeData> TryGetAttributes(
        this ImmutableArray<AttributeData> attributes,
        string                             name,
        bool                               onlyInProjectNS = true
    ) {
        foreach (var a in attributes) {
            if (onlyInProjectNS && !Constants.AllNamespaces.Contains(a.AttributeClass?.ContainingNamespace.ToDisplayString()))
                continue;
            if (a.AttributeClass?.Name == name)
                yield return a;
        }
    }

    public static bool TryGetAttribute(this ImmutableArray<AttributeData> attributes, IEnumerable<string> names, out AttributeData attribute, bool onlyInProjectNS = true) {
        attribute = attributes.SingleOrDefault(a => {
            if (onlyInProjectNS && !Constants.AllNamespaces.Contains(a.AttributeClass?.ContainingNamespace.ToDisplayString()))
                return false;
            if (names.Contains(a.AttributeClass?.Name)) {
                return true;
            }
            return false;
        });
        return attribute != null;
    }

    public static T GetAttributeArgument<T>(this ISymbol symbol, string name, int i = 0) =>
        symbol.GetAttributeArgument(name, default(T), i);
    
    public static T GetAttributeArgumentWithCompilation<T>(this ISymbol symbol, string name, Compilation compilation, int i = 0) =>
        symbol.GetAttributeArgument(name, default(T), i, compilation);

    public static T GetAttributeArgument<T>(this ISymbol symbol, string name, T defaultValue, int i = 0, Compilation compilation = null) {
        if (symbol.TryGetAttribute(name, out var attribute)) {
            var argVal = attribute.GetArgument<T>(i, compilation);
            return argVal != null ? argVal : defaultValue;
        }

        return defaultValue;
    }

    public static IEnumerable<object> GetArguments(this AttributeData attribute) {
        if (attribute.AttributeClass is IErrorTypeSymbol) {
            if (attribute.ApplicationSyntaxReference != null) {
                var syntax = attribute.ApplicationSyntaxReference.GetSyntax() as AttributeSyntax;
                if (syntax == null) {
                    yield break;
                }

                foreach (var arg in (syntax.ArgumentList?.Arguments ?? Enumerable.Empty<AttributeArgumentSyntax>())) {
                    if (arg.Expression is LiteralExpressionSyntax literal) {
                        yield return literal.Token.Value;
                    }
                }
            }
        }

        foreach (var arg in attribute.ConstructorArguments) {
            yield return arg.Value;
        }

    }
    public static object GetArgument(this AttributeData attribute, int i = 0) {
        // check if attribute has errors
        if (attribute.AttributeClass is IErrorTypeSymbol) {
            if (attribute.ApplicationSyntaxReference != null) {
                var syntax = attribute.ApplicationSyntaxReference.GetSyntax() as AttributeSyntax;
                if (syntax == null) {
                    return default;
                }

                if (syntax.ArgumentList?.Arguments.Count <= i) {
                    return default;
                }

                var expr = syntax.ArgumentList?.Arguments[i].Expression;
                if (expr is LiteralExpressionSyntax literal) {
                    return literal.Token.Value;
                }

                return default;
            }
        }

        var args = attribute.ConstructorArguments;
        if (i >= args.Length) {
            return default;
        }

        return args[i].Value;
    }
    public static T GetArgument<T>(this AttributeData attribute, int i = 0, Compilation compilation = null) {
        // check if attribute has errors
        if (attribute.AttributeClass is IErrorTypeSymbol) {
            if (attribute.ApplicationSyntaxReference != null) {
                var syntax = attribute.ApplicationSyntaxReference.GetSyntax() as AttributeSyntax;
                if (syntax == null) {
                    return default;
                }

                if (syntax.ArgumentList?.Arguments.Count <= i) {
                    return default;
                }

                var expr = syntax.ArgumentList?.Arguments[i].Expression;
                if (expr is LiteralExpressionSyntax literal) {
                    var value = literal.Token.Value;
                    if (value is T t) {
                        return t;
                    }
                    return default;
                }
                if (expr is TypeOfExpressionSyntax typeOf) {
                    var type = typeOf.Type;
                    if (type is IdentifierNameSyntax id) {
                        var name = id.Identifier.Text;
                        if (typeof(T) == typeof(string)) {
                            return (T) (object) name;
                        }
                        if (typeof(T) == typeof(TypeSyntax)) {
                            return (T) (object) Type.GetType(name);
                        }

                        if (typeof(T) == typeof(INamedTypeSymbol)) {
                            if (compilation == null) {
                                throw new ArgumentNullException(nameof(compilation));
                            }
                            var semanticModel = compilation.GetSemanticModel(attribute.ApplicationSyntaxReference.SyntaxTree);
                            var symbol        = semanticModel.GetTypeInfo(type).Type as INamedTypeSymbol;

                            return (T) symbol;
                        }

                        throw new NotSupportedException($"Cannot convert {typeof(T)} from {name}");
                    }
                }

                return default;
            }
        }

        var args = attribute.ConstructorArguments;
        if (i >= args.Length) {
            return default;
        }

        return (T) args[i].Value;
    }

}