using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CSScriptingLangGenerators;

[Generator]
public class TokenExtensionSourceGenerator : IIncrementalGenerator
{
    private const string Namespace     = "Generators";
    private const string AttributeName = "ReportAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context.SyntaxProvider
               .CreateSyntaxProvider(
                    (s, _) => {
                        if (s is not ClassDeclarationSyntax classDeclarationSyntax)
                            return false;

                        if (classDeclarationSyntax.Identifier.Text != "Token")
                            return false;

                        return true;
                    },
                    (ctx, _) => GetClassDeclarationForSourceGen(ctx)
                )
            ;

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right!))
        );
    }

    private static ClassDeclarationSyntax GetClassDeclarationForSourceGen(GeneratorSyntaxContext context) {
        var decl = (ClassDeclarationSyntax) context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(decl) is not INamedTypeSymbol structSymbol)
            return null;

        if (structSymbol.ContainingNamespace.ToDisplayString() != "CSScriptingLang.Lexing")
            return null;
        if (structSymbol.Name != "Token")
            return null;

        return decl;
    }

    private void GenerateCode(
        SourceProductionContext                context,
        Compilation                            compilation,
        ImmutableArray<ClassDeclarationSyntax> declarations
    ) {
        var tokenTypeEnumSymbol = compilation.GetTypeByMetadataName("CSScriptingLang.Lexing.TokenType");
        if (tokenTypeEnumSymbol == null)
            return;
        var keywordTypeEnumSymbol = compilation.GetTypeByMetadataName("CSScriptingLang.Lexing.Keyword");
        if (keywordTypeEnumSymbol == null)
            return;
        var operatorTypeEnumSymbol = compilation.GetTypeByMetadataName("CSScriptingLang.Lexing.OperatorType");
        if (operatorTypeEnumSymbol == null)
            return;

        foreach (var decl in declarations) {
            var semanticModel = compilation.GetSemanticModel(decl.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(decl) is not INamedTypeSymbol structSymbol)
                continue;

            var tokenTypeMethods = tokenTypeEnumSymbol.GetMembers()
               .OfType<IFieldSymbol>()
               .Select(m => $@"    public bool Is{m.Name} => Type.HasAny(TokenType.{m.Name});");

            var keywordTypeMethods = keywordTypeEnumSymbol.GetMembers()
               .OfType<IFieldSymbol>()
               .Select(m => $@"    public bool Is{m.Name}Keyword => Keyword.HasAny(Keyword.{m.Name});");

            var operatorTypeMethods = operatorTypeEnumSymbol.GetMembers()
               .OfType<IFieldSymbol>()
               .Select(m => $@"    public bool Is{m.Name}Operator => IsOperator && Op == OperatorType.{m.Name};");

            // Build up the source code
            var code = $@"// <auto-generated/>

using System;
using System.Collections.Generic;
using CSScriptingLang.Utils;

namespace {structSymbol.ContainingNamespace.ToDisplayString()};

public partial class {decl.Identifier.Text}
{{

{string.Join("\n", tokenTypeMethods)}

{string.Join("\n", keywordTypeMethods)}

{string.Join("\n", operatorTypeMethods)}

    public bool Is(TokenType type, bool exact = false) => exact ? Type.HasAll(type) : Type.HasAny(type);
    

}}
";

            context.AddSource($"{decl.Identifier.Text}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}