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
public class VirtualMachineInstructionHandlerSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor MissingInstructionHandlerMethod = new(
        "CSSG002",
        "Missing instruction handler method",
        "[{0}] Missing handler method: private void On{1}({0} inst)",
        "VirtualMachine",
        DiagnosticSeverity.Warning,
        true
    );


    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context.SyntaxProvider.CreateSyntaxProvider(
            (s,   _) => s is ClassDeclarationSyntax {Identifier.Text: "VirtualMachine"},
            (ctx, _) => GetClassDeclarationForSourceGen(ctx)
        );


        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right!))
        );
    }

    private static ClassDeclarationSyntax GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context
    ) {
        var structDeclarationSyntax = (ClassDeclarationSyntax) context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(structDeclarationSyntax) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.ContainingNamespace.ToDisplayString() != "CSScriptingLang.VM")
            return null;

        return symbol.Name != "VirtualMachine" ? null : structDeclarationSyntax;
    }

    private void GenerateCode(
        SourceProductionContext                context,
        Compilation                            compilation,
        ImmutableArray<ClassDeclarationSyntax> declarations
    ) {
        var instructionBaseSymbol = compilation.GetTypeByMetadataName("CSScriptingLang.VM.Instructions.Instruction");
        if (instructionBaseSymbol == null) {
            return;
        }

        var initialDerivedTypes = ClassUtils.GetDerivedTypes(compilation, instructionBaseSymbol);

        /*var derivedTypes = initialDerivedTypes.Except(
            initialDerivedTypes
               .SelectMany(t => initialDerivedTypes.Where(t2 => SymbolEqualityComparer.Default.Equals(t2.BaseType, t)))
        ).ToList();

        var missingDerivedTypes = initialDerivedTypes.Except(derivedTypes).ToList();
        foreach (var missingDerivedType in missingDerivedTypes) {
            // Add `missingDerivedType` at the start of the derived types list
            derivedTypes.Insert(0, missingDerivedType);
        }*/


        // Remove any which are inherited types, we only want the most outer types
        var derivedTypes = initialDerivedTypes
           .Where(t => {
                return initialDerivedTypes.All(t2 => !SymbolEqualityComparer.Default.Equals(t, t2.BaseType));
            })
            // Move base types of x type after all outer most types
           .ToList();

        var initialChildTypes = initialDerivedTypes
            // Exclude all from derived types
           .Where(t => !derivedTypes.Contains(t))
           .Where(t => derivedTypes.Any(d => SymbolEqualityComparer.Default.Equals(d.BaseType, t)))
           .ToList();

#pragma warning disable RS1024
        var childTypesMap = new Dictionary<INamedTypeSymbol, List<INamedTypeSymbol>>();
#pragma warning restore RS1024

        foreach (var type in initialChildTypes) {
            var childTypes = ClassUtils.GetDerivedTypes(compilation, type, true);

            if (!childTypesMap.ContainsKey(type)) {
                childTypesMap[type] = [];
            }

            childTypesMap[type].AddRange(childTypes);
        }

        // derivedTypes = initialDerivedTypes
        //    .OrderBy(t => childTypesMap.TryGetValue(t, out var value) ? value.Count : 0)
        //    .ToList();


        // find all `On{InstructionName}` methods which take an instruction as a parameter
        var instructionMethods = declarations
           .SelectMany(d => d.Members.OfType<MethodDeclarationSyntax>())
           .Where(m => {
                if (!m.Identifier.Text.StartsWith("On"))
                    return false;
                if (m.ParameterList.Parameters.Count != 1)
                    return false;
                if (m.ParameterList.Parameters[0].Type is not IdentifierNameSyntax identifierNameSyntax)
                    return false;

                return true;
                // return derivedTypes.Any(t => t.Name == identifierNameSyntax.Identifier.Text);
            })
           .Select(m => {
                var paramType = ((IdentifierNameSyntax) m.ParameterList.Parameters[0].Type!).Identifier.Text;
                return new {
                    Name                 = m.Identifier.Text,
                    ParameterType        = paramType,
                    ShortInstructionName = paramType.Replace("Instruction", ""),
                    HasFallthrouhAttr = m.AttributeLists
                       .SelectMany(a => a.Attributes)
                       .Any(a => a.Name.ToString() == "InstructionHandlerAllowFallthrough")
                };
            })
           .ToDictionary(m => m.Name, m => m);

        // Ensure all values in `childTypesMap` are removed from `derivedTypes`
#pragma warning disable RS1024
        var mostOuterMostTypes = derivedTypes.Where(t => !childTypesMap.Values.SelectMany(v => v).Contains(t)).ToList();
        /*var childTypesList = new List<INamedTypeSymbol>();
        foreach (var pair in childTypesMap) {
            if(instructionMethods.TryGetValue($"On{pair.Key.Name.Replace("Instruction", "")}", out var method)) {
                if(method.HasFallthrouhAttr) {
                    childTypesList.Add(pair.Key);
                }

                childTypesList.AddRange(pair.Value);

                if(!method.HasFallthrouhAttr) {
                    mostOuterMostTypes.Add(pair.Key);
                }
            }

        }*/
#pragma warning restore RS1024


        var cases = mostOuterMostTypes
           .Where(t => instructionMethods.ContainsKey($"On{t.Name.Replace("Instruction", "")}"))
           .Select(t => new {
                Type = t,
                // HasFallthrough = instructionMethods.TryGetValue($"On{t.Name.Replace("Instruction", "")}", out var method) && method.HasFallthrouhAttr
            })
           .Select(
                t =>
                    $"            case {t.Type.Name} inst: \n" +
                    $"                  On{t.Type.Name.Replace("Instruction", "")}(inst);\n" +
                    $"                  return true;"
                /*$"                  {(!t.HasFallthrough ? "return true;" : "")}"*/
            )
           .ToList();

        foreach (var pair in childTypesMap) {
            var baseType = pair.Key;
            var children = pair.Value;

            // check if the base type class has the `InstructionHandlerAllowFallthrough` class attribute defined
            var baseHasFallthrough = baseType.GetAttributes().Any(a => a.AttributeClass?.Name == "InstructionHandlerAllowFallthroughAttribute");
            var baseMethodExists   = instructionMethods.TryGetValue($"On{pair.Key.Name.Replace("Instruction", "")}", out var method);

            children.ForEach(child => {
                cases.Add(
                    $"            case {child.Name} inst: \n" +
                    $"                  {(baseHasFallthrough && baseMethodExists ? $"{method!.Name}(inst);" : "")}\n" +
                    $"                  On{child.Name.Replace("Instruction", "")}(inst);\n" +
                    $"                  return true;"
                );
            });

        }

        var missingMethods = derivedTypes
           .Where(t => !instructionMethods.ContainsKey($"On{t.Name.Replace("Instruction", "")}"))
           .Select(t => new {Name = t.Name, Locations = t.Locations, ShortName = t.Name.Replace("Instruction", "")})
           .ToList();

        foreach (var missingMethod in missingMethods) {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    MissingInstructionHandlerMethod,
                    missingMethod.Locations[0],
                    missingMethod.Name,
                    missingMethod.ShortName
                )
            );
        }

        var code = $@"// <auto-generated/>

using System;
using System.Collections.Generic;
using CSScriptingLang.Utils;
using CSScriptingLang.VM.Instructions;

namespace CSScriptingLang.VM;

public partial class VirtualMachine
{{

    public bool TryExecuteHandler(Instruction instruction)
    {{
        switch (instruction)
        {{
{string.Join("\n", cases)}
            default: 
                return false;
        }}
    }}

}}
";

        context.AddSource($"VirtualMachine.g.cs", SourceText.From(code, Encoding.UTF8));
    }
}