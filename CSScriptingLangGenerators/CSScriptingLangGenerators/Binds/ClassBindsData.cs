using System.Collections.Immutable;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Binds;

public class ClassBindsData
{
    public ClassWriter w { get; set; } = new();

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

    public Compilation Compilation { get; }

    public string Qualifier { get; set; }
    
    public ClassBindsData(GeneratorValuesContext ctx, Compilation compilation) {
        Compilation = compilation;

        TargetNode    = ctx.TargetNode;
        TargetSymbol  = ctx.TargetSymbol as INamedTypeSymbol;
        SemanticModel = ctx.SemanticModel;
        Attributes    = ctx.Attributes;

        Qualifier = $"global::{TargetSymbol.GetFullyQualifiedName()}";
    }
}