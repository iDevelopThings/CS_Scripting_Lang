using System;
using System.Collections.Immutable;
using System.Linq;
using CSScriptingLangGenerators.Bindings;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.SyntaxTreeGenerators;

[Generator]
public class SyntaxNodeExtensionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            Attributes.SyntaxNode.FullyQualifiedName,
            (node, _) => node is ClassDeclarationSyntax cd && !cd.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            (ctx,  _) => ctx
        );

        var sources = context.CompilationProvider.Combine(provider.Collect());

        context.RegisterSourceOutput(
            sources,
            (spc, p) => GenerateCode(spc, p.Left, p.Right)
        );
    }

    private void GenerateCode(
        SourceProductionContext                         ctx,
        Compilation                                     compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> sources
    ) {
        try {
            if (!TypeData.Initialize(compilation, ctx.ReportDiagnostic)) {
                return;
            }

            GenerateVisitor(
                ctx, compilation, sources,
                canGenerateTokenMethods: false
            );

            GenerateNodeExtensions(
                ctx, compilation, sources,
                canGenerateTokenMethods: false
            );
        }
        catch (Exception e) {
            ctx.ReportDiagnostic(Diagnostic.Create(Diagnostics.ErrorGeneratingBindings, Location.None, e.ToString()));
            GeneratorLogger.Log(e.ToString());
        }
    }

    public struct VisitorNodeData(
        TypeDeclarationSyntax node,
        INamedTypeSymbol      symbol
    )
    {
        public TypeDeclarationSyntax Node   { get; } = node;
        public INamedTypeSymbol      Symbol { get; } = symbol;

        public string SymbolName => Symbol.Name;

        public string TypeParams {
            get {
                if (Node.TypeParameterList is null)
                    return null;

                var typeParameters = Node.TypeParameterList.Parameters
                   .Select(p => p.Identifier.Text)
                   .ToList();

                return $"<{string.Join(", ", typeParameters)}>";
            }
        }

        public string SymbolNameWithTypeParams {
            get {
                var className = Symbol.Name;
                if (Node.TypeParameterList is not null) {
                    className += TypeParams;
                }
                return className;
            }
        }

        public string VisitorMethodName => $"Visit_{SymbolName}";
    }

    private void GenerateVisitor(
        SourceProductionContext                         ctx,
        Compilation                                     compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> sources,
        bool                                            canGenerateTokenMethods
    ) {
        var w = new ClassWriter();

        var visitorNodes = sources
           .Select(
                n => new VisitorNodeData(
                    (TypeDeclarationSyntax) n.TargetNode,
                    (INamedTypeSymbol) n.TargetSymbol
                )
            )
           .Where(
                n => {
                    if (canGenerateTokenMethods)
                        return true;

                    if (compilation.HasImplicitConversion(n.Symbol, TypeData.SyntaxTokenSymbol))
                        return false;

                    return true;
                }
            )
           .ToList();

        w.Using(TypeData.SyntaxNodeSymbol);
        w.Using(visitorNodes.Select(n => n.Symbol));

        w.Imports(
            [
                "System",
                "System.Collections.Generic",
            ]
        );

        w.Namespace("CSScriptingLang.IncrementalParsing.Syntax");

        w.Interface(
            "ISyntaxNodeVisitor", c => {
                c.Method(
                    "OnVisitAny", m => {
                        m.Parameter("node", "SyntaxNode", p => { });
                    }
                );
                c.Method(
                    "OnEnter", m => {
                        m.Parameter("node", "SyntaxNode", p => { });
                    }
                );
                c.Method(
                    "OnExit", m => {
                        m.Parameter("node", "SyntaxNode", p => { });
                    }
                );

                foreach (var node in visitorNodes) {
                    c.Method(
                        node.VisitorMethodName, m => {
                            m.Parameter("node", node.Symbol, p => { });
                        }
                    );
                }
            }
        );

        w.Class(
            "BaseSyntaxNodeVisitor", c => {
                c.Implements("ISyntaxNodeVisitor");

                c.Property(
                    "VisitedNodes", p => {
                        p.WithType("HashSet<SyntaxNode>")
                           .WithAccessibility(Accessibility.Public)
                           .WithDefaultValue("new()");
                    }
                );

                c.Method(
                    "OnVisitAny", m => {
                        m.Accessibility = Accessibility.Public;
                        m.IsVirtual     = true;
                        m.Parameter("node", "SyntaxNode", p => { });

                    }
                );
                c.Method(
                    "OnEnter", m => {
                        m.Accessibility = Accessibility.Public;
                        m.IsVirtual     = true;
                        m.Parameter("node", "SyntaxNode", p => { });
                    }
                );
                c.Method(
                    "OnExit", m => {
                        m.Accessibility = Accessibility.Public;
                        m.IsVirtual     = true;
                        m.Parameter("node", "SyntaxNode", p => { });
                    }
                );


                foreach (var node in visitorNodes) {
                    c.Method(
                        node.VisitorMethodName,
                        m => {
                            m.Accessibility = Accessibility.Public;
                            m.IsVirtual     = true;
                            m.Parameter("node", node.Symbol, p => { });


                            using (m.Body.Block("if(!VisitedNodes.Add(node))")) {
                                m.Body.WriteLine("return;");
                            }

                            m.Body.WriteLine("OnEnter(node);");
                            m.Body.WriteLine("OnVisitAny(node);");

                            using (m.Body.Block("foreach(var child in node.ChildrenNode)")) {
                                m.Body.WriteLine("child?.Accept(this);");
                            }

                            m.Body.WriteLine("OnExit(node);");
                        }
                    );
                }
            }
        );

        var output = w.ToString();
        ctx.AddSource("NodeVisitor.g.cs", output);

    }

    private void GenerateNodeExtensions(
        SourceProductionContext                         ctx,
        Compilation                                     compilation,
        ImmutableArray<GeneratorAttributeSyntaxContext> sources,
        bool                                            canGenerateTokenMethods
    ) {
        var w = new ClassWriter();

        var visitorNodes = sources
           .Select(
                n => new VisitorNodeData(
                    (TypeDeclarationSyntax) n.TargetNode,
                    (INamedTypeSymbol) n.TargetSymbol
                )
            )
           .Where(
                n => {
                    if (canGenerateTokenMethods)
                        return true;

                    if (compilation.HasImplicitConversion(n.Symbol, TypeData.SyntaxTokenSymbol))
                        return false;

                    return true;
                }
            )
           .ToList();

        w.Using(TypeData.SyntaxNodeSymbol);
        w.Using(visitorNodes.Select(n => n.Symbol));

        w.Imports(
            [
                "System",
                "System.Collections.Generic",
            ]
        );

        w.Namespace("CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes");

        foreach (var node in visitorNodes) {
            w.Class(
                node.Symbol, c => {
                    c.IsPartial = true;
                    c.Name      = node.SymbolNameWithTypeParams;


                    c.Method(
                        "Accept",
                        m => {
                            m.Accessibility = Accessibility.Public;
                            m.IsOverride    = true;

                            m.Parameter("visitor", "ISyntaxNodeVisitor", p => { });
                            m.Body.WriteLine($"visitor.{node.VisitorMethodName}(this);");
                        }
                    );


                }
            );
        }


        var output = w.ToString();
        ctx.AddSource("NodeVisitorExtensions.g.cs", output);

    }


}