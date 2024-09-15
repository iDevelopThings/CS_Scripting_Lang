using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSScriptingLangGenerators.Bindings;

public static class TypeData
{
    public static INamedTypeSymbol Value      { get; private set; }
    public static INamedTypeSymbol ValueList  { get; private set; }
    public static IArrayTypeSymbol ValueArray { get; private set; }

    public static INamedTypeSymbol Prototype { get; private set; }

    public static INamedTypeSymbol BaseValue   { get; private set; }
    public static INamedTypeSymbol ValueObject { get; private set; }
    public static INamedTypeSymbol ValueString { get; private set; }
    public static INamedTypeSymbol RTVT        { get; set; }

    public static INamedTypeSymbol ExecContext                    { get; private set; }
    public static INamedTypeSymbol FnExecContext                  { get; set; }
    public static INamedTypeSymbol NativeFunctionExecutionContext { get; set; }

    public static INamedTypeSymbol Void   { get; private set; }
    public static INamedTypeSymbol String { get; private set; }
    public static INamedTypeSymbol Bool   { get; private set; }

    public static INamedTypeSymbol BaseValueList  { get; private set; }
    public static IArrayTypeSymbol BaseValueArray { get; private set; }
    public static INamedTypeSymbol BaseValueDict  { get; private set; }


    public static HashSet<INamedTypeSymbol> ValueTypes  { get; private set; }
    public static HashSet<ITypeSymbol>      NumberTypes { get; private set; }


    public static Dictionary<ITypeSymbol, List<ValueType>> TypeCheckMap { get; private set; }

    public struct OperatorData
    {
        public string Name       { get; set; }
        public string Token      { get; set; }
        public string Identifier { get; set; }
    }

    public static Dictionary<string, OperatorData> Operators           { get; private set; }
    public static Dictionary<string, OperatorData> OperatorsByOperator => Operators.Values.ToDictionary(o => o.Token, o => o);

    public static bool Initialize(GeneratorExecutionContext context) {

        try {
            var compilation = context.Compilation;

            var doubleSym   = compilation.GetSpecialType(SpecialType.System_Double);
            var floatSym    = compilation.GetSpecialType(SpecialType.System_Single);
            var intSym      = compilation.GetSpecialType(SpecialType.System_Int32);
            var longSym     = compilation.GetSpecialType(SpecialType.System_Int64);
            var voidSym     = compilation.GetSpecialType(SpecialType.System_Void);
            var stringSym   = compilation.GetSpecialType(SpecialType.System_String);
            var boolSym     = compilation.GetSpecialType(SpecialType.System_Boolean);
            var nullableSym = compilation.GetSpecialType(SpecialType.System_Nullable_T);

            ExecContext                    = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Context.ExecContext");
            FnExecContext                  = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Context.FunctionExecContext");
            NativeFunctionExecutionContext = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.Interpreter.Bindings.NativeFunctionExecutionContext");
            RTVT                           = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Types.RTVT");

            var valueSym = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Values.BaseValue");
            // .SingleOrDefault(s => s.ContainingAssembly.Identity.Name == Constants.AssemblyName);

            if (valueSym == null) {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BaseValueNotFound, Location.None));
                return false;
            }

            Prototype = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Prototypes.Prototype");
            Value     = compilation.GetBestTypeByMetadataName($"{Constants.RootNamespace}.RuntimeValues.Values.Value");

            ValueTypes  = new HashSet<INamedTypeSymbol>(valueSym.GetDerivedTypes(), SymbolEqualityComparer.Default);
            ValueObject = ValueTypes.First(t => t.Name == "ValueObject");
            ValueString = ValueTypes.First(t => t.Name == "ValueString");

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
                        Token      = attr?.GetArgument<string>()
                    };
                })
               .Where(o => o.Token != null && o.Identifier != null)
               .ToDictionary(o => o.Name, o => o);

            /*
            var operatorsEnumDeclaration = operatorsEnum.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as EnumDeclarationSyntax;
            if (operatorsEnumDeclaration != null) {
                var operatorsEnumMembers = operatorsEnumDeclaration!.Members
                   .Select(m => new {
                        Name = m.Identifier.Text,
                        OperatorToken = m.AttributeLists
                           .SelectMany(a => a.Attributes)
                           .Where(a => a.Name is IdentifierNameSyntax {Identifier.Text: "OperatorChars"})
                           .Select(a => a.ArgumentList)
                           .SelectMany(a => a.Arguments)
                           .Select(a => a.Expression)
                           .OfType<LiteralExpressionSyntax>()
                           .Select(l => l.Token.ValueText)
                           .FirstOrDefault()
                    });

                Operators = operatorsEnumMembers.ToDictionary(
                    m => m.Name,
                    m => new OperatorData() {
                        Token = m.OperatorToken
                    }
                );
            } else {
                Operators = operatorsEnum.GetMembers()
                   .OfType<IFieldSymbol>()
                   .ToDictionary(
                        f => f.Name,
                        f => {
                            var attr = f.GetAttributes().GetAttribute("OperatorChars");

                            return new OperatorData() {
                                Token      = attr?.GetArgument<string>(),
                                Identifier = attr?.GetArgument<string>(1),
                            };
                        }
                    );
            }
            */

            Void   = voidSym;
            String = stringSym;
            Bool   = boolSym;

            BaseValue = valueSym;

            ValueList  = compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1")?.Construct(Value);
            ValueArray = compilation.CreateArrayTypeSymbol(Value);

            BaseValueList  = compilation.GetBestTypeByMetadataName("System.Collections.Generic.List`1")?.Construct(valueSym);
            BaseValueArray = compilation.CreateArrayTypeSymbol(valueSym);
            BaseValueDict  = compilation.GetBestTypeByMetadataName("System.Collections.Generic.Dictionary`2")?.Construct(String, valueSym);

            TypeCheckMap = new Dictionary<ITypeSymbol, List<ValueType>>(SymbolEqualityComparer.Default) {
                {doubleSym, [ValueType.Double]},
                {floatSym, [ValueType.Float]},
                {intSym, [ValueType.Int32]},
                {longSym, [ValueType.Int64]},
                {stringSym, [ValueType.String]},
                {boolSym, [ValueType.Boolean]},
                {valueSym, [ValueType.Any]},
                {ValueObject, [ValueType.Object]},
                {ValueString, [ValueType.String]},
            };

            NumberTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default) {
                doubleSym,
                floatSym,
                intSym,
                longSym
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