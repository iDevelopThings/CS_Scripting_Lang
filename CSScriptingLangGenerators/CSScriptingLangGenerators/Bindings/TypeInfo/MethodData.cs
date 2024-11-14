using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public class MethodData
{
    public string             Name          { get; set; }
    public string             Identifier    { get; set; }
    public List<List<Method>> Methods       { get; set; }
    public List<Method>       ParamsMethods { get; set; }

    public bool IsGlobal => Methods?.Any(m => m.Any(m => m.IsGlobal)) ?? false;

    public ITypeSymbol ReturnType  => Methods?.SelectMany(m => m).FirstOrDefault()?.ReturnType;
    public Method      FirstMethod => Methods?.SelectMany(m => m).FirstOrDefault();

    public bool RequiresInstanceParameter() => Methods?.Any(m => m.Any(m => m.RequiresInstanceParameter)) ?? false;
    public bool IsStatic()                  => Methods?.Any(m => m.Any(m => m.Info.IsStatic)) ?? false;

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


    public bool IgnoreParamChecks() => Methods?.Any(m => m.Any(m => m.IgnoreParamChecks())) ?? false;
}