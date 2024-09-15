using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Utils;

static class CompilationExtensions
{
    public static INamedTypeSymbol GetTypeByName(this Compilation compilation, string typeMetadataName) {
        return compilation.References
           .Select(compilation.GetAssemblyOrModuleSymbol)
           .OfType<IAssemblySymbol>()
           .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
           .FirstOrDefault(t => t != null);
    }

    public static IEnumerable<INamedTypeSymbol> GetTypesByName(this Compilation compilation, string typeMetadataName) {
        return compilation.References
           .Select(compilation.GetAssemblyOrModuleSymbol)
           .OfType<IAssemblySymbol>()
           .Select(assemblySymbol => assemblySymbol.GetTypeByMetadataName(typeMetadataName))
           .Where(t => t != null);
    }


    public static INamedTypeSymbol GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName) {
        INamedTypeSymbol type = null;

        foreach (var currentType in compilation.GetTypesByMetadataName(fullyQualifiedMetadataName)) {
            if (ReferenceEquals(currentType.ContainingAssembly, compilation.Assembly)) {
                Debug.Assert(type is null);
                return currentType;
            }

            switch (currentType.GetResultantVisibility()) {
                case SymbolVisibility.Public:
                case SymbolVisibility.Internal when currentType.ContainingAssembly.GivesAccessTo(compilation.Assembly):
                    break;

                default:
                    continue;
            }

            if (type != null) {
                // Multiple visible types with the same metadata name are present
                return null;
            }

            type = currentType;
        }

        return type;
    }

    // https://github.com/dotnet/roslyn/blob/d2ff1d83e8fde6165531ad83f0e5b1ae95908289/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs#L28-L73
    private static SymbolVisibility GetResultantVisibility(this ISymbol symbol) {
        // Start by assuming it's visible.
        var visibility = SymbolVisibility.Public;
        switch (symbol.Kind) {
            case SymbolKind.Alias:
                // Aliases are uber private.  They're only visible in the same file that they
                // were declared in.
                return SymbolVisibility.Private;
            case SymbolKind.Parameter:
                // Parameters are only as visible as their containing symbol
                return GetResultantVisibility(symbol.ContainingSymbol);
            case SymbolKind.TypeParameter:
                // Type Parameters are private.
                return SymbolVisibility.Private;
        }

        while (symbol is not null && symbol.Kind != SymbolKind.Namespace) {
            switch (symbol.DeclaredAccessibility) {
                // If we see anything private, then the symbol is private.
                case Accessibility.NotApplicable:
                case Accessibility.Private:
                    return SymbolVisibility.Private;
                // If we see anything internal, then knock it down from public to
                // internal.
                case Accessibility.Internal:
                case Accessibility.ProtectedAndInternal:
                    visibility = SymbolVisibility.Internal;
                    break;
                // For anything else (Public, Protected, ProtectedOrInternal), the
                // symbol stays at the level we've gotten so far.
            }

            symbol = symbol.ContainingSymbol;
        }

        return visibility;
    }

    private enum SymbolVisibility
    {
        Public,
        Internal,
        Private,
    }
}

public static class SymbolExtensions
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
            return a.AttributeClass?.Name == name;
        });
        return attribute != null;
    }
    public static bool TryGetAttribute(this ImmutableArray<AttributeData> attributes, IEnumerable<string> names, out AttributeData attribute, bool onlyInProjectNS = true) {
        attribute = attributes.SingleOrDefault(a => {
            if (onlyInProjectNS && !Constants.AllNamespaces.Contains(a.AttributeClass?.ContainingNamespace.ToDisplayString()))
                return false;
            return names.Contains(a.AttributeClass?.Name);
        });
        return attribute != null;
    }

    public static T GetAttributeArgument<T>(this ISymbol symbol, string name, int i = 0) =>
        symbol.GetAttributeArgument(name, default(T), i);

    public static T GetAttributeArgument<T>(this ISymbol symbol, string name, T defaultValue, int i = 0) {
        if (symbol.TryGetAttribute(name, out var attribute)) {
            var argVal = attribute.GetArgument<T>(i);
            return argVal != null ? argVal : defaultValue;
        }

        return defaultValue;
    }

    public static T GetArgument<T>(this AttributeData attribute, int i = 0) {
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

                return default;
            }
        }

        var args = attribute.ConstructorArguments;
        if (i >= args.Length) {
            return default;
        }

        return (T) args[i].Value;
    }

    public static bool HasDefaultConstructor(this INamedTypeSymbol klass) {
        return !klass.IsStatic && (klass.InstanceConstructors.Length == 0 || klass.InstanceConstructors.Any(c => c.Parameters.Length == 0));
    }

    public static List<INamedTypeSymbol> GetParentTypes(this ITypeSymbol type) {
        var result = new List<INamedTypeSymbol>();

        var parent = type.ContainingType;
        while (parent != null) {
            result.Add(parent);
            parent = parent.ContainingType;
        }

        return result;
    }

    public static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(this INamespaceSymbol type) {

        foreach (var member in type.GetMembers()) {
            if (member is INamedTypeSymbol namedType) {
                yield return namedType;
            }
        }

    }

    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol @this, INamedTypeSymbol stopAt = null) {
        var baseType = @this.BaseType;
        while (baseType != null) {
            yield return baseType;
            baseType = baseType.BaseType;
            if (SymbolEqualityComparer.Default.Equals(baseType, stopAt)) {
                yield break;
            }
        }
    }
    public static bool HasBaseType(this ITypeSymbol @this, INamedTypeSymbol baseType) {
        return @this.GetBaseTypes().Any(t => SymbolEqualityComparer.Default.Equals(t, baseType));
    }

    public static List<INamedTypeSymbol> GetDerivedTypes(this ITypeSymbol type) {
        var result = new List<INamedTypeSymbol>();

        var derived = type.ContainingNamespace.GetNamespaceTypes()
           .Where(t => {
                if (t.BaseType == null)
                    return false;

                if (!t.HasBaseType((INamedTypeSymbol) type))
                    return false;

                return true;
            });

        result.AddRange(derived);

        return result;
    }

    public static string GetFullyQualifiedName(this ITypeSymbol symbol) {
        string result = null;

        var parent = symbol.ContainingType;
        while (parent != null) {
            result = result != null
                ? parent.Name + "." + result
                : parent.Name;

            parent = parent.ContainingType;
        }

        var ns = GetFullNamespace(symbol);
        var withNs = result != null
            ? ns + "." + result
            : ns;

        return string.IsNullOrEmpty(withNs)
            ? symbol.Name
            : withNs + "." + symbol.Name;
    }

    public static string GetFullNamespace(this ITypeSymbol symbol) {
        string result = null;

        var ns = symbol.ContainingNamespace;
        while (ns is {IsGlobalNamespace: false}) {
            result = result != null
                ? ns.Name + "." + result
                : ns.Name;

            ns = ns.ContainingNamespace;
        }

        return result;
    }

    public static string ToCamelCase(this string identifier) {
        if (string.IsNullOrEmpty(identifier)) {
            return identifier;
        }

        if (!char.IsLetter(identifier[0])) {
            return identifier;
        }

        var chars = identifier.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}