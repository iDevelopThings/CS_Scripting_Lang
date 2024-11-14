using System;
using System.Collections.Generic;
using System.Linq;
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
            if (!TypeData.Initialize(context.Compilation, context.ReportDiagnostic)) {
                return;
            }

            TypeData.WrappableClasses = syntaxReceiver.WrappableClasses;

            foreach (var location in syntaxReceiver.MissingPartials) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundClassesMustBePartial, location));
            }

            var hasErrors = false;
            foreach (var (symbol, type) in syntaxReceiver.AllTypes) {
                if (symbol.Arity != 0) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotBindGeneric, symbol.Locations.First()));
                    hasErrors = true;
                }
                if (type == BindingsSyntaxReceiver.SymbolType.Class && symbol.IsStatic) {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.ClassesCannotBeStatic, symbol.Locations.First()));
                    hasErrors = true;
                }
            }

            if (hasErrors) {
                return;
            }

            TypeData.Modules = syntaxReceiver.Modules
               .Select(m => ModuleTypeData.ForModuleBinding(m, context, new Writer()))
               .ToHashSet();
            TypeData.AllTypeMeta.AddRange(TypeData.Modules.Select(m => m.Meta).ToList());

            TypeData.Classes = syntaxReceiver.Classes
               .Select(c => ClassTypeData.ForClassBinding(c, context, new Writer()))
               .ToHashSet();
            TypeData.AllTypeMeta.AddRange(TypeData.Classes.Select(c => c.Meta).ToList());

            TypeData.Prototypes = syntaxReceiver.Prototypes
               .Select(c => PrototypeTypeData.ForPrototypeBinding(c, context, new Writer()))
               .ToHashSet();
            TypeData.AllTypeMeta.AddRange(TypeData.Prototypes.Select(c => c.Meta).ToList());

            var allClassesAndProtos = TypeData.AllTypeMeta
               .Where(m => {
                    if (m.Kind is not (TypeMetaKind.Class or TypeMetaKind.Prototype))
                        return false;
                    if (string.IsNullOrEmpty(m.Module))
                        return false;
                    return true;
                }).ToList();

            foreach (var meta in allClassesAndProtos) {
                TypeMeta_Module module = TypeData.Modules
                   .Select(m => m.Meta as TypeMeta_Module)
                   .Concat(TypeData.AdditionalModuleTypeMeta)
                   .FirstOrDefault(m => m!.Name == meta.Module);

                if (module == null) {
                    module = new TypeMeta_Module() {
                        Name = meta.Module,
                        Kind = TypeMetaKind.Module,
                    };
                    TypeData.AdditionalModuleTypeMeta.Add(module);
                    TypeData.AllTypeMeta.Add(module);
                }
            }

            var allModulesMeta = TypeData.Modules
               .Select(m => m.Meta as TypeMeta_Module)
               .Concat(TypeData.AdditionalModuleTypeMeta)
               .ToList();

            var toRemove = new HashSet<TypeMeta_ClassBased>();
            foreach (var typeMeta in TypeData.AllTypeMeta) {
                if (typeMeta.Kind == TypeMetaKind.Module) {
                    continue;
                }
                if (string.IsNullOrEmpty(typeMeta.Module)) {
                    continue;
                }

                TypeMeta_Module module = allModulesMeta.FirstOrDefault(m => m!.Name == typeMeta.Module);

                if (module == null) {
                    throw new Exception($"Module '{typeMeta.Module}' not found for class '{typeMeta.Name}', available modules: {TypeData.Modules.Select(m => m.Meta.Name).Join(", ")}");
                }

                if (typeMeta.Kind == TypeMetaKind.Class) {
                    module.Classes.Add(typeMeta);
                    toRemove.Add(typeMeta);
                }
                if (typeMeta.Kind == TypeMetaKind.Prototype) {
                    module.Prototypes.Add(typeMeta);
                    toRemove.Add(typeMeta);
                }
            }

            foreach (var meta in toRemove) {
                TypeData.AllTypeMeta.Remove(meta);
            }

            foreach (var module in TypeData.Modules) {
                context.AddSource(
                    $"{module.GetFullyQualifiedName()}.Module.g.cs",
                    GenerateWith(
                        context, module.Symbol, module,
                        (executionContext, symbol, data) => ModuleBindings(executionContext, symbol, (ModuleTypeData) data)
                    )
                );
            }

            foreach (var klass in TypeData.Classes) {
                context.AddSource(
                    $"{klass.GetFullyQualifiedName()}.Class.g.cs",
                    GenerateWith(context, klass.Symbol, klass, ClassBindings)
                );
            }

            foreach (var module in TypeData.Prototypes) {
                context.AddSource(
                    $"{module.GetFullyQualifiedName()}.Prototype.g.cs",
                    GenerateWith(
                        context, module.Symbol, module,
                        (executionContext, symbol, data) => PrototypeBindings(executionContext, symbol, (PrototypeTypeData) data)
                        // (executionContext, symbol, data) => PrototypeBindingsOld(executionContext, symbol, (PrototypeTypeData) data)
                    )
                );
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

                /*if (SymbolEqualityComparer.Default.Equals(type, TypeData.MondValueNullable)) {
                    return $"({input} == MondValue.Undefined ? null : (MondValue?){input})";
                }*/

                if (TypeData.WrappableClasses.Contains(type, SymbolEqualityComparer.Default)) {
                    return $"(" +
                           $"({input} as WrappedValue<{typeName}>)?.Value " +
                           $"?? throw new InterpreterRuntimeException(\"Unable to convert argument {i} to {typeName}\")" +
                           $")";
                    // return $"({input} as {typeName} ?? throw new InterpreterRuntimeException(\"Unable to convert argument {i} to {typeName}\"))";
                }

                if (type.TryGetAttribute(Attributes.Class, out var attr)) {
                    var name = attr.GetArgument<string>() ?? type.Name;
                    return $"({input} as global::{type.GetFullyQualifiedName()} ?? throw new InterpreterRuntimeException(\"Unable to convert argument {i} to {name}\"))";
                }

                if (typeSource.GetAttributeArgument(Attributes.Parameter, false, 1)) {
                    return input;
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
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.ValueArray)) {
                    return $"Value.Array((IEnumerable<Value>){input})";
                }
                if (TypeData.WrappableClasses.Contains(type, SymbolEqualityComparer.Default)) {
                    return $"Value.Wrapped(ctx, {input})";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Prototype)) {
                    return $"{input}.GetPrototype()";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.RTVT)) {
                    return $"Value.String({input}.ToString())";
                }

                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Task)) {
                    return $"ScriptTask.Wrap(ctx, {input});";
                }

                if (type is INamedTypeSymbol {Arity: 1} namedType && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, TypeData.TaskOfT)) {
                    var returnType    = namedType.TypeArguments[0];
                    var returnWrapper = ConvertToValue(context, "t.Result", returnType, typeSource);
                    return $"ScriptTask.Wrap(ctx, {input}.ContinueWith(t => t.IsFaulted ? AsyncContext.RethrowAsyncException(t.Exception) : {returnWrapper}));";
                }

                //if (SymbolEqualityComparer.Default.Equals(type, TypeData.MondValueNullable)) {
                //    return $"({input} ?? MondValue.Undefined)";
                //}

                if (type.HasAttribute(Attributes.ClassDataObject)) {
                    if (type.HasAttribute(Attributes.ClassDataObject)) {
                        return $"Value.ClassInstance(" +
                               $"ctx, {input}, " +
                               $"\"{type.GetFullyQualifiedName()}\"" +
                               $")";
                    }

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
    public static string ConvertToType(GeneratorExecutionContext context, string input, ITypeSymbol type, ISymbol typeSource = null) {
        if (typeSource is IMethodSymbol method) {
            return $"Ty.Function(\"{input}\", {ConvertToType(context, input, type, null)})";
        }
        switch (type.SpecialType) {

            case SpecialType.System_Double:
                return $"Ty.Double()";
            case SpecialType.System_Single:
                return $"Ty.Float()";
            case SpecialType.System_Int64:
                return $"Ty.Int64()";
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
                return $"Ty.Int32()";
            case SpecialType.System_SByte:
                throw new NotSupportedException("SByte is not supported");
            case SpecialType.System_Byte:
                throw new NotSupportedException("Byte is not supported");
            case SpecialType.System_String:
                return $"Ty.String()";
            case SpecialType.System_Boolean:
                return $"Ty.Boolean()";
            case SpecialType.System_Void:
                return $"Ty.Unit()";

            default:
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Value)) {
                    return $"Ty.Object()";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.ValueArray)) {
                    return $"Ty.Array({ConvertToType(context, input, ((IArrayTypeSymbol)type).ElementType)})";
                }
                if (TypeData.WrappableClasses.Contains(type, SymbolEqualityComparer.Default)) {
                    return $"Ty.Wrapped(ctx, {input})";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Prototype)) {
                    return $"{input}.GetPrototype()";
                }
                if (SymbolEqualityComparer.Default.Equals(type, TypeData.RTVT)) {
                    return $"new Ty({input})";
                }

                if (SymbolEqualityComparer.Default.Equals(type, TypeData.Task)) {
                    return $"ScriptTask.Wrap(ctx, {input});";
                }

                // if (type is INamedTypeSymbol {Arity: 1} namedType && SymbolEqualityComparer.Default.Equals(namedType.ConstructedFrom, TypeData.TaskOfT)) {
                    // var returnType    = namedType.TypeArguments[0];
                    // var returnWrapper = ConvertToValue(context, "t.Result", returnType, typeSource);
                    // return $"ScriptTask.Wrap(ctx, {input}.ContinueWith(t => t.IsFaulted ? AsyncContext.RethrowAsyncException(t.Exception) : {returnWrapper}));";
                // }

                if (type.HasAttribute(Attributes.ClassDataObject)) {
                    if (type.HasAttribute(Attributes.ClassDataObject)) {
                        return $"Ty.ClassInstance(" +
                               $"ctx, {input}, " +
                               $"\"{type.GetFullyQualifiedName()}\"" +
                               $")";
                    }

                    return $"Ty.ClassInstance(" +
                           $"ctx, " +
                           $"\"{type.GetFullyQualifiedName()}\"" +
                           (input == "" ? "" : $", {input}") +
                           $")";
                }

                var typeName = type.GetFullyQualifiedName();
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.CannotConvertToValue, typeSource?.Locations.First(), typeName));
                return $"Ty.Null() /* cannot convert {typeName} -> Value */";
        }
    }

    public static ValueTypeHint ConvertToValueTypeHint(ITypeSymbol type, ISymbol typeSource) {
        switch (type.SpecialType) {

            case SpecialType.System_Double:
                return new ValueTypeHint("double", TypeData.PrototypeMap["DoublePrototype"]);
            case SpecialType.System_Single:
                return new ValueTypeHint("float", TypeData.PrototypeMap["FloatPrototype"]);
            case SpecialType.System_Int64:
                return new ValueTypeHint("int64", TypeData.PrototypeMap["Int64Prototype"]);
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
                return new ValueTypeHint("int32", TypeData.PrototypeMap["Int32Prototype"]);
            case SpecialType.System_SByte:
                throw new NotSupportedException("SByte is not supported");
            case SpecialType.System_Byte:
                throw new NotSupportedException("Byte is not supported");
            case SpecialType.System_String:
                return new ValueTypeHint("string", TypeData.PrototypeMap["StringPrototype"]);
            case SpecialType.System_Boolean:
                return new ValueTypeHint("boolean", TypeData.PrototypeMap["BooleanPrototype"]);

            case SpecialType.System_Void:
                return new ValueTypeHint("void", TypeData.PrototypeMap["UnitPrototype"]);

            default:
                return new ValueTypeHint(
                    "object",
                    TypeData.PrototypeMap["ObjectPrototype"],
                    type
                );
        }
    }


    private static string CompareArgument(int i, Parameter p) {
        var isAny = p.Types == null ||
                    p.Types.Count == 0 ||
                    (p.Types.Count == 1 && p.Types[0] == ValueType.Unit);

        var comparer = "";
        if (isAny) {
            comparer = $"(true /* args[{i}] is any */)";
        } else {
            var comparableTypes = p.Types
                   .Where(t => t != ValueType.Unit && t != ValueType.WrappedValue)
                   .Select(t => $"args[{i}].Type == RTVT.{t}")
                ;
            comparer = $"({comparableTypes.Join(" || ")})";
        }

        // var comparer = isAny
        //     ? $"(true /* args[{i}] is any */)"
        //     : "(" + string.Join(" || ", p.Types.Select(t => $"args[{i}].Type == RTVT.{t}")) + ")";

        return p.IsOptional
            ? $"(args.Length > {i} && {comparer})"
            : comparer;
    }

    public static string GetMethodNotMatchedErrorMessage(string prefix, MethodData methodTable) {
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

    public static string EscapeForStringLiteral(string str) {
        return str
           .Replace("\r", "")
           .Replace(@"\", @"\\")
           .Replace("\n", @"\n");
    }


    private static List<(IMethodSymbol Method, string Name, string Identifier)> GetMethods(
        GeneratorExecutionContext context,
        INamedTypeSymbol          klass,
        bool?                     isStatic = null
    ) {
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
                name = (funcAttr.GetArgument<string>() ?? method.Name).ToIdentifierCasing();
                result.Add((method, name, name));
                continue;
            }

            name = opAttr.GetArgument<string>();
            result.Add((method, name, $"operator_{TypeData.GetOperatorIdentifier(name)}"));

        }

        return result;
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

            var p = new PropertyBind(
                property,
                context.Compilation
            ) {
                Name      = name,
                HasGetter = hasGetter,
                HasSetter = hasSetter,
                Property  = property
            };

            yield return p;
        }

    }

    private static List<IMethodSymbol> GetConstructors(
        GeneratorExecutionContext context,
        INamedTypeSymbol          klass,
        bool                      allowMethods = false
    ) {
        var result = new List<IMethodSymbol>();
        foreach (var member in klass.GetMembers()) {
            if (member is not IMethodSymbol method) {
                continue;
            }
            if (!(
                    method.MethodKind == MethodKind.Constructor ||
                    (allowMethods && method.MethodKind == MethodKind.Ordinary)
                )) {
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

    private delegate void GeneratorAction(
        GeneratorExecutionContext context,
        INamedTypeSymbol          symbol,
        ClassTypeData             typeData
    );

    private static string GenerateWith(
        GeneratorExecutionContext context,
        INamedTypeSymbol          symbol,
        ClassTypeData             typeData,
        GeneratorAction           generator
    ) {
        typeData.AddGeneratedHeader();
        typeData.WithImports(
            "System",
            "System.Collections.Generic",
            $"{Constants.RootNamespace}.RuntimeValues",
            $"{Constants.RootNamespace}.RuntimeValues.Values",
            $"{Constants.RootNamespace}.RuntimeValues.Prototypes.Types",
            $"{Constants.RootNamespace}.Interpreter.Context",
            $"{Constants.RootNamespace}.RuntimeValues.Types",
            $"{Constants.RootNamespace}.Interpreter.Libraries",
            $"{Constants.RootNamespace}.Lexing",
            $"{Constants.RootNamespace}.Core.Async"
        );


        var ns = symbol.GetFullNamespace();
        if (ns != null) {
            typeData.WithNamespace(ns);
        }

        typeData.AddHeaderLine("#pragma warning disable CS0162 // Unreachable code detected");

        var parents = symbol.GetParentTypes();
        for (var i = parents.Count - 1; i >= 0; i--) {
            typeData._($"public partial class {parents[i].Name}");
            typeData.OpenBracket();
        }

        using (typeData.B($"public{(symbol.IsStatic ? " static" : "")} partial class {symbol.Name}")) {
            generator(context, symbol, typeData);
        }

        for (var i = 0; i < parents.Count; i++) {
            typeData.CloseBracket();
        }

        return typeData.ToString();
    }
}