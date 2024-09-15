using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public static class Diagnostics
{
    private const string Category = "CSScriptingLang";

    public static readonly DiagnosticDescriptor MissingSyntaxReceiver = new(
        "CSSL00",
        "Internal error - syntax receiver is null",
        "The syntax receiver was not set or is an unexpected type - cannot generate bindings",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingOperandType = new(
        "CSSL01",
        "Missing operand type",
        "Missing operand type",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor MissingInstructionHandlerMethod = new(
        "CSSL02",
        "Missing instruction handler method",
        "[{0}] Missing handler method: private void On{1}({0} inst)",
        Category,
        DiagnosticSeverity.Warning,
        true
    );


    public static readonly DiagnosticDescriptor BaseValueNotFound = new(
        "CSSL03",
        "Value symbol type not found",
        "The BaseValue type was not found in the compilation",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor BoundClassesMustBePartial = new(
        "CSSL04",
        "Bound classes must be partial",
        "Bound classes must be partial",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor CannotBindGeneric = new(
        "CSSL05",
        "Cannot bind generic classes",
        "Cannot bind generic classes",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor ClassesCannotBeStatic = new(
        "CSSL06",
        "Classes cannot be static",
        "Classes cannot be static",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor BoundMembersMustBePublic = new(
        "CSSL07",
        "Bound members must be public",
        "Bound members must be public",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    
    public static readonly DiagnosticDescriptor BoundMethodsCannotBeFunctionAndOperator = new(
        "CSSL08",
        "Bound methods cannot be functions and operators",
        "Bound methods cannot be functions and operators",
        Category,
        DiagnosticSeverity.Error,
        true
    );

    public static readonly DiagnosticDescriptor UnsupportedParameterType = new(
        "CSSL09",
        "Unsupported parameter type",
        "The method parameter type `{0}` is not supported for binding",
        Category,
        DiagnosticSeverity.Error,
        true
    ); 
        
    public static readonly DiagnosticDescriptor BoundMethodOverloadConflicts = new(
        "CSSL10",
        "Bound method overload conflicts",
        "The method `{0}` has parameters which map to the same types as other overloads. Method would not be callable.",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor CannotConvertToValue = new(
        "CSSL11",
        "Cannot convert type to Value",
        "The type `{0}` cannot be automatically converted to a Value in generated bindings",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor CannotConvertFromValue = new(
        "CSSL12",
        "Cannot convert type from Value",
        "The type `{0}` cannot be automatically converted from a Value in generated bindings",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor ErrorGeneratingBindings = new(
        "CSSL13",
        "Error generating bindings",
        "An error occurred while generating bindings: {0}",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    public static readonly DiagnosticDescriptor FailedToInitialize = new(
        "CSSL14",
        "Error initializing bindings",
        "Error while initializing bindings: {0}",
        Category,
        DiagnosticSeverity.Error,
        true
    );
    //  public static readonly DiagnosticDescriptor 
}