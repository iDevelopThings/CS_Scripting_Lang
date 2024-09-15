using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CSScriptingLangGenerators;

[Generator]
public class InterpreterSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        /*return;
        
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            (s,   _) => IsCandidateClass(s),
            (ctx, _) => GetClassDeclarationForSourceGen(ctx)
        ).Where(m => m is not null);


        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => ExecuteInterpreterGeneration(ctx, t.Left, t.Right!))
        );*/
    }
    private bool IsCandidateClass(SyntaxNode syntaxNode) {
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            return false;
        if (classDeclaration.BaseList is null)
            return false;
        if (classDeclaration.BaseList.Types.Count == 0)
            return false;

        var baseTypes = classDeclaration.BaseList.Types;
        if (baseTypes.Count == 0)
            return false;

        // Return true if it has the `ASTNode` attribute
        return classDeclaration.AttributeLists
           .SelectMany(al => al.Attributes)
           .Any(attr => {
                var attrType = attr.Name as IdentifierNameSyntax;
                return attrType?.Identifier.Text == "ASTNode";
            });
    }

    private static ClassDeclarationSyntax GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context
    ) {
        var classDeclaration = (ClassDeclarationSyntax) context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.BaseType is null)
            return null;

        if (!symbol.ContainingNamespace.ToDisplayString().Contains(Constants.ASTNamespace))
            return null;

        return classDeclaration;
    }

    private void ExecuteInterpreterGeneration(
        SourceProductionContext                context,
        Compilation                            compilation,
        ImmutableArray<ClassDeclarationSyntax> classes
    ) {
        var interpreterSymbols = compilation.GetTypesByMetadataName("CSScriptingLang.Interpreter.Interpreter");
        if (interpreterSymbols.Length == 0)
            return;

        var genericTypes = classes
           .Where(d => d.Identifier.Text != "BaseNode")
           .Where(c => c.BaseList is not null && c.BaseList.Types.Any(t => t.Type is GenericNameSyntax))
           .SelectMany(c => c.BaseList.Types)
           .Select(t => t.Type)
           .OfType<GenericNameSyntax>()
           .Select(g => new {
                Declaration = g,
                ClassName   = g.Identifier.Text + "<" + string.Join(", ", g.TypeArgumentList.Arguments.Select(a => a.ToString())) + ">",
                Symbol      = compilation.GetTypeByMetadataName($"CSScriptingLang.Parsing.AST.{g.Identifier.Text}"),
            })
           .ToList();

        var declarationsInfo = classes
           .Where(d => d.Identifier.Text != "BaseNode")
           .Select(c => {
                var model     = compilation.GetSemanticModel(c.SyntaxTree);
                var symbol    = model.GetDeclaredSymbol(c);
                var className = symbol.Name;

                /*if (c.TypeParameterList is not null) {
                    var typeParameters = c.TypeParameterList.Parameters
                       .Select(p => p.Identifier.Text)
                       .ToList();

                    className += $"<{string.Join(", ", typeParameters)}>";
                }*/

                return new {
                    Declaration = c,
                    Model       = model,
                    Symbol      = symbol,
                    ClassName   = className,
                };
            })
           .Where(d => genericTypes.All(g => g.Declaration.Identifier.Text != d.ClassName))
           .ToList();

        Dictionary<string, IMethodSymbol> execMethods = new();
        foreach (var symb in interpreterSymbols) {
            var members = symb.GetMembers("Execute");
            foreach (var member in members) {
                if (member is not IMethodSymbol method)
                    continue;

                if (method.Parameters.Length != 1)
                    continue;
                var param = method.Parameters[0];
                execMethods[param.Type!.Name] = method;
            }
        }

        /*var execMethods = interpreterSymbols
           .SelectMany(i => i.GetMembers("Execute"))
           .OfType<IMethodSymbol>()
           .Where(m => m.Parameters != null && m.Parameters.Length == 1 && m.Parameters[0].Type != null)
           .ToDictionary(m => m.Parameters[0].Type!.Name, m => m);*/

        var allClassNames = declarationsInfo.Select(d => d.ClassName).ToList();
        allClassNames.AddRange(genericTypes.Select(g => g.ClassName));

        var undefinedMethods = allClassNames
           .Where(d => !execMethods.ContainsKey(d))
           .ToList();

        /*
        public void Execute(BaseNode node)
        {{
            switch (node)
            {{
                {string.Join("\n", allClassNames.Select(t => $"        case {t} n: Execute(n); break;"))}
                default:
                throw new NotImplementedException();
            }}
        }}
        */


        var defaultMethodsImpl = string.Join(
            "\n",
            undefinedMethods.Select(m => $@"
    private void Execute({m} node) {{ 
        throw new NotImplementedException(); 
    }}
")
        );

        var code = $@"// <auto-generated/>

using System;
using System.Collections.Generic;
using CSScriptingLang.Utils;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{{


{defaultMethodsImpl}
}}
";

        context.AddSource($"Interpreter.g.cs", SourceText.From(code, Encoding.UTF8));

    }

    private void GenerateCode(
        SourceProductionContext                context,
        Compilation                            compilation,
        ImmutableArray<ClassDeclarationSyntax> declarations
    ) {
        var baseNodeType = compilation.GetTypeByMetadataName("CSScriptingLang.Parsing.AST.BaseNode");
        if (baseNodeType == null) {
            return;
        }

        // var excludedTypes = new[] {/*"NodeList"*/};

        var initialDerivedTypes = ClassUtils.GetDerivedTypes(compilation, baseNodeType)
            // .Where(t => !excludedTypes.Contains(t.Name))
           .ToList();

        // Remove any which are inherited types, we only want the most outer types
        var derivedTypes = initialDerivedTypes
           .Where(t => {
                return initialDerivedTypes.All(t2 => !SymbolEqualityComparer.Default.Equals(t, t2.BaseType));
            })
            // Move base types of x type after all outer most types
           .ToList();

        var baseExecMethods = declarations
           .SelectMany(d => d.Members.OfType<MethodDeclarationSyntax>())
           .Where(m => m.Identifier.Text == "Execute")
           .ToList();

        var execMethods = baseExecMethods
           .Where(m => {
                if (m.ParameterList.Parameters.Count != 1)
                    return false;
                if (m.ParameterList.Parameters[0].Type is not IdentifierNameSyntax identifierNameSyntax)
                    return false;

                return true;
            })
           .Select(m => {
                var paramType = ((IdentifierNameSyntax) m.ParameterList.Parameters[0].Type!).Identifier.Text;
                return new {
                    Name          = m.Identifier.Text,
                    ParameterType = paramType,
                };
            })
           .ToList();

        var execMethodsByType = execMethods.ToDictionary(m => m.ParameterType, m => m);

        var undefinedMethods = derivedTypes
           .Where(t => !execMethodsByType.ContainsKey(t.Name))
           .ToList();

        var defaultMethodsImpl = string.Join(
            "\n",
            undefinedMethods.Select(m => $@"
    private void Execute({m.Name} node) {{ 
        throw new NotImplementedException(); 
    }}
")
        );

        /*
         public bool Execute(BaseNode node)
    {{
        switch (node)
        {{
{string.Join("\n", cases)}
            default:
                return false;
        }}
    }}
         */
        var code = $@"// <auto-generated/>

using System;
using System.Collections.Generic;
using CSScriptingLang.Utils;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter;

public partial class Interpreter
{{

public void Execute(BaseNode node)
{{
    switch (node)
    {{
{string.Join("\n", derivedTypes.Select(t => $"        case {t.Name} n: Execute(n); break;"))}
        default:
            throw new NotImplementedException();
    }}
}}

{defaultMethodsImpl}
}}
";

        context.AddSource($"Interpreter.g.cs", SourceText.From(code, Encoding.UTF8));
    }
}