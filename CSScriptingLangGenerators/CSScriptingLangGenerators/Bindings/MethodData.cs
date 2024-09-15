using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public class MethodData
{
    public string             Name          { get; set; }
    public string             Identifier    { get; set; }
    public List<List<Method>> Methods       { get; set; }
    public List<Method>       ParamsMethods { get; set; }

    public bool IsGlobal => Methods?.Any(m => m.Any(m => m.IsGlobal)) ?? false;

    public MethodData(string name, string identifier, List<List<Method>> methods, List<Method> paramsMethods) {
        Name          = name;
        Identifier    = identifier;
        Methods       = methods;
        ParamsMethods = paramsMethods;
    }

    public Method GetFirstMethod() => Methods.SelectMany(m => m).FirstOrDefault();

    public static List<MethodData> Build(GeneratorExecutionContext context, IEnumerable<(IMethodSymbol Method, string Name, string Identifier)> source) {
        return source
           .GroupBy(m => m.Name)
           .Select(g => BuildMethodTable(context, g.Select(m => new Method(context, g.Key, m.Identifier, m.Method))))
           .ToList();
    }

    public static MethodData BuildMethodTable(GeneratorExecutionContext context, IEnumerable<Method> source) {
        var sourceList = source.ToList();

        string name          = null;
        string identifier    = null;
        var    methods       = new List<List<Method>>();
        var    paramsMethods = new List<Method>();

        foreach (var method in sourceList) {
            if (name == null || identifier == null) {
                name       = method.Name;
                identifier = method.Identifier;
            }

            if (method.HasParams) {
                paramsMethods.Add(method);
            }

            for (var i = method.RequiredParameterCount; i <= method.ParameterCount; i++) {
                while (methods.Count <= i) {
                    methods.Add(new List<Method>());
                }

                methods[i].Add(method);
            }
        }

        for (var i = 0; i < methods.Count; i++) {
            methods[i].Sort();
            methods[i] = methods[i].Distinct(new MethodParameterEqualityComparer(i)).ToList();
        }

        paramsMethods.Sort();

        // make sure all functions made it in
        var sourceMethodInfo = sourceList.Select(m => m.Info);

        var tableMethodInfo  = methods.SelectMany(l => l).Select(m => m.Info);
        var paramsMethodInfo = paramsMethods.Select(m => m.Info);

        var difference = sourceMethodInfo
           .Except(tableMethodInfo.Concat(paramsMethodInfo))
           .ToList();

        foreach (var method in difference) {
            var methodName = method.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMethodOverloadConflicts, method.Locations.First(), methodName));
        }

        return new MethodData(name, identifier, methods, paramsMethods);
    }

    private class MethodParameterEqualityComparer : IEqualityComparer<Method>
    {
        private readonly int _length;

        public MethodParameterEqualityComparer(int length) {
            _length = length;
        }

        public bool Equals(Method x, Method y) {
            var xParams = x.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();
            var yParams = y.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();

            for (var i = 0; i < _length; i++) {
                if (!xParams[i].Types.SequenceEqual(yParams[i].Types) ||
                    xParams[i].IsOptional != yParams[i].IsOptional) {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(Method obj) {
            return 0;
        }
    }


}

public class Method : IComparable<Method>
{
    public IMethodSymbol Info { get; set; }

    public string Name       { get; set; }
    public string Identifier { get; set; }

    public int ParameterCount         { get; set; }
    public int RequiredParameterCount { get; set; }

    public List<Parameter> Parameters      { get; set; }
    public List<Parameter> ValueParameters { get; set; }

    public bool HasParams { get; set; }

    public bool IsGlobal { get; set; }

    public Method(GeneratorExecutionContext context, string name, string identifier, IMethodSymbol info) {
        Name       = name;
        Identifier = identifier;
        Info       = info;

        var parameters = info.Parameters;

        Parameters = parameters
           .Select(p => Parameter.Create(context, p))
           .ToList();

        ValueParameters = Parameters
           .Where(p => p.Type == ParameterType.Value)
           .ToList();

        ParameterCount         = ValueParameters.Count;
        RequiredParameterCount = ValueParameters.Count(p => !p.IsOptional);

        HasParams = Parameters.Any(p => p.Type == ParameterType.Params);
        IsGlobal  = info.HasAttribute(Attributes.GlobalFunction);
    }

    public override string ToString() {
        var sb = new StringBuilder();
        sb.Append(Name);
        sb.Append('(');

        string sep = null;

        foreach (var p in Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params)) {
            if (sep != null)
                sb.Append(sep);

            sb.Append(p);

            if (p.IsOptional)
                sb.Append('?');

            sep = ", ";
        }

        sb.Append(')');
        return sb.ToString();
    }

    public int CompareTo(Method other) {
        var x = this;
        var y = other;

        var xParams = x.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();
        var yParams = y.Parameters.Where(p => p.Type == ParameterType.Value || p.Type == ParameterType.Params).ToList();

        for (var i = 0;; i++) {
            if (i >= xParams.Count && i >= yParams.Count)
                return 0; // x == y

            if (i >= xParams.Count)
                return -1; // x < y

            if (i >= yParams.Count)
                return 1; // x > y

            var diff = xParams[i].Priority - yParams[i].Priority;
            if (diff != 0)
                return diff;
        }
    }
    public bool RequiresParamTypeCheck() => Parameters.Any(p => p.RequiresParamTypeCheck());


    public IEnumerable<string> GetArgumentBindings(GeneratorExecutionContext context, int argCount, ParameterType type = ParameterType.None) {
        var valueIdx = 0;

        foreach (var param in Parameters) {
            if (valueIdx >= argCount && param.Type == ParameterType.Value) {
                continue;
            }

            yield return BindArgument(context, valueIdx, param, type == ParameterType.None ? param.Type : type);

            // Add `,` after each argument except the last one


            if (param.Type == ParameterType.Value) {
                valueIdx++;
            }
        }

    }
    public string BindArguments(GeneratorExecutionContext context, int argCount) {
        var valueIdx = 0;
        var args     = new List<string>();
        foreach (var param in Parameters) {
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

    public string BindArgument(GeneratorExecutionContext context, int i, Parameter parameter, ParameterType type) {
        return type switch {
            ParameterType.Unsupported => $"default /* unsupported type {parameter.Info.Type.GetFullyQualifiedName()} */",
            ParameterType.Value       => BindingsGenerator.ConvertFromValue(context, i, parameter.Info.Type, parameter.Info),
            ParameterType.Params      => $"args[{i}..]",
            ParameterType.ExecCtx     => "ctx",
            ParameterType.FnExecCtx   => "ctx",
            ParameterType.Instance    => "instance",
            _                         => throw new NotSupportedException($"{nameof(BindArgument)} {nameof(ParameterType)} {parameter.Type}"),
        };
    }
    public string BindArgument(GeneratorExecutionContext context, int i, Parameter parameter) {
        return BindArgument(context, i, parameter, parameter.Type);
    }

}

public enum ParameterType
{
    None,
    Unsupported,
    Value,
    Params,
    ExecCtx,
    FnExecCtx,
    Instance,
}

public class Parameter
{
    private static readonly List<ValueType> AnyTypes    = [ValueType.Unit];
    private static readonly List<ValueType> ObjectTypes = [ValueType.Object];

    public readonly IParameterSymbol Info;
    public readonly bool             IsOptional;

    public ParameterType Type     { get; private set; }
    public string        TypeName { get; private set; }

    public int Priority { get; private set; }

    public string Name => Info.Name;

    public List<ValueType> Types { get; private set; } = new();

    public ITypeSymbol UserDataType { get; private set; }

    private Parameter(IParameterSymbol info) {
        Info       = info;
        IsOptional = info.IsOptional;

        if (!IsOptional && info.GetAttributeArgument(Attributes.Parameter, false)) {
            IsOptional = true;
        }
    }

    public override string ToString() {
        return TypeName;
    }

    public bool RequiresParamTypeCheck() {
        if (Types?.Count == 0)
            return false;
        if (Types?.Count == 1 && Types[0] == ValueType.Unit)
            return false;

        return Types?.Any(t => t.RequiresParameterTypeCheck()) ?? false;
    }

    public static Parameter Create(GeneratorExecutionContext context, IParameterSymbol info) {
        var param     = new Parameter(info);
        var paramType = info.Type;

        if (TypeData.TypeCheckMap.TryGetValue(paramType, out var types)) {
            param.Type     = ParameterType.Value;
            param.TypeName = types[0].GetName();

            if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.Bool)) {
                param.Priority = 10;
            } else if (TypeData.NumberTypes.Contains(paramType)) {
                param.Priority = 20;
            } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.String)) {
                param.Priority = 30;
            }

            param.Types = types;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.Value)) {
            if (info.HasAttribute(Attributes.InstanceParameter)) {
                param.Type     = ParameterType.Instance;
                param.TypeName = "instance";
            } else {
                param.Type     = ParameterType.Value;
                param.TypeName = "any";
                param.Priority = 100;
                param.Types    = AnyTypes;
            }
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueList) && info.IsParams) {
            param.Type     = ParameterType.Params;
            param.TypeName = "...";
            param.Priority = 75;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueArray) && info.IsParams) {
            param.Type     = ParameterType.Params;
            param.TypeName = "...";
            param.Priority = 75;
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.RTVT)) {
            param.Type     = ParameterType.Value;
            param.TypeName = "RTVT";
            param.Priority = 50;
            param.Types    = [ValueType.Any];
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ValueList)) {
            param.Type     = ParameterType.Value;
            param.TypeName = "array";
            param.Priority = 50;
            param.Types    = [ValueType.Array];
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.ExecContext)) {
            param.Type     = ParameterType.ExecCtx;
            param.TypeName = "ctx";
        } else if (SymbolEqualityComparer.Default.Equals(paramType, TypeData.FnExecContext)) {
            param.Type     = ParameterType.FnExecCtx;
            param.TypeName = "ctx";
        } else if (paramType.TryGetAttribute(Attributes.Class, out var cls)) {
            param.Type         = ParameterType.Value;
            param.TypeName     = cls.GetArgument<string>() ?? paramType.Name;
            param.Types        = ObjectTypes;
            param.UserDataType = info.Type;
        } else if (TypeData.ValueTypes.TryGetValue(paramType as INamedTypeSymbol, out var valueType)) {
            param.Type     = ParameterType.Value;
            param.TypeName = valueType.ToDisplayString();
            param.Priority = 100;
            // param.Types    = AnyTypes;
        } else if (paramType.OriginalDefinition.ToDisplayString() is "List<>") {
            param.Type     = ParameterType.Value;
            param.TypeName = "array";
            param.Priority = 50;
            param.Types    = [ValueType.Array];
        } else {
            param.Type     = ParameterType.Unsupported;
            param.TypeName = "unknown";

            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedParameterType, info.Locations.First(), info.Type.GetFullyQualifiedName()));
        }

        return param;
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