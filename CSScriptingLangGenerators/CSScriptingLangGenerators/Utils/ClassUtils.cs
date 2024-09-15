using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Utils;

public class ClassUtils
{
    
    public static bool IsDerivedFrom(INamedTypeSymbol classSymbol, INamedTypeSymbol targetSymbol) {
        var baseType = classSymbol.BaseType;

        while (baseType != null) {
            if (SymbolEqualityComparer.Default.Equals(baseType, targetSymbol)) {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
    
    public static ClassDeclarationSyntax GetDeclarationFor(INamedTypeSymbol symbol, Compilation compilation) {
        foreach (var syntaxTree in compilation.SyntaxTrees) {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classDeclarations) {
                if (semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol && SymbolEqualityComparer.Default.Equals(classSymbol, symbol)) {
                    return classDeclaration;
                }
            }
        }

        return null;
    }
    
    public static List<ClassDeclarationSyntax> GetDeclarationsFor(IEnumerable<INamedTypeSymbol> symbols, Compilation compilation) {
        var declarations = new List<ClassDeclarationSyntax>();

        foreach (var symbol in symbols) {
            foreach (var syntaxTree in compilation.SyntaxTrees) {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations) {
                    if (semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol && SymbolEqualityComparer.Default.Equals(classSymbol, symbol)) {
                        declarations.Add(classDeclaration);
                    }
                }
            }
        }

        return declarations;
    }
    
    public static List<INamedTypeSymbol> GetDerivedTypes(
        Compilation      compilation,
        INamedTypeSymbol instructionBaseSymbol,
        bool             includeAbstract = false
    ) {
        var derivedTypes = new List<INamedTypeSymbol>();

        foreach (var syntaxTree in compilation.SyntaxTrees) {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);

            var classDeclarations = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDeclaration in classDeclarations) {
                if (
                    semanticModel.GetDeclaredSymbol(classDeclaration) is INamedTypeSymbol classSymbol &&
                    IsDerivedFrom(classSymbol, instructionBaseSymbol)
                ) {

                    // Skip any which are abstract
                    if (classSymbol.IsAbstract && !includeAbstract) {
                        continue;
                    }

                    derivedTypes.Add(classSymbol);
                }
            }
        }

        return derivedTypes;
    }
}