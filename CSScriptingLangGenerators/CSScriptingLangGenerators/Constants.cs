using System.Collections.Generic;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators;

internal static class Constants
{
    public const string RootNamespace       = "CSScriptingLang";
    public const string AssemblyName        = "CSScriptingLang";
    public const string GeneratorName       = "CSScriptingLangGenerator";
    public const string ScriptFileExtension = ".vlt";

    public const string BindingsNamespace  = $"{RootNamespace}.Interpreter.Bindings";
    public const string TokensNamespace    = $"{RootNamespace}.Lexing";
    public const string ASTNamespace       = $"{RootNamespace}.Parsing.AST";
    public const string ExecutionNamespace = $"{RootNamespace}.Interpreter.Execution";
    public const string MixinsNamespace    = $"{RootNamespace}.Mixins";

    public static string Namespace(params string[] parts) => new[] {RootNamespace}.Concat(parts).Join(".");

    public static IEnumerable<string> AllNamespaces => [
        BindingsNamespace,
        TokensNamespace,
        ASTNamespace,
        ExecutionNamespace,
        MixinsNamespace,
    ];

    public static bool IsNodeNamespace(INamespaceSymbol ns)
        => ns.ToDisplayString().Contains(ASTNamespace) ||
           ns.ToDisplayString().Contains(ExecutionNamespace);
}

internal struct TypeName
{
    public string Namespace;
    public string Name;

    public string FullyQualifiedName => $"{Namespace}.{Name}";

    public TypeName(string ns, string name) {
        Namespace = ns;
        Name      = name;
    }

    public static implicit operator string(TypeName typeName) => typeName.Name;

    public static TypeName Bind(string name)            => new(Constants.BindingsNamespace, name);
    public static TypeName Bind(string name, string ns) => new(ns, name);
}

internal static class Attributes
{
    public static TypeName Class                = TypeName.Bind("LanguageClassBind");
    public static TypeName ClassDataObject      = TypeName.Bind("LanguageClassDataObjectBind");
    public static TypeName ClassWrappableObject = TypeName.Bind("LanguageClassWrappableObjectBind");
    public static TypeName Prototype            = TypeName.Bind("LanguagePrototypeAttribute");
    public static TypeName Module               = TypeName.Bind("LanguageModuleBindAttribute");
    // Links x Class to X Module
    public static TypeName BindToModule      = TypeName.Bind("LanguageBindToModuleAttribute");
    public static TypeName Function          = TypeName.Bind("LanguageFunctionAttribute");
    public static TypeName IgnoreParamChecks = TypeName.Bind("LanguageFunctionDisableParameterChecksAttribute");
    public static TypeName GlobalFunction    = TypeName.Bind("LanguageGlobalFunctionAttribute");
    public static TypeName Operator          = TypeName.Bind("LanguageOperatorAttribute");
    public static TypeName Constructor       = TypeName.Bind("LanguageValueConstructorAttribute");
    public static TypeName Parameter         = TypeName.Bind("LanguageParameterAttribute");
    public static TypeName InstanceParameter = TypeName.Bind("LanguageInstanceAttribute");
    // Used in prototype objects to register a field like getter function
    public static TypeName InstanceGetterFunction = TypeName.Bind("LanguageInstanceGetterFunctionAttribute");
    public static TypeName PropertyGetter         = TypeName.Bind("LanguagePropertyGetterAttribute");
    public static TypeName PropertySetter         = TypeName.Bind("LanguagePropertySetterAttribute");

    // Meta generation attributes:
    public static TypeName MetaDefinition       = TypeName.Bind("LanguageMetaDefinition");
    public static TypeName LanguageBindTypeHint = TypeName.Bind("LanguageBindTypeHint");


    public static TypeName SyntaxNode = TypeName.Bind("SyntaxNodeAttribute", "CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes");
}