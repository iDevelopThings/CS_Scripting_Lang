using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;


namespace CSScriptingLangGenerators.Bindings;

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

    public bool RequiresInstanceParameter => !Info.IsStatic || Parameters.Any(p => p.Type == ParameterType.Instance);
    public bool IsConstructor             { get; set; }
    public bool IsPropertyGetter          { get; set; }

    public ITypeSymbol ReturnType => Info.ReturnType;
    public ValueTypeHint ReturnTypeHint { get; set; }

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

        ReturnTypeHint = BindingsGenerator.ConvertToValueTypeHint(info.ReturnType, info.ReturnType);
        
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
            ParameterType.Wrapped     => BindingsGenerator.ConvertFromValue(context, i, parameter.Info.Type, parameter.Info),
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

    public void WriteCall(GeneratorExecutionContext context, Writer w, string qualifier, int argCount = 10000) {
        var isConstructor = Info.MethodKind == MethodKind.Constructor; // || IsConstructor;

        var returnType = isConstructor
            ? Info.ContainingType
            : Info.ReturnType;

        var hasReturn = !SymbolEqualityComparer.Default.Equals(returnType, TypeData.Void);

        qualifier = Info.IsStatic ? Info.ContainingType.GetFullyQualifiedName() : qualifier;

        w._(
            $"// Has return? {hasReturn}",
            $"// Method: {Info.Name}",
            $"// ReturnType: {returnType.GetFullyQualifiedName()}",
            $"// IsConstructor: {isConstructor}",
            $"// Args: "
        );

        w._(
            Parameters.Select(p => {
                return $"// - {p.Info.Name}: (RequiresTypeCheck: {p.RequiresParamTypeCheck()}) {p.Types.Select(t => t.ToString()).Join(", ")}";
            })
        );

        if (isConstructor) {
            var argBindings = GetArgumentBindings(context, argCount, ParameterType.Params)
               .Select(b => $"{w.GetIndentString()}{b}")
               .Join(", \n");

            var isClassDataObj = Info.ContainingType.HasAttribute(Attributes.ClassDataObject);
            var extendsValue   = Info.ContainingType.HasBaseType(TypeData.Value);

            if (isClassDataObj || extendsValue) {
                w._(
                    $"var inst = new global::{Info.ContainingType.GetFullyQualifiedName()}(",
                    GetArgumentBindings(context, argCount).Select(b => $"{w.GetIndentString()}{b}").Join(", \n"),
                    ");"
                );

                if (isClassDataObj) {
                    w._($"return Value.ClassInstance(ctx, inst, \"{Info.ContainingType.GetFullyQualifiedName()}\");");
                    return;
                }

                w._("return inst;");
                return;
            }

            w._(
                $"return {BindingsGenerator.ConvertToValue(context, argBindings, returnType, Info)};"
            );

            return;
        }

        if (hasReturn) {
            w._(
                $"var result = {qualifier}.{Info.Name}(",
                GetArgumentBindings(context, argCount).Select(b => $"{w.GetIndentString()}{b}").Join(", \n"),
                ");",
                $"return {BindingsGenerator.ConvertToValue(context, "result", returnType, Info)};"
            );

            return;
        }

        w._(
            $"{qualifier}.{Info.Name}(",
            GetArgumentBindings(context, argCount).Select(b => $"{w.GetIndentString()}{b}").Join(", \n"),
            ");"
        );
        w._("return Value.Null();");

    }
    public void WriteParameterChecks(Writer w, Method method) {
        WriteParameterChecks(w, method, out _, 10000);
    }
    public void WriteParameterChecks(Writer w, Method method, out bool isTrueResult, int limit) {
        /*var paramsToCheck = method.Parameters
           .Take(limit)
           .Where(p => p.Type == ParameterType.Value)
           .Where(p => p.RequiresParamTypeCheck())
           .ToList();*/

        var paramsChecked = 0;
        var pIdx          = 0;
        for (var i = 0; i < method.Parameters.Count; i++) {
            if (i >= limit) {
                break;
            }

            var p = method.Parameters[i];
            if (p.Type != ParameterType.Value)
                continue;
            if (!p.RequiresParamTypeCheck()) {
                pIdx++;
                continue;
            }

            p.WriteTypeCheck(pIdx, w);

            paramsChecked++;
            pIdx++;
        }

        /*isTrueResult = paramsToCheck.Count == 0;

        for (var i = 0; i < paramsToCheck.Count; i++) {
            paramsToCheck[i].WriteTypeCheck(i, w);
        }*/

        isTrueResult = paramsChecked >= 0;
    }
    public bool IgnoreParamChecks() {
        return Info.HasAttribute(Attributes.IgnoreParamChecks);
    }
}