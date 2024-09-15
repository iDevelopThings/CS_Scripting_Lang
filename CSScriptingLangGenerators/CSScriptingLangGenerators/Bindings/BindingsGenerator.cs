using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

[Generator]
public partial class BindingsGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context) {
        // Util.WaitForDebugger();

        context.RegisterForSyntaxNotifications(() => new BindingsSyntaxReceiver());
    }

    public void Execute(GeneratorExecutionContext context) {

        if (context.SyntaxContextReceiver is not BindingsSyntaxReceiver syntaxReceiver) {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingSyntaxReceiver, Location.None));
            return;
        }

        try {
            if (!TypeData.Initialize(context)) {
                return;
            }

            foreach (var location in syntaxReceiver.MissingPartials) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundClassesMustBePartial, location));
            }

            foreach (var prototype in syntaxReceiver.Prototypes) {
                if (prototype.Arity != 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, prototype.Locations.First()));
                    continue;
                }

                context.AddSource($"{prototype.GetFullyQualifiedName()}.Prototype.g.cs", GenerateWith(context, prototype, PrototypeBindings));
            }

            foreach (var module in syntaxReceiver.Modules) {
                if (module.Arity != 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, module.Locations.First()));
                    continue;
                }

                context.AddSource($"{module.GetFullyQualifiedName()}.Module.g.cs", GenerateWith(context, module, ModuleBindings));
            }

            foreach (var klass in syntaxReceiver.Classes) {
                if (klass.Arity != 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, klass.Locations.First()));
                    continue;
                }

                if (klass.IsStatic) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ClassesCannotBeStatic, klass.Locations.First()));
                    continue;
                }

                context.AddSource($"{klass.GetFullyQualifiedName()}.Class.g.cs", GenerateWith(context, klass, ClassBindings));
            }
        }
        catch (Exception e) {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ErrorGeneratingBindings, Location.None, e.ToString()));

            GeneratorLogger.Log(e.ToString());
        }

    }

    private static void CallMethod(GeneratorExecutionContext context, Writer writer, string qualifier, Method method, int argCount = 10000) {
        var isConstructor = method.Info.MethodKind == MethodKind.Constructor;

        var returnType = isConstructor
            ? method.Info.ContainingType
            : method.Info.ReturnType;

        var hasReturn = !SymbolEqualityComparer.Default.Equals(returnType, TypeData.Void);

        qualifier = method.Info.IsStatic ? method.Info.ContainingType.GetFullyQualifiedName() : qualifier;

        writer._(
            $"// Has return? {hasReturn}",
            $"// Method: {method.Info.Name}",
            $"// ReturnType: {returnType.GetFullyQualifiedName()}",
            $"// IsConstructor: {isConstructor}",
            $"// Args: "
        );

        writer._(
            method.Parameters.Select(p => {
                return $"// - {p.Info.Name}: (RequiresTypeCheck: {p.RequiresParamTypeCheck()}) {p.Types.Select(t => t.ToString()).Join(", ")}";
            })
        );

        if (isConstructor) {
            var argBindings = method.GetArgumentBindings(context, argCount, ParameterType.Params)
               .Select(b => $"{writer.GetIndentString()}{b}")
               .Join(", \n");

            writer._(
                //  $"var result = new {method.Info.ContainingType.GetFullyQualifiedName()}(",
                //  method.GetArgumentBindings(context, argCount).Select(b => $"{writer.GetIndentString()}{b}").Join(", \n"),
                //  ");",
                $"return {ConvertToValue(context, argBindings, returnType, method.Info)};"
            );

            return;
        }

        if (hasReturn) {
            writer._(
                $"var result = {qualifier}.{method.Info.Name}(",
                method.GetArgumentBindings(context, argCount).Select(b => $"{writer.GetIndentString()}{b}").Join(", \n"),
                ");",
                $"return {ConvertToValue(context, "result", returnType, method.Info)};"
            );

            return;
        }

        writer._(
            $"{qualifier}.{method.Info.Name}(",
            method.GetArgumentBindings(context, argCount).Select(b => $"{writer.GetIndentString()}{b}").Join(", \n"),
            ");"
        );
        writer._("return Value.Null();");
    }

    private static string BindArguments(GeneratorExecutionContext context, Method method, int argCount) {
        var valueIdx = 0;
        var args     = new List<string>();
        foreach (var param in method.Parameters) {
            if (valueIdx >= argCount && param.Type == ParameterType.Value) {
                continue;
            }

            args.Add(BindArgument(context, valueIdx, param));

            if (param.Type == ParameterType.Value) {
                valueIdx++;
            }
        }

        return args.Join(", \n");
    }

    private static string BindArgument(GeneratorExecutionContext context, int i, Parameter parameter) {
        return parameter.Type switch {
            ParameterType.Unsupported => $"default /* unsupported type {parameter.Info.Type.GetFullyQualifiedName()} */",
            ParameterType.Value       => ConvertFromValue(context, i, parameter.Info.Type, parameter.Info),
            ParameterType.Params      => $"args[{i}..]",
            ParameterType.ExecCtx     => "ctx",
            ParameterType.FnExecCtx   => "ctx",
            ParameterType.Instance    => "instance",
            _                         => throw new NotSupportedException($"{nameof(BindArgument)} {nameof(ParameterType)} {parameter.Type}"),
        };
    }

    public static string ConvertFromValue(GeneratorExecutionContext context, int i, ITypeSymbol type, ISymbol typeSource) {
        var input = $"args[{i}]";
        switch (type.SpecialType) {
            case SpecialType.System_Double:
                return $"(double){input}";
            case SpecialType.System_Single:
                return $"(float){input}";
            case SpecialType.System_Int32:
                return $"(int){input}";
            case SpecialType.System_UInt32:
                return $"(uint){input}";
            case SpecialType.System_Int16:
                return $"(short){input}";
            case SpecialType.System_UInt16:
                return $"(ushort){input}";
            case SpecialType.System_SByte:
                return $"(sbyte){input}";
            case SpecialType.System_Byte:
                return $"(byte){input}";
            case SpecialType.System_String:
                return $"(string){input}";
            case SpecialType.System_Boolean:
                return $"(bool){input}";
            default:
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Value)) {
                    return input;
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.RTVT)) {
                    // Convert from string to RTVT enum
                    return $"Enum.Parse<RTVT>((string){input}, true)";
                }

                var typeName = type.GetFullyQualifiedName();

                if (SymbolEqualityComparer.Default.Equals(type, TypeData.ValueList) || typeName == "List") {
                    return $"(List<Value>){input}";
                }

                /*if (SymbolEqualityComparer.Default.Equals(type, TypeData.MondValueNullable))
                {
                    return $"({input} == MondValue.Undefined ? null : (MondValue?){input})";
                }*/

                if (type.TryGetAttribute(Attributes.Class, out var attr)) {
                    var name = attr.GetArgument<string>() ?? type.Name;
                    return $"({input} as global::{type.GetFullyQualifiedName()} ?? throw new InterpreterRuntimeException(\"Unable to convert argument {i} to {name}\"))";
                }

                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotConvertFromValue, typeSource.Locations.First(), typeName));
                return $"default /* cannot convert Value -> {typeName} */";
        }
    }

    public static string ConvertToValue(GeneratorExecutionContext context, string input, ITypeSymbol type, ISymbol typeSource) {
        switch (type.SpecialType) {

            case SpecialType.System_Double:
                return $"Value.Double({input})";
            case SpecialType.System_Single:
                return $"Value.Float({input})";
            case SpecialType.System_Int64:
                return $"Value.Int64({input})";
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
                return $"Value.Int32({input})";
            case SpecialType.System_SByte:
                throw new NotSupportedException("SByte is not supported");
            case SpecialType.System_Byte:
                throw new NotSupportedException("Byte is not supported");
            case SpecialType.System_String:
                return $"Value.String({input})";
            case SpecialType.System_Boolean:
                return $"Value.Boolean({input})";

            default:
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Value)) {
                    return input;
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Prototype)) {
                    return $"{input}.GetPrototype()";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.RTVT)) {
                    return $"Value.String({input}.ToString())";
                }

                //if (SymbolEqualityComparer.Default.Equals(type, TypeData.MondValueNullable)) {
                //    return $"({input} ?? MondValue.Undefined)";
                //}

                if (type.HasAttribute(Attributes.Class)) {
                    return $"Value.ClassInstance(" +
                           $"ctx, " +
                           $"\"{type.GetFullyQualifiedName()}\"" +
                           (input == "" ? "" : $", {input}") +
                           $")";
                }

                var typeName = type.GetFullyQualifiedName();
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotConvertToValue, typeSource.Locations.First(), typeName));
                return $"Value.Null() /* cannot convert {typeName} -> Value */";
        }
    }


    private static string CompareArgument(int i, Parameter p) {
        var isAny = p.Types == null ||
                    p.Types.Count == 0 ||
                    (p.Types.Count == 1 && p.Types[0] == ValueType.Unit);

        var comparer = isAny
            ? $"(true /* args[{i}] is any */)"
            : "(" + string.Join(" || ", p.Types.Select(t => $"args[{i}].Type == RTVT.{t}")) + ")";

        return p.IsOptional
            ? $"(args.Length > {i} && {comparer})"
            : comparer;
    }

    private static string GetMethodNotMatchedErrorMessage(string prefix, MethodData methodTable) {
        var sb = new StringBuilder();

        sb.Append(prefix);
        sb.AppendLine("argument types do not match any available functions");

        var methods = methodTable.Methods
           .SelectMany(l => l)
           .Concat(methodTable.ParamsMethods)
           .Distinct();

        foreach (var method in methods) {
            sb.Append("- ");
            sb.AppendLine(method.ToString());
        }

        return sb.ToString().Trim();
    }

    private static string CompareArguments(Method method, out bool isTrueResult, int limit = 10000) {
        var argComparers = method.Parameters
           .Take(limit)
           .Where(p => p.Type == ParameterType.Value)
           .Select((p, i) => CompareArgument(i, p))
           .ToList();

        isTrueResult = argComparers.Count == 0;

        if (argComparers.Count > 0) {
            return string.Join(" && ", argComparers);
        }

        return "true /* no arguments */";
    }

    private static string CompareArguments(Method method, int limit = 10000) {
        return CompareArguments(method, out _, limit);
    }

    private static string EscapeForStringLiteral(string str) {
        return str
           .Replace("\r", "")
           .Replace(@"\", @"\\")
           .Replace("\n", @"\n");
    }

    
    private static List<(IMethodSymbol Method, string Name, string Identifier)> GetMethods(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null) {
        var result = new List<(IMethodSymbol, string, string)>();

        foreach (var member in klass.GetMembers()) {
            if (member is not IMethodSymbol {MethodKind: MethodKind.Ordinary} method || (isStatic != null && method.IsStatic != isStatic)) {
                continue;
            }

            var attributes  = method.GetAttributes();
            var hasFuncAttr = attributes.TryGetAttribute([Attributes.Function, Attributes.GlobalFunction], out var funcAttr);
            var hasOpAttr   = attributes.TryGetAttribute(Attributes.Operator, out var opAttr);

            if (!hasFuncAttr && !hasOpAttr) {
                continue;
            }

            if (hasFuncAttr && hasOpAttr) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMethodsCannotBeFunctionAndOperator, method.Locations.First()));
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            string name = null;
            if (hasFuncAttr) {
                name = (funcAttr.GetArgument<string>() ?? method.Name).ToCamelCase();
                result.Add((method, name, name));
                continue;
            }

            name = opAttr.GetArgument<string>();
            result.Add((method, name, $"operator_{TypeData.GetOperatorIdentifier(name)}"));

        }

        return result;
    }

    public class PropertyBind
    {
        public string Name { get; set; }

        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public IPropertySymbol Property { get; set; }

        public ITypeSymbol Type => Property.Type;

        public IMethodSymbol GetMethod => HasGetter && Property.DeclaredAccessibility == Accessibility.Public ? Property.GetMethod : null;
        public IMethodSymbol SetMethod => HasSetter && Property.DeclaredAccessibility == Accessibility.Public ? Property.SetMethod : null;
    }

    private static IEnumerable<PropertyBind> GetProperties(GeneratorExecutionContext context, INamedTypeSymbol klass, bool? isStatic = null) {

        foreach (var member in klass.GetMembers()) {
            if (member is not IPropertySymbol property || (isStatic != null && property.IsStatic != isStatic)) {
                continue;
            }

            var name      = property.Name;
            var hasGetter = false;
            var hasSetter = false;

            if (property.TryGetAttribute([Attributes.Function, Attributes.GlobalFunction], out var attr)) {
                hasGetter = true;
                hasSetter = true;
                name      = attr.GetArgument<string>() ?? name;
            }


            if (!hasGetter && property.TryGetAttribute([Attributes.PropertyGetter], out var getterAttr)) {
                hasGetter = true;
                name      = getterAttr.GetArgument<string>() ?? name;
            }

            if (!hasSetter && property.TryGetAttribute([Attributes.PropertySetter], out var setterAttr)) {
                hasSetter = true;
                name      = setterAttr.GetArgument<string>() ?? name;
            }

            if (!hasGetter && !hasSetter) {
                continue;
            }

            if (property.DeclaredAccessibility != Accessibility.Public) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, property.Locations.First()));
                continue;
            }

            var p = new PropertyBind {
                Name      = name,
                HasGetter = hasGetter,
                HasSetter = hasSetter,
                Property  = property
            };

            yield return p;
        }

    }

    private static List<IMethodSymbol> GetConstructors(GeneratorExecutionContext context, INamedTypeSymbol klass) {
        var result = new List<IMethodSymbol>();
        foreach (var member in klass.GetMembers()) {
            if (member is not IMethodSymbol {MethodKind: MethodKind.Constructor} method) {
                continue;
            }

            if (!method.HasAttribute(Attributes.Constructor)) {
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            result.Add(method);
        }

        return result;
    }

    private delegate void GeneratorAction(GeneratorExecutionContext context, INamedTypeSymbol symbol, Writer writer);

    private static string GenerateWith(GeneratorExecutionContext context, INamedTypeSymbol symbol, GeneratorAction generator) {
        var writer = new Writer();

        writer.AddGeneratedHeader();
        writer.WithImports(
            "System",
            "System.Collections.Generic",
            "CSScriptingLang.RuntimeValues",
            "CSScriptingLang.RuntimeValues.Values",
            "CSScriptingLang.Interpreter.Context",
            "CSScriptingLang.RuntimeValues.Types",
            "CSScriptingLang.Interpreter.Libraries",
            "CSScriptingLang.Lexing"
        );


        var ns = symbol.GetFullNamespace();
        if (ns != null) {
            writer.WithNamespace(ns);
        }

        writer.AddHeaderLine("#pragma warning disable CS0162 // Unreachable code detected");

        var parents = symbol.GetParentTypes();
        for (var i = parents.Count - 1; i >= 0; i--) {
            writer._($"public partial class {parents[i].Name}");
            writer.OpenBracket();
        }

        writer._($"public {(symbol.IsStatic ? "static" : "")} partial class {symbol.Name}");
        writer.OpenBracket();

        generator(context, symbol, writer);

        writer.CloseBracket();

        for (var i = 0; i < parents.Count; i++) {
            writer.CloseBracket();
        }

        return writer.ToString();
    }
}