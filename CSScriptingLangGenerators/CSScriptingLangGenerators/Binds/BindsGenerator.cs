using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSScriptingLangGenerators.Bindings;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Binds;

public class GeneratorValuesContext
{
    /// <summary>
    /// The syntax node the attribute is attached to.  For example, with <c>[CLSCompliant] class C { }</c> this would
    /// the class declaration node.
    /// </summary>
    public SyntaxNode TargetNode { get; }

    /// <summary>
    /// The symbol that the attribute is attached to.  For example, with <c>[CLSCompliant] class C { }</c> this would be
    /// the <see cref="INamedTypeSymbol"/> for <c>"C"</c>.
    /// </summary>
    public INamedTypeSymbol TargetSymbol { get; }

    /// <summary>
    /// Semantic model for the file that <see cref="TargetNode"/> is contained within.
    /// </summary>
    public SemanticModel SemanticModel { get; }

    /// <summary>
    /// <see cref="AttributeData"/>s for any matching attributes on <see cref="TargetSymbol"/>.  Always non-empty.  All
    /// these attributes will have an <see cref="AttributeData.AttributeClass"/> whose fully qualified name metadata
    /// name matches the name requested in <see cref="SyntaxValueProvider.ForAttributeWithMetadataName{T}"/>.
    /// <para>
    /// To get the entire list of attributes, use <see cref="ISymbol.GetAttributes"/> on <see cref="TargetSymbol"/>.
    /// </para>
    /// </summary>
    public ImmutableArray<AttributeData> Attributes { get; }

    public GeneratorValuesContext(GeneratorAttributeSyntaxContext ctx) {
        TargetNode    = ctx.TargetNode;
        TargetSymbol  = ctx.TargetSymbol as INamedTypeSymbol;
        SemanticModel = ctx.SemanticModel;
        Attributes    = ctx.Attributes;
    }
}

public class PrototypeGeneratorValuesContext : GeneratorValuesContext
{
    public PrototypeGeneratorValuesContext(GeneratorAttributeSyntaxContext ctx) : base(ctx) { }
}

public class ModuleGeneratorValuesContext : GeneratorValuesContext
{
    public ModuleGeneratorValuesContext(GeneratorAttributeSyntaxContext ctx) : base(ctx) { }
}

public class ClassGeneratorValuesContext : GeneratorValuesContext
{
    public ClassGeneratorValuesContext(GeneratorAttributeSyntaxContext ctx) : base(ctx) { }
}

[Generator]
public class BindsGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context) { }
    
    public void Initializee(IncrementalGeneratorInitializationContext context) {
        var protoProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            Attributes.Prototype.FullyQualifiedName,
            (node, _) => node is ClassDeclarationSyntax,
            (ctx,  _) => new PrototypeGeneratorValuesContext(ctx)
        );
        var moduleProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            Attributes.Module.FullyQualifiedName,
            (node, _) => node is ClassDeclarationSyntax,
            (ctx,  _) => new ModuleGeneratorValuesContext(ctx)
        );
        var classProvider = context.SyntaxProvider.ForAttributeWithMetadataName(
            Attributes.Class.FullyQualifiedName,
            (node, _) => node is ClassDeclarationSyntax,
            (ctx,  _) => new ClassGeneratorValuesContext(ctx)
        );

        var provider = context.CompilationProvider
           .Combine(protoProvider.Collect())
           .Combine(moduleProvider.Collect())
           .Combine(classProvider.Collect());

        context.RegisterSourceOutput(
            provider,
            (spc, p) => {
                var compilation = p.Left.Left.Left;
                var protosCtx   = p.Left.Left.Right;
                var modulesCtx  = p.Left.Right;
                var classesCtx  = p.Right;

                TypeData.Initialize(compilation, spc);


                var modules = modulesCtx.Select(m => new ModuleBindsData(m, compilation)).ToList();
                var classes = classesCtx.Select(c => new ClassBindsData(c, compilation)).ToList();

                GenerateModules(spc, compilation, modules);
                GenerateClasses(spc, compilation, classes);

            }
        );


    }
    private void GenerateModules(SourceProductionContext spc, Compilation compilation, List<ModuleBindsData> modules) {
        foreach (var module in modules) {
            var w = module.w;

            w.AddHeaderLine("#pragma warning disable CS0162 // Unreachable code detected");

            w.Imports([
                "System",
                "System.Collections.Generic",
                $"{Constants.RootNamespace}.RuntimeValues",
                $"{Constants.RootNamespace}.RuntimeValues.Values",
                $"{Constants.RootNamespace}.Interpreter.Context",
                $"{Constants.RootNamespace}.RuntimeValues.Types",
                $"{Constants.RootNamespace}.Interpreter.Libraries",
                $"{Constants.RootNamespace}.Lexing",
                $"{Constants.RootNamespace}.Core.Async",
            ]);

            w.Namespace(module.TargetSymbol);

            w.Class(module.TargetSymbol, (mc) => {

                mc.Class("Library", c => {
                    c.IsPartial = true;
                    c.IsSealed  = true;

                    c.Property("GlobalInstance", p => p
                                  .WithType(module.Qualifier)
                                  .WithStatic()
                    );

                    c.Property("Instance", p => p.WithType(module.Qualifier));

                    c.Method("Library", m => {
                        m.Parameter("inst", p => p.WithType(module.Qualifier));

                        m.WithType(module.Qualifier);

                    });
                });

            });

            var output = w.ToString();

            spc.AddSource(
                $"{module.TargetSymbol.GetFullyQualifiedName()}.Module.g.cs",
                output
            );
        }
    }
    private void GenerateClasses(SourceProductionContext spc, Compilation compilation, List<ClassBindsData> classes) { }


}