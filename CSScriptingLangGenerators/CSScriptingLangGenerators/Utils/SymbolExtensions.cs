using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

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

public static partial class SymbolExtensions
{
    public static IEnumerable<IMethodSymbol> GetConstructors(this INamedTypeSymbol symbol, Func<IMethodSymbol, bool> predicate = null) {
        foreach (var member in symbol.GetMembers()) {
            if (member is not IMethodSymbol method) {
                continue;
            }
            if (predicate != null && predicate(method)) {
                yield return method;
            } else if (method.MethodKind == MethodKind.Constructor) {
                yield return method;
            }
        }
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

    public static bool ImplementsInterface(this INamedTypeSymbol type, INamedTypeSymbol interfaceType) {
        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceType));
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

    public static string ToIdentifierCasing(this string identifier, bool lowerFirst = true) {
        // if the string is all uppercase, then leave it as is
        // otherwise it should be camelCased

        if (string.IsNullOrEmpty(identifier)) {
            return identifier;
        }

        var chars = identifier.ToCharArray();

        if (chars.All(char.IsUpper)) {
            return identifier;
        }

        if(lowerFirst) {
            chars[0] = char.ToLowerInvariant(chars[0]);
        }

        return new string(chars);
    }

    public static bool IsArrayInterface(this ITypeSymbol type, out ITypeSymbol typeArgument) {
        if (type.BaseType?.SpecialType == SpecialType.System_Array) {
            // typeArgument = type.TypeArguments[0];
            typeArgument = ((IArrayTypeSymbol) type).ElementType;
            return true;
        }
        /*if (type is INamedTypeSymbol namedType) {

            foreach (var itype in namedType.AllInterfaces) {
                if (itype.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_ICollection_T) {
                    typeArgument = itype.TypeArguments[0];
                    return true;
                }
            }
        }*/

        typeArgument = default;
        return false;
    }
    
    
    
}