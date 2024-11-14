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
        if (HasName(names, @this))
            return true;

        foreach (var node in @this.ChildNodes()) {
            if (HasName(names, node))
                return true;
        }

        return false;
    }

    private static bool HasName(string[] names, SyntaxNode node) {
        string nameStr = null;
        if (node is IdentifierNameSyntax identifierNameSyntax) {
            nameStr = identifierNameSyntax.Identifier.Text;
        } else if (node is GenericNameSyntax genericNameSyntax) {
            nameStr = genericNameSyntax.Identifier.Text;
        } else if (node is QualifiedNameSyntax qualifiedNameSyntax) {
            nameStr = qualifiedNameSyntax.Right.Identifier.Text;
        } else if (node is PropertyDeclarationSyntax propertyDeclarationSyntax) {
            nameStr = propertyDeclarationSyntax.Identifier.Text;
        } else if (node is MethodDeclarationSyntax methodDeclarationSyntax) {
            nameStr = methodDeclarationSyntax.Identifier.Text;
        } else if (node is ClassDeclarationSyntax classDeclarationSyntax) {
            nameStr = classDeclarationSyntax.Identifier.Text;
        } else if (node is StructDeclarationSyntax structDeclarationSyntax) {
            nameStr = structDeclarationSyntax.Identifier.Text;
        } else if (node is InterfaceDeclarationSyntax interfaceDeclarationSyntax) {
            nameStr = interfaceDeclarationSyntax.Identifier.Text;
        } else if (node is EnumDeclarationSyntax enumDeclarationSyntax) {
            nameStr = enumDeclarationSyntax.Identifier.Text;
        } else if (node is DelegateDeclarationSyntax delegateDeclarationSyntax) {
            nameStr = delegateDeclarationSyntax.Identifier.Text;
        } else if (node is FieldDeclarationSyntax fieldDeclarationSyntax) {
            nameStr = fieldDeclarationSyntax.Declaration.Variables.First().Identifier.Text;
        } else if (node is EventDeclarationSyntax eventDeclarationSyntax) {
            nameStr = eventDeclarationSyntax.Identifier.Text;
        } else if (node is EventFieldDeclarationSyntax eventFieldDeclarationSyntax) {
            nameStr = eventFieldDeclarationSyntax.Declaration.Variables.First().Identifier.Text;
        } else if (node is ConstructorDeclarationSyntax constructorDeclarationSyntax) {
            nameStr = constructorDeclarationSyntax.Identifier.Text;
        } else if (node is DestructorDeclarationSyntax destructorDeclarationSyntax) {
            nameStr = destructorDeclarationSyntax.Identifier.Text;
        } else if (node is IndexerDeclarationSyntax indexerDeclarationSyntax) {
            nameStr = indexerDeclarationSyntax.ThisKeyword.Text;
        } else if (node is OperatorDeclarationSyntax operatorDeclarationSyntax) {
            nameStr = operatorDeclarationSyntax.OperatorToken.Text;
        } else if (node is ConversionOperatorDeclarationSyntax conversionOperatorDeclarationSyntax) {
            nameStr = conversionOperatorDeclarationSyntax.ImplicitOrExplicitKeyword.Text;
        } else if (node is EnumMemberDeclarationSyntax enumMemberDeclarationSyntax) {
            nameStr = enumMemberDeclarationSyntax.Identifier.Text;
        }
        if (nameStr is not null && names.Contains(nameStr)) {
            return true;
        }
        return false;
    }

}