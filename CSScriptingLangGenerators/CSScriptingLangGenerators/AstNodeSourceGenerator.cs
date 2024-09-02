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
public class AstNodeSourceGenerator : IIncrementalGenerator
{
    private List<INamedTypeSymbol> _derivedTypes       = new();
    private bool                   _loadedDerivedTypes = false;

    public INamedTypeSymbol BaseNode { get; private set; }

    private List<INamedTypeSymbol> GetDerivedTypes(Compilation compilation) {
        if (_loadedDerivedTypes)
            return _derivedTypes;

        BaseNode = compilation.GetTypeByMetadataName("CSScriptingLang.Parsing.AST.BaseNode");

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

        if (symbol.ContainingNamespace.ToDisplayString() != "CSScriptingLang.Parsing.AST")
            return null;

        return classDeclaration;
    }

    private void ExecuteVisitor(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        var source = new StringBuilder();
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine("namespace CSScriptingLang.Parsing.AST;");

        var visitorInterfaceSb = new StringBuilder();
        visitorInterfaceSb.AppendLine("public partial interface IAstVisitor {");

        var visitorSb = new StringBuilder();
        visitorSb.AppendLine("public partial class BaseAstVisitor : IAstVisitor {");

        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;

            var className = classSymbol.Name;
            if (classDeclaration.TypeParameterList is not null) {
                var typeParameters = classDeclaration.TypeParameterList.Parameters
                   .Select(p => p.Identifier.Text)
                   .ToList();

                className += $"<{string.Join(", ", typeParameters)}>";
            }

            visitorInterfaceSb.AppendLine($"    void Visit{className}({className} node);");

            visitorSb.AppendLine($@"
    public virtual void Visit{className}({className} node) {{
        if (!_visitedNodes.Add(node))
            return;
        
        foreach (var child in node.AllNodes()) {{
            child?.Accept(this);
        }}
    }}"
            );

        }

        visitorInterfaceSb.AppendLine("}");
        visitorSb.AppendLine("}");

        source.AppendLine(visitorInterfaceSb.ToString());
        source.AppendLine(visitorSb.ToString());

        context.AddSource($"IAstVisitor.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        var source = new StringBuilder();
        source.AppendLine("using System.Collections.Generic;");
        source.AppendLine("namespace CSScriptingLang.Parsing.AST;");


        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;

            // var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className   = classSymbol.Name;
            var nodesMethod = GenerateNodesMethod(classDeclaration, model);

            // Skip this class if it already has a `AllNodes` method
            if (
                classDeclaration.Members
               .OfType<MethodDeclarationSyntax>()
               .Any(m => m.Identifier.Text == "AllNodes")
            )
                continue;

            // handle class name generics
            if (classDeclaration.TypeParameterList is not null) {
                var typeParameters = classDeclaration.TypeParameterList.Parameters
                   .Select(p => p.Identifier.Text)
                   .ToList();

                className += $"<{string.Join(", ", typeParameters)}>";
            }

            source.AppendLine($@"
    public partial class {className} {{
        public override void Accept(IAstVisitor visitor) {{
            visitor.Visit{className}(this);
        }}
        {nodesMethod}
    }}
");

        }

        context.AddSource($"GetAllNodesMethods.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
    }

    private string GenerateNodesMethod(ClassDeclarationSyntax classDeclaration, SemanticModel model) {
        var propertyDeclarations = classDeclaration.Members
           .OfType<PropertyDeclarationSyntax>();

        var derived = GetDerivedTypes(model.Compilation);

        /*
        var baseTypes = classDeclaration.BaseList?.Types.ToList() ?? new List<BaseTypeSyntax>();

        foreach (var baseType in baseTypes) {
            var typeInfo = model.GetTypeInfo(baseType.Type);
            if (typeInfo.Type is null)
                continue;
            if (typeInfo.Type.Name == "BaseNode")
                continue;

            var baseTypePropertySymbols = typeInfo.Type.GetMembers().OfType<IPropertySymbol>();
            var baseTypeProperties = baseTypePropertySymbols
                   .Select(
                        p => p.DeclaringSyntaxReferences.First().GetSyntax() as PropertyDeclarationSyntax
                    )
                   .Where(p => p != null)
                ;
               // .Where(p => p.AccessorList?.Accessors.Count == 2);

            propertyDeclarations = propertyDeclarations.Concat(baseTypeProperties);

        }*/

        // propertyDeclarations = propertyDeclarations.Concat(
        //     parents?.Select(p => p.DeclaringSyntaxReferences.First().GetSyntax() as PropertyDeclarationSyntax) ?? Enumerable.Empty<PropertyDeclarationSyntax>()
        // );

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
                    /*
                    var typeInfo = model.GetTypeInfo(prop.Type);
                    var isNodeType = typeInfo.Type != null &&
                                     derived.Any(d => SymbolEqualityComparer.Default.Equals(d, typeInfo.Type));
                    if (!isNodeType)
                        return false;

                    /*if (prop.AccessorList != null) {
                        var hasGetter = false;
                        var hasSetter = false;
                        foreach (var accessor in prop.AccessorList.Accessors) {
                            if (accessor.Keyword.Text == "get")
                                hasGetter = true;
                            if (accessor.Keyword.Text == "set")
                                hasSetter = true;
                        }

                        if(hasGetter || (hasGetter && hasSetter))
                            return true;
                    }#1#


                    return hasVisitableAttribute;*/
                })
            ;
        // .Select(prop => prop.Identifier.Text);

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

                    if (namedTypeSymbol.Name == "List") {
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