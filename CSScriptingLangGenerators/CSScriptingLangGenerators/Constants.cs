using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators;

internal static class Constants
{
    public const string RootNamespace       = "CSScriptingLang";
    public const string AssemblyName        = "CSScriptingLang";
    public const string GeneratorName       = "CSScriptingLangGenerator";
    public const string ScriptFileExtension = ".js";

    public const string BindingsNamespace  = $"{RootNamespace}.Interpreter.Bindings";
    public const string TokensNamespace    = $"{RootNamespace}.Lexing";
    public const string ASTNamespace       = $"{RootNamespace}.Parsing.AST";
    public const string ExecutionNamespace = $"{RootNamespace}.Interpreter.Execution";

    public static IEnumerable<string> AllNamespaces => [
        BindingsNamespace,
        TokensNamespace,
        ASTNamespace,
        ExecutionNamespace,
    ];

    public static bool IsNodeNamespace(INamespaceSymbol ns)
        => ns.ToDisplayString().Contains(ASTNamespace) ||
           ns.ToDisplayString().Contains(ExecutionNamespace);
}

internal static class Attributes
{
    public const string Class             = "LanguageClassBind";
    public const string Prototype         = "LanguagePrototypeAttribute";
    public const string Module            = "LanguageModuleBindAttribute";
    public const string Function          = "LanguageFunctionAttribute";
    public const string GlobalFunction    = "LanguageGlobalFunctionAttribute";
    public const string Operator          = "LanguageOperatorAttribute";
    public const string Constructor       = "LanguageValueConstructorAttribute";
    public const string Parameter         = "LanguageParameterAttribute";
    public const string InstanceParameter = "LanguageInstanceAttribute";
    // Used in prototype objects to register a field like getter function
    public const string InstanceGetterFunction = "LanguageInstanceGetterFunctionAttribute";
    public const string PropertyGetter         = "LanguagePropertyGetterAttribute";
    public const string PropertySetter         = "LanguagePropertySetterAttribute";
}