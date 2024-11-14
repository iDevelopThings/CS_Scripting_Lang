using System;
using System.Collections.Generic;
using System.Linq;
using CSScriptingLangGenerators.Bindings;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Binds;

public static class TypeData
{
    public static INamedTypeSymbol Value      { get; private set; }
    public static INamedTypeSymbol ValueList  { get; private set; }
    public static IArrayTypeSymbol ValueArray { get; private set; }
    public static INamedTypeSymbol ValueDict  { get; private set; }

    public static INamedTypeSymbol ILibraryInterface     { get; private set; }
    public static INamedTypeSymbol ILibraryImplInterface { get; private set; }

    public static INamedTypeSymbol Prototype { get; private set; }

    public static INamedTypeSymbol RTVT { get; set; }

    public static INamedTypeSymbol ExecContext   { get; private set; }
    public static INamedTypeSymbol FnExecContext { get; set; }

    public static INamedTypeSymbol Double   { get; private set; }
    public static INamedTypeSymbol Float    { get; private set; }
    public static INamedTypeSymbol Int      { get; private set; }
    public static INamedTypeSymbol Long     { get; private set; }
    public static INamedTypeSymbol Void     { get; private set; }
    public static INamedTypeSymbol String   { get; private set; }
    public static INamedTypeSymbol Bool     { get; private set; }
    public static INamedTypeSymbol Nullable { get; private set; }
    public static INamedTypeSymbol Task     { get; private set; }
    public static INamedTypeSymbol TaskOfT  { get; private set; }

    public static HashSet<ITypeSymbol> NumberTypes { get; private set; }


    public static Dictionary<ITypeSymbol, List<ValueType>> TypeCheckMap { get; private set; }

    public struct OperatorData
    {
        public string Name                   { get; set; }
        public string Token                  { get; set; }
        public string Identifier             { get; set; }
        public string OperatorOverloadFnName => $"operator_{Identifier}";
    }

    public static Dictionary<string, OperatorData> Operators           { get; private set; }
    public static Dictionary<string, OperatorData> OperatorsByOperator => Operators.Values.ToDictionary(o => o.Token, o => o);
    public static Dictionary<string, OperatorData> OperatorsByIdent    => Operators.Values.ToDictionary(o => o.Identifier, o => o);

    public static bool Initialize(Compilation compilation, SourceProductionContext context) {
        try {
            Double   = compilation.GetSpecialType(SpecialType.System_Double);
            Float    = compilation.GetSpecialType(SpecialType.System_Single);
            Int      = compilation.GetSpecialType(SpecialType.System_Int32);
            Long     = compilation.GetSpecialType(SpecialType.System_Int64);
            Void     = compilation.GetSpecialType(SpecialType.System_Void);
            String   = compilation.GetSpecialType(SpecialType.System_String);
            Bool     = compilation.GetSpecialType(SpecialType.System_Boolean);
            Nullable = compilation.GetSpecialType(SpecialType.System_Nullable_T);
            Task     = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            TaskOfT  = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");

            ExecContext           = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Context.ExecContext");
            FnExecContext         = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Context.FunctionExecContext");
            RTVT                  = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Types.RTVT");
            Prototype             = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Prototypes.Prototype");
            Value                 = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Values.Value");
            ILibraryInterface     = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Libraries.ILibrary");
            ILibraryImplInterface = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Libraries.ILibraryImpl");

            var operatorsEnum = compilation.GetTypeByMetadataName($"{Constants.RootNamespace}.Lexing.OperatorType");
            if (operatorsEnum == null) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.MissingOperandType, Location.None));
                return false;
            }

            Operators = operatorsEnum.GetMembers()
               .OfType<IFieldSymbol>()
               .Select(f => {
                    var attr = f.GetAttribute("OperatorChars");
                    return new OperatorData {
                        Name       = f.Name,
                        Identifier = attr?.GetArgument<string>(1),
                        Token      = attr?.GetArgument<string>(),
                    };
                })
               .Where(o => o is {Token: not null, Identifier: not null})
               .ToDictionary(o => o.Name, o => o);

            ValueList  = compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1")?.Construct(Value);
            ValueArray = compilation.CreateArrayTypeSymbol(Value);
            ValueDict  = compilation.GetBestTypeByMetadataName("System.Collections.Generic.Dictionary`2")?.Construct(String, Value);

            TypeCheckMap = new Dictionary<ITypeSymbol, List<ValueType>>(SymbolEqualityComparer.Default) {
                {Double, [ValueType.Double]},
                {Float, [ValueType.Float]},
                {Int, [ValueType.Int32]},
                {Long, [ValueType.Int64]},
                {String, [ValueType.String]},
                {Bool, [ValueType.Boolean]},
            };

            NumberTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default) {
                Double,
                Float,
                Int,
                Long
            };

        }
        catch (InvalidOperationException e) {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.FailedToInitialize, Location.None, $"{e.Message}\n{e.StackTrace}"));
            return false;
        }

        return true;
    }


    public static string GetOperatorIdentifier(string token) {
        if (OperatorsByOperator.TryGetValue(token, out var op)) {
            return op.Identifier;
        }

        throw new InvalidOperationException($"Operator '{token}' not found");
    }
}

public enum ValueType
{
    Unit,
    Null,
    Any,
    Boolean,
    Object,
    Array,
    Int32,
    Int64,
    Float,
    Double,
    String,
    Function
}

public static class ValueTypeExtensions
{
    public static bool RequiresParameterTypeCheck(this ValueType type) {
        switch (type) {
            case ValueType.Unit:
            case ValueType.Null:
            case ValueType.Any:
                return false;

            case ValueType.Boolean:
            case ValueType.Object:
            case ValueType.Array:
            case ValueType.Int32:
            case ValueType.Int64:
            case ValueType.Float:
            case ValueType.Double:
            case ValueType.String:
            case ValueType.Function:
                return true;

            default:
                throw new NotSupportedException();
        }
    }

    public static string GetName(this ValueType type) {
        switch (type) {
            case ValueType.Unit:
                return "Unit";

            case ValueType.Null:
                return "null";

            case ValueType.Any:
                return "any";

            case ValueType.Boolean:
                return "bool";

            case ValueType.Object:
                return "object";

            case ValueType.Array:
                return "array";

            case ValueType.Int32:
                return "int32";
            case ValueType.Int64:
                return "int64";
            case ValueType.Float:
                return "float";
            case ValueType.Double:
                return "double";
            case ValueType.String:
                return "String";

            case ValueType.Function:
                return "function";

            default:
                throw new NotSupportedException();
        }
    }
    public static string GetTypeCheckFunction(this ValueType type) {
        switch (type) {
            case ValueType.Unit:
                return "IsUnit()";
            case ValueType.Null:
                return "IsNull()";
            case ValueType.Boolean:
                return "IsBool()";
            case ValueType.Object:
                return "IsObject()";
            case ValueType.Array:
                return "IsArray()";
            case ValueType.Int32:
                return "IsInt32()";
            case ValueType.Int64:
                return "IsInt64()";
            case ValueType.Float:
                return "IsFloat()";
            case ValueType.Double:
                return "IsDouble()";
            case ValueType.String:
                return "IsString()";
            case ValueType.Function:
                return "IsFunction()";

            default:
                throw new NotSupportedException();
        }
    }
}