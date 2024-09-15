using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CSScriptingLangGenerators.RTObjects;

[Generator]
public class RTTypeSourceGenerator : IIncrementalGenerator
{
    public INamedTypeSymbol       BaseRuntimeType { get; private set; }
    public List<INamedTypeSymbol> RuntimeTypes    { get; private set; } = new();

    public INamedTypeSymbol       BaseRuntimeValueType { get; private set; }
    public List<INamedTypeSymbol> RuntimeValues        { get; private set; } = new();

    public bool Loadedtypes { get; set; }

    private void EnsureTypesLoaded(Compilation compilation) {
        if (Loadedtypes) return;

        BaseRuntimeType      = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.Types.RuntimeTypeInfo");
        BaseRuntimeValueType = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.RuntimeValue");

        RuntimeTypes  = ClassUtils.GetDerivedTypes(compilation, BaseRuntimeType);
        RuntimeValues = ClassUtils.GetDerivedTypes(compilation, BaseRuntimeValueType);

        Loadedtypes = true;
    }


    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var classDeclarations = context.SyntaxProvider
           .CreateSyntaxProvider(
                (s,   _) => IsCandidateClass(s),
                (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
           .Where(m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(
            classDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndClasses,
            (spc, source) => Execute(source.Left, source.Right, spc)
        );

    }

    private bool IsCandidateClass(SyntaxNode syntaxNode) {
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            return false;

        // Return true if it has the `RuntimeValueAttribute` attribute
        var hasAttr = classDeclaration.AttributeLists.SelectMany(al => al.Attributes)
           .Any(a => a.Name.ToString() == "RuntimeValueAttribute" || a.Name.ToString() == "RuntimeValue");
        
        return hasAttr;
    }

    private ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
        var classDeclaration = (ClassDeclarationSyntax) context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.BaseType is null)
            return null;

        if (symbol.ContainingNamespace.ToDisplayString() != "CSScriptingLang.RuntimeValues")
            return null;

        return classDeclaration;
    }


    private struct NativeMethodBind
    {
        public ClassDeclarationSyntax  ClassDeclaration  { get; set; }
        public string                  MethodName        { get; set; }
        public MethodDeclarationSyntax MethodDeclaration { get; set; }
    }


    private void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        var w = new Writer()
           .WithNamespace("CSScriptingLang.RuntimeValues")
           .WithImports("System.Collections.Generic");


        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;

            var className = classSymbol.Name;

            var methods = classDeclaration.Members
               .OfType<MethodDeclarationSyntax>()
               .Where(
                    method => method.AttributeLists
                       .SelectMany(al => al.Attributes)
                       .Any(attr => {
                            var attrType = model.GetTypeInfo(attr).Type;
                            return attrType?.Name is "NativeFunctionBindAttribute" or "NativeFunctionBind";
                        })
                )
               .Select(method => new NativeMethodBind {
                    ClassDeclaration  = classDeclaration,
                    MethodName        = method.Identifier.Text,
                    MethodDeclaration = method
                });

            /*
            using (w.B($"public partial class {className}")) {

                using (w.B("public void Natives()")) { }

            }
            */

        }

        context.AddSource($"RuntimeValue.NativeBindings.g.cs", SourceText.From(w.ToString(), Encoding.UTF8));
    }
}