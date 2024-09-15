using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;


namespace CSScriptingLangGenerators;

[Generator]
public class AstNodeSourceGenerator : IIncrementalGenerator
{
    private List<INamedTypeSymbol> _derivedTypes       = new();
    private bool                   _loadedDerivedTypes = false;

    public INamedTypeSymbol BaseNode { get; private set; }

    private List<INamedTypeSymbol> GetDerivedTypes(Compilation compilation) {
        if (_loadedDerivedTypes)
            return _derivedTypes;

        BaseNode = compilation.GetTypeByMetadataName($"{Constants.ASTNamespace}.BaseNode");

        _derivedTypes       = ClassUtils.GetDerivedTypes(compilation, BaseNode);
        _loadedDerivedTypes = true;

        return _derivedTypes;
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

        context.RegisterSourceOutput(
            compilationAndClasses,
            (spc, source) => ExecuteVisitor(source.Left, source.Right, spc)
        );

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

    private ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
        // We already know it is a ClassDeclarationSyntax with a BaseList
        var classDeclaration = (ClassDeclarationSyntax) context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.BaseType is null)
            return null;

        if (!Constants.IsNodeNamespace(symbol.ContainingNamespace))
            return null;

        return classDeclaration;
    }

    private void ExecuteVisitor(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        var w = new Writer();
        w.WithNamespace(Constants.ASTNamespace);
        w.WithImports([
            "System.Collections.Generic",
        ]);

        var visitorInterface = new Writer();
        visitorInterface._("public partial interface IAstVisitor");
        visitorInterface.OpenBracket();
        visitorInterface._("void OnVisitAny(BaseNode node);");

        var visitor = new Writer();
        visitor._("public partial class BaseAstVisitor : IAstVisitor");
        visitor.OpenBracket();
        visitor._("public virtual void OnVisitAny(BaseNode node) { }");

        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;

            w.AddReferencedType(classSymbol);

            var className = classSymbol.Name;
            if (classDeclaration.TypeParameterList is not null) {
                var typeParameters = classDeclaration.TypeParameterList.Parameters
                   .Select(p => p.Identifier.Text)
                   .ToList();

                className += $"<{string.Join(", ", typeParameters)}>";
            }

            visitorInterface._($"void Visit{className}({className} node);");
            
            using(visitor.B($"public virtual void Visit{className}({className} node)")) {
                using(visitor.B("if (!_visitedNodes.Add(node))")) {
                    visitor._("return;");
                }
                visitor._("OnVisitAny(node);");
                using(visitor.B("foreach (var child in node.AllNodes())")) {
                    visitor._("child?.Accept(this);");
                }
            }
        }

        visitor.CloseBracket();
        visitorInterface.CloseBracket();

        w._(visitorInterface.ToString());
        w._(visitor.ToString());

        context.AddSource($"IAstVisitor.g.cs", SourceText.From(w.ToString(), Encoding.UTF8));
    }

    private void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {

        foreach (var classDeclaration in classes) {
            var model = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            if (model.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol classSymbol)
                continue;

            var derived = GetDerivedTypes(model.Compilation);

            var w = new Writer();
            w.WithImports(
                "System.Collections.Generic",
                Constants.ASTNamespace
            );
            
            w.WithNamespace(classSymbol.ContainingNamespace.ToDisplayString());

            var className = classSymbol.Name;

            // Skip this class if it already has a `AllNodes` method
            if (
                classDeclaration.Members
               .OfType<MethodDeclarationSyntax>()
               .Any(m => m.Identifier.Text == "AllNodes")
            )
                continue;

            w.AddReferencedType(classSymbol);

            if (classDeclaration.TypeParameterList is not null) {
                var typeParameters = classDeclaration.TypeParameterList.Parameters
                   .Select(p => p.Identifier.Text)
                   .ToList();

                className += $"<{string.Join(", ", typeParameters)}>";
            }

            using (w.B($"public partial class {className}")) {
                using (w.B("public override void Accept(IAstVisitor visitor)")) {
                    w._($"visitor.Visit{className}(this);");
                }

                var nodeProps = classSymbol.GetMembers()
                   .OfType<IPropertySymbol>()
                   .Where(p => {
                        return p.HasAttribute("VisitableNodePropertyAttribute");
                    })
                   .ToList();

                using (w.B("public override IEnumerable<BaseNode> AllNodes()")) {
                    foreach (var property in nodeProps) {
                        var type = property.Type;
                        var name = property.Name;

                        if (type is INamedTypeSymbol {IsGenericType: true} namedTypeSymbol) {
                            var typeArguments = namedTypeSymbol.TypeArguments
                               .Select(t => t.Name)
                               .ToList();

                            w.AddReferencedType(namedTypeSymbol);

                            using (w.B($"if({name} != null)")) {

                                if (namedTypeSymbol.Name == "Dictionary") {
                                    var selector = typeArguments[0] == "String" ? "Value" : "Key";
                                    if (typeArguments.Count == 2 && typeArguments[0] == "String") {
                                        using (w.B($"foreach (var kvp in {name})")) {
                                            using (w.B($"if (kvp.{selector} != null)")) {
                                                w._($"yield return kvp.{selector};");
                                            }
                                        }

                                        continue;
                                    }
                                }

                                if (namedTypeSymbol.Name is "List" or "IEnumerable") {
                                    using (w.B($"foreach (var item in {name})")) {
                                        using (w.B($"if (item != null)")) {
                                            w._($"yield return item;");
                                        }
                                    }

                                    continue;
                                }

                            }

                        }

                        using (w.B($"if({name} != null)")) {
                            w._($"yield return {name};");
                        }
                    }

                    using (w.B("foreach (var node in base.AllNodes())")) {
                        using (w.B("if (node != null)")) {
                            w._("yield return node;");
                        }
                    }
                }
            }

            var fileName = className.Replace("`", "_").Replace("<", "_").Replace(">", "_");
            context.AddSource($"{fileName}.AllNodes.g.cs", SourceText.From(w.ToString(), Encoding.UTF8));

        }

        // context.AddSource($"GetAllNodesMethods.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private string GenerateNodesMethod(ClassDeclarationSyntax classDeclaration, SemanticModel model) {
        var propertyDeclarations = classDeclaration.Members
           .OfType<PropertyDeclarationSyntax>();

        var derived = GetDerivedTypes(model.Compilation);


        var nodeProperties = propertyDeclarations
               .Where(prop => {

                    var hasVisitableAttribute = prop.AttributeLists
                       .SelectMany(al => al.Attributes)
                       .Any(attr => {
                            var attrType = model.GetTypeInfo(attr).Type;
                            return attrType?.Name == "VisitableNodePropertyAttribute";
                        });

                    if (hasVisitableAttribute)
                        return true;

                    return false;
                })
            ;

        var statements = string.Join(
            "\n",
            nodeProperties.Select(p => {

                // handle dictionary types
                var typeInfo = model.GetTypeInfo(p.Type);

                var outStr = $"if({p.Identifier.Text} != null)";

                if (typeInfo.Type is INamedTypeSymbol {IsGenericType: true} namedTypeSymbol) {
                    var typeArguments = namedTypeSymbol.TypeArguments
                       .Select(t => t.Name)
                       .ToList();

                    if (namedTypeSymbol.Name == "Dictionary") {
                        if (typeArguments.Count == 2 && typeArguments[0] == "String") {
                            outStr += $@"
foreach (var kvp in {p.Identifier.Text})
    if (kvp.Value != null) 
        yield return kvp.Value;
";
                            return outStr;
                        }

                        if (typeArguments.Count == 2 && typeArguments[1] == "String") {
                            outStr += $@"
foreach (var kvp in {p.Identifier.Text})
    if (kvp.Key != null)
        yield return kvp.Key;
";
                        }
                    }

                    if (namedTypeSymbol.Name is "List" or "IEnumerable") {
                        outStr += $@"
foreach (var item in {p.Identifier.Text})
    if (item != null)
        yield return item;
";
                        return outStr;
                    }
                }

                outStr += $"yield return {p.Identifier.Text};";
                return outStr;
            })
        );
        // var methodBody = string.Join("\n", nodeProperties.Select(prop => $"            yield return {prop};"));

        return $@"
        public override IEnumerable<BaseNode> AllNodes() {{
            {statements}
            foreach (var node in base.AllNodes()) {{
                yield return node;
            }}
        }}
        ";
    }
}