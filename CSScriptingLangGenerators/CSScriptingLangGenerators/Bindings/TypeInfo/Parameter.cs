using System;
using System.Collections.Generic;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public enum ParameterType
{
    None,
    Unsupported,
    Value,
    Params,
    ExecCtx,
    FnExecCtx,
    Instance,
    Wrapped,
}

public class Parameter
{
    private static readonly List<ValueType> AnyTypes    = [ValueType.Unit];
    private static readonly List<ValueType> ObjectTypes = [ValueType.Object];

    public IParameterSymbol Info;

    public bool IsOptional;
    public bool PassRawValue;

    public ParameterType Type     { get; private set; }
    public string        TypeName { get; private set; }

    public int Priority { get; private set; }

    public string Name => Info.Name;

    public List<ValueType> Types { get; private set; } = new();

    public ITypeSymbol   UserDataType { get; private set; }
    public ValueTypeHint TypeHint     { get; set; }

    private Parameter(IParameterSymbol info) {
        Info       = info;
        IsOptional = info.IsOptional;

        if (!IsOptional && info.GetAttributeArgument(Attributes.Parameter, false)) {
            IsOptional = true;
        }
        if (info.GetAttributeArgument(Attributes.Parameter, false, 1)) {
            PassRawValue = true;
        }
    }

    public override string ToString() {
        return TypeName;
    }

    public bool RequiresParamTypeCheck() {
        if (Types?.Count == 0)
            return false;
        if (Types is [ValueType.Unit])
            return false;

        return Types?.Any(t => t.RequiresParameterTypeCheck()) ?? false;
    }

    public static Parameter Create(GeneratorExecutionContext context, IParameterSymbol info) {
        var param     = new Parameter(info);
        var paramType = info.Type;

        param.SetParamTypeData(context, info, paramType);
        param.TypeHint = BindingsGenerator.ConvertToValueTypeHint(paramType, paramType);

        return param;
    }

    private void SetParamTypeData(GeneratorExecutionContext context, IParameterSymbol info, ITypeSymbol paramType) {
        if (TypeData.TypeCheckMap.TryGetValue(paramType, out var types)) {
            Type     = ParameterType.Value;
            TypeName = types[0].GetName();

            if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.Bool)) {
                Priority = 10;
            } else if (TypeData.NumberTypes.Contains(paramType)) {
                Priority = 20;
            } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.String)) {
                Priority = 30;
            }

            Types = types;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.Value)) {
            if (info.HasAttribute(Attributes.InstanceParameter)) {
                Type     = ParameterType.Instance;
                TypeName = "instance";
            } else {
                Type     = ParameterType.Value;
                TypeName = "any";
                Priority = 100;
                Types    = AnyTypes;
            }
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueList) && info.IsParams) {
            Type     = ParameterType.Params;
            TypeName = "...";
            Priority = 75;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueArray) && info.IsParams) {
            Type     = ParameterType.Params;
            TypeName = "...";
            Priority = 75;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.RTVT)) {
            Type     = ParameterType.Value;
            TypeName = "RTVT";
            Priority = 50;
            Types    = [ValueType.Any];
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueList)) {
            Type     = ParameterType.Value;
            TypeName = "array";
            Priority = 50;
            Types    = [ValueType.Array];
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ExecContext)) {
            Type     = ParameterType.ExecCtx;
            TypeName = "ctx";
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.FnExecContext)) {
            Type     = ParameterType.FnExecCtx;
            TypeName = "ctx";
        } else if (paramType.TryGetAttribute(Attributes.Class, out var cls)) {
            Type         = ParameterType.Value;
            TypeName     = cls.GetArgument<string>() ?? paramType.Name;
            Types        = ObjectTypes;
            UserDataType = info.Type;
        } else if (paramType.OriginalDefinition.ToDisplayString() is "List<>") {
            Type     = ParameterType.Value;
            TypeName = "array";
            Priority = 50;
            Types    = [ValueType.Array];
        } else if (TypeData.WrappableClasses.Contains(paramType, SymbolEqualityComparer.Default)) {
            Type     = ParameterType.Wrapped;
            TypeName = "any";
            Priority = 100;
            Types    = [ValueType.WrappedValue];
        } else {
            if (info.Type.IsArrayInterface(out var arrayType)) {

                if (SymbolEqualityComparer.Default.Equals(arrayType, TypeData.InlineFunctionDeclaration) && !info.IsParams) {
                    Type     = ParameterType.Value;
                    TypeName = "array";
                    Priority = 50;
                    Types    = [ValueType.Array];

                    return;
                }

            }

            Type     = ParameterType.Unsupported;
            TypeName = "unknown";

            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedParameterType, info.Locations.First(), info.Type.GetFullyQualifiedName()));
        }
    }

    public void WriteTypeCheck(int index, Writer w) {
        // using (w.If($"args.Length <= {index}")) {
        //     w._($"throw new InterpreterRuntimeException(\"{Name}: expected {index + 1} arguments\");");
        // }

        if (!RequiresParamTypeCheck())
            return;

        var comparers = Types.Select(
            t => $"args.Length > {index} && !args[{index}].Type.{t.GetTypeCheckFunction()}"
        ).ToArray();

        using (w.If($"{string.Join(" || ", comparers)}")) {
            w._($"throw new InterpreterRuntimeException(\"Expected argument ({index + 1} - {Name}) of type {TypeName}\");");
        }
    }
}