using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Utils;

public static class DeclarationExtensions
{
    public static bool HasName(this ITypeSymbol @this, params string[] names) {
        return names.Contains(@this.Name);
    }

    public static bool HasName(this SyntaxNode @this, params string[] names) {
        foreach (var node in @this.ChildNodes()) {
            string nameStr = null;
            if (node is IdentifierNameSyntax identifierNameSyntax) {
                nameStr = identifierNameSyntax.Identifier.Text;
            } else if (node is GenericNameSyntax genericNameSyntax) {
                nameStr = genericNameSyntax.Identifier.Text;
            } else if (node is QualifiedNameSyntax qualifiedNameSyntax) {
                nameStr = qualifiedNameSyntax.Right.Identifier.Text;
            }

            if (nameStr is not null && names.Contains(nameStr)) {
                return true;
            }
        }

        return false;
    }
    
}