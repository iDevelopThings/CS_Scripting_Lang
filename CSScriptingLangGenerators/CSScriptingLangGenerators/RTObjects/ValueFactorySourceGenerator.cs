using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharp.CSharpExtensions;


namespace CSScriptingLangGenerators.RTObjects;

[Generator]
public class ValueFactorySourceGenerator : IIncrementalGenerator
{
    public INamedTypeSymbol       BaseValueType   { get; private set; }
    public INamedTypeSymbol       NumberValueType { get; private set; }
    public List<INamedTypeSymbol> ValueTypes      { get; private set; } = new();
    public List<INamedTypeSymbol> AllValueTypes   { get; private set; } = new();
    public List<INamedTypeSymbol> AllNumberTypes  { get; private set; } = new();

    public struct TypeInformation
    {
        public TypeInformation() {
            Name        = null;
            ClassType   = null;
            RTVT        = null;
            ValueType   = null;
            RuntimeType = null;
        }
        public string Name { get; set; }

        public string ClassType   { get; set; }
        public string RTVT        { get; set; }
        public string ValueType   { get; set; }
        public string RuntimeType { get; set; }


        public Dictionary<string, MathOpInfo> MathOperators { get; set; } = new();
        public List<CastAttribute>            Casts         { get; set; } = new();
        public List<INamedTypeSymbol>         ParentTypes   { get; set; } = new();

        public List<AttributeSyntax> AllAttributes { get; set; } = new();
    }

    public struct MathOpInfo
    {
        public string Operator           { get; set; }
        public string CastToIntermediary { get; set; }
    }

    public static Dictionary<string, TypeInformation> TypeInfos     { get; private set; } = new();
    public static Dictionary<string, TypeInformation> BaseTypeInfos { get; private set; } = new();

    public bool LoadedTypes { get; set; }

    private void EnsureTypesLoaded(Compilation compilation) {
        if (LoadedTypes) return;

        BaseValueType   = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.Values.BaseValue");
        NumberValueType = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.Values.Number");
        ValueTypes      = ClassUtils.GetDerivedTypes(compilation, BaseValueType);
        AllValueTypes   = ClassUtils.GetDerivedTypes(compilation, BaseValueType, true);

        AllNumberTypes = ClassUtils.GetDerivedTypes(compilation, NumberValueType, false);

        var valueTypeAttr = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.Values.ValueTypeAttribute");
        var valueMathAttr = compilation.GetTypeByMetadataName("CSScriptingLang.RuntimeValues.Values.ValueMathOperator");

        foreach (var type in AllValueTypes) {
            var name = type.Name;

            var parentClassTypes = type.GetBaseTypes(BaseValueType).ToList();

            var classDeclaration        = ClassUtils.GetDeclarationFor(type, compilation);
            var parentClassDeclarations = ClassUtils.GetDeclarationsFor(parentClassTypes, compilation);

            if (type.Name == "Number" && type.IsGenericType)
                continue;

            if (type.Name == "BaseValue" && type.IsGenericType)
                continue;

            var typeInfo = new TypeInformation {
                Name        = name,
                ParentTypes = parentClassTypes,
                Casts       = new(),
            };

            var valueAttr = classDeclaration.AttributeLists.SelectMany(al => al.Attributes)
               .FirstOrDefault(a => a.HasName("ValueType", "ValueTypeAttribute"));

            if (valueAttr is not null) {
                var typeArgs = valueAttr.Name as GenericNameSyntax;

                typeInfo.ClassType   = typeArgs?.TypeArgumentList.Arguments[0].ToString();
                typeInfo.RuntimeType = typeArgs?.TypeArgumentList.Arguments[1].ToString();
                typeInfo.ValueType   = typeArgs?.TypeArgumentList.Arguments[2].ToString();
                typeInfo.RTVT        = valueAttr.ArgumentList!.Arguments[0].Expression.ToString();
            }


            var attributes = classDeclaration.AttributeLists
               .Concat(parentClassDeclarations.SelectMany(d => d.AttributeLists))
               .SelectMany(al => al.Attributes)
               .ToList();

            typeInfo.AllAttributes = attributes;

            var valueTypeCastAttrs = attributes.Where(a => a.HasName("ValueTypeCast", "ValueTypeCastAttribute")).ToList();
            var mathOpAttrs        = attributes.Where(a => a.HasName("ValueMathOperator", "ValueMathOperatorAttribute")).ToList();

            foreach (var mathOpAttr in mathOpAttrs) {
                var op = mathOpAttr.ArgumentList!.Arguments[0].Expression.ToString().Replace("\"", "");
                typeInfo.MathOperators.Add(op, new MathOpInfo {
                    Operator           = op,
                    CastToIntermediary = (mathOpAttr.Name as GenericNameSyntax)?.TypeArgumentList.Arguments[0].ToString(),
                });
            }

            foreach (var attr in valueTypeCastAttrs) {
                var castTo        = (attr.Name as GenericNameSyntax)?.TypeArgumentList.Arguments[0].ToString();
                var castFrom      = name;
                var mathOperators = attr.ArgumentList!.Arguments.Select(arg => arg.Expression.ToString().Replace("\"", "")).ToArray();

                if (castTo == "Number") {
                    foreach (var namedTypeSymbol in AllNumberTypes) {
                        typeInfo.Casts.Add(new CastAttribute {
                            CastTo        = namedTypeSymbol.Name,
                            CastFrom      = castFrom,
                            MathOperators = mathOperators
                        });
                    }

                    continue;
                }

                var castAttr = new CastAttribute {
                    CastTo        = castTo,
                    CastFrom      = castFrom,
                    MathOperators = mathOperators
                };

                typeInfo.Casts.Add(castAttr);
            }


            TypeInfos.TryAdd(name, typeInfo);

            if (ValueTypes.Any(vt => vt.Name == name))
                BaseTypeInfos.TryAdd(name, typeInfo);

        }

        LoadedTypes = true;
    }


    public void Initialize(IncrementalGeneratorInitializationContext context) {
        var classDeclarations = context.SyntaxProvider
           .CreateSyntaxProvider(
                (s,   _) => IsCandidateClass(s),
                (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
           .Where(m => m is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(
            classDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndClasses,
            (spc, source) => Execute(source.Left, source.Right, spc)
        );

    }

    private bool IsCandidateClass(SyntaxNode syntaxNode) {
        if (syntaxNode is not ClassDeclarationSyntax classDeclaration)
            return false;

        var hasAttr = classDeclaration.AttributeLists.SelectMany(al => al.Attributes)
           .Any(a => a.HasName("ValueType", "ValueTypeAttribute"));

        return hasAttr;
    }

    private ClassDeclarationSyntax GetSemanticTargetForGeneration(GeneratorSyntaxContext context) {
        var classDeclaration = (ClassDeclarationSyntax) context.Node;

        if (context.SemanticModel.GetDeclaredSymbol(classDeclaration) is not INamedTypeSymbol symbol)
            return null;

        if (symbol.BaseType is null)
            return null;

        if (symbol.ContainingNamespace.ToDisplayString() != "CSScriptingLang.RuntimeValues.Values")
            return null;

        return classDeclaration;
    }

    private void Execute(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, SourceProductionContext context) {
        var classDeclarations = classes.ToList();

        EnsureTypesLoaded(compilation);

        // ExecuteClassCastAndOperatorGeneration(compilation, classDeclarations, context);

        // ExecuteClassAdditionalGeneration(compilation, classDeclarations, context);
        // ExecuteFactoryGeneration(compilation, classDeclarations, context);

    }


    private void ExecuteFactoryGeneration(Compilation compilation, List<ClassDeclarationSyntax> classes, SourceProductionContext context) {

        var w = new Writer()
           .WithNamespace("CSScriptingLang.RuntimeValues")
           .WithImports("CSScriptingLang.RuntimeValues.Types")
           .WithImports("CSScriptingLang.Parsing.AST")
           .WithImports("ValueTypes = CSScriptingLang.RuntimeValues.Values")
           .WithImports("System.Collections.Generic");

        using (w.B("public static class ValueFactory")) {

            using (w.B($"public static T Make<T>(object value = null) where T : {FactoryPrefix}.BaseValue")) {
                using (w.B($"return (T) Make(typeof(T), value);")) { }
            }

            using (w.B($"public static {FactoryPrefix}.BaseValue Make(Type type, object value = null)")) {
                var typesDefined = new HashSet<string>();
                foreach (var type in ValueTypes) {
                    if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                        if (!typesDefined.Add(type.Name))
                            continue;
                        if (typeInfo.AllAttributes.Any(a => a.HasName("NoGeneratedMakeFromValue")))
                            continue;

                        if (type.Name is "Null" or "Unit") {
                            w._($"if (type == typeof({typeInfo.ValueType})) return {typeInfo.ClassType}.Make();");
                            continue;
                        }

                        w._($"if (type == typeof({typeInfo.ValueType})) return {typeInfo.ClassType}.Make(value);");
                    }
                }

                using (w.B($"if(type != null && type.IsAssignableTo(typeof({FactoryPrefix}.BaseValue)))")) {
                    typesDefined.Clear();

                    foreach (var type in ValueTypes) {
                        if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                            if (!typesDefined.Add(type.Name))
                                continue;
                            if (type.Name is "Null" or "Unit") {
                                w._($"if (type == typeof({FactoryPrefix}.{typeInfo.ClassType})) return {typeInfo.ClassType}.Make();");
                                continue;
                            }

                            w._($"if (type == typeof({FactoryPrefix}.{typeInfo.ClassType})) return {typeInfo.ClassType}.Make(value);");
                        }
                    }
                }
                using (w.B($"if(type != null && type.IsAssignableTo(typeof(CSScriptingLang.RuntimeValues.Types.RuntimeType)))")) {
                    typesDefined.Clear();

                    foreach (var type in ValueTypes) {
                        if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                            if (!typesDefined.Add(type.Name))
                                continue;
                            if (type.Name is "Null" or "Unit") {
                                w._($"if (type == typeof(CSScriptingLang.RuntimeValues.Types.{typeInfo.RuntimeType})) return {typeInfo.ClassType}.Make();");
                                continue;
                            }

                            w._($"if (type == typeof(CSScriptingLang.RuntimeValues.Types.{typeInfo.RuntimeType})) return {typeInfo.ClassType}.Make(value);");
                        }
                    }
                }

                w._("throw new ArgumentException($\"Unknown RTVT: {type}\", nameof(type));");
            }

            using (w.B($"public static {FactoryPrefix}.BaseValue Make(object value)")) {
                w._($"if (value is null) return Null.Zero();");

                using (w.B($"switch(value)")) {
                    var typesDefined = new HashSet<string>();
                    foreach (var type in ValueTypes) {
                        if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                            if (!typesDefined.Add(type.Name))
                                continue;
                            if (type.Name is "Null" or "Unit") {
                                continue;
                            }

                            w._($"case {typeInfo.ValueType} v{type.Name}: return {typeInfo.ClassType}.Make(v{type.Name});");
                        }
                    }

                }

                w._("throw new ArgumentException($\"Unknown value type: {value.GetType()}\", nameof(value));");
            }
            
            using (w.B($"public static {FactoryPrefix}.BaseValue Make(RTVT type, object value = null)")) {
                using (w.B($"switch(type)")) {
                    var typesDefined = new HashSet<string>();
                    foreach (var type in ValueTypes) {
                        if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                            if (!typesDefined.Add(type.Name))
                                continue;
                            if (type.Name is "Null" or "Unit") {
                                w._($"case {typeInfo.RTVT}: return {typeInfo.ClassType}.Make();");
                                continue;
                            }

                            w._($"case {typeInfo.RTVT}: return {typeInfo.ClassType}.Make(value);");
                        }
                    }

                }

                w._("throw new ArgumentException($\"Unknown RTVT: {type}\", nameof(type));");
            }
            using (w.B($"public static object NativeZeroValue(RTVT type)")) {
                using (w.B($"switch(type)")) {
                    var typesDefined = new HashSet<string>();
                    foreach (var type in ValueTypes) {
                        if (BaseTypeInfos.TryGetValue(type.Name, out var typeInfo)) {
                            if (!typesDefined.Add(type.Name))
                                continue;
                            if (type.Name == "Null") {
                                w._($"case {typeInfo.RTVT}: return null;");
                                continue;
                            }

                            w._($"case {typeInfo.RTVT}: return {typeInfo.ClassType}.NativeZero();");
                        }
                    }

                }

                w._("throw new ArgumentException($\"Unknown RTVT: {type}\", nameof(type));");
            }

            // w._($"public static T Make<T>(object value) where T : {FactoryPrefix}.BaseValue => (T) Make(value);");

            foreach (var classDeclaration in classes) {
                var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(classDeclaration);
                if (classSymbol is null)
                    continue;

                var className = classSymbol.Name;
                var typeInfo  = TypeInfos[className];

                using (w.B($"public static class {className}")) {

                    var getNativeZero = classDeclaration.Members
                       .OfType<MethodDeclarationSyntax>()
                       .FirstOrDefault(m => m.Identifier.Text == "GetNativeZero");
                    var definedNativeZero = false;
                    switch (className) {
                        case "Null": {
                            w._($"public static {FactoryPrefix}.Null Zero() => new {FactoryPrefix}.Null();");
                            w._($"public static {FactoryPrefix}.Null Value() => new {FactoryPrefix}.Null();");
                            w._($"public static object NativeZero() => {(getNativeZero == null ? "null" : "GetNativeZero()")};");
                            definedNativeZero = true;
                            break;
                        }
                        case "ValueBoolean": {
                            w._($"public static {FactoryPrefix}.ValueBoolean Zero() => new {FactoryPrefix}.ValueBoolean(false);");
                            w._($"public static {FactoryPrefix}.ValueBoolean True()  => new {FactoryPrefix}.ValueBoolean(true);");
                            w._($"public static {FactoryPrefix}.ValueBoolean False() => new {FactoryPrefix}.ValueBoolean(false);");
                            w._($"public static bool NativeZero() => {(getNativeZero == null ? "false" : "GetNativeZero()")};");
                            definedNativeZero = true;
                            break;
                        }
                        case "String": {
                            w._($"public static {FactoryPrefix}.String Zero() => new {FactoryPrefix}.String(string.Empty);");
                            w._($"public static string NativeZero() => {(getNativeZero == null ? "string.Empty" : "GetNativeZero()")};");
                            definedNativeZero = true;
                            break;
                        }
                        case "Object": {
                            w._($"public static {FactoryPrefix}.Object Zero() => new {FactoryPrefix}.Object(new CSScriptingLang.RuntimeValues.Values.ObjectDictionary());");
                            w._(
                                $"public static Dictionary<string, ValueTypes.BaseValue> NativeZero() => {(getNativeZero == null ? "new CSScriptingLang.RuntimeValues.Values.ObjectDictionary()" : "GetNativeZero()")};");
                            definedNativeZero = true;
                            break;
                        }
                    }

                    if (className.StartsWith("Number")) {
                        w._($"public static {FactoryPrefix}.{className} Zero() => new {FactoryPrefix}.{className}(0);");
                        w._($"public static {typeInfo.ValueType} NativeZero() => {(getNativeZero == null ? "0" : "GetNativeZero()")};");
                        definedNativeZero = true;
                    }

                    if (getNativeZero != null && !definedNativeZero) {
                        using (w.B($"public static object NativeZero()")) {
                            w._($"return {FactoryPrefix}.{className}.GetNativeZero();");
                        }
                    }

                    w._($"public static {FactoryPrefix}.{className} Make() => new {FactoryPrefix}.{className}();");
                    w._($"public static {FactoryPrefix}.{className} Make({typeInfo.RuntimeType} value) => new {FactoryPrefix}.{className}(value);");
                    w._($"public static {FactoryPrefix}.{className} Make({typeInfo.ValueType} value) => new {FactoryPrefix}.{className}(value);");
                    if (className != "Null" && className != "Unit") {
                        w._(
                            $"public static {FactoryPrefix}.{className} Make(object value) " +
                            $"=> value == null ? Make() : new {FactoryPrefix}.{className}(({typeInfo.ValueType})value);"
                        );
                    }

                }


            }

        }

        var src = w.ToString();
        src = src.Replace("Dictionary<string, BaseValue>", "Dictionary<string, ValueTypes.BaseValue>");
        src = src.Replace("<BaseValue>", "<ValueTypes.BaseValue>");

        context.AddSource($"Values.Factory.g.cs", SourceText.From(src, Encoding.UTF8));
    }

    private void ExecuteClassCastAndOperatorGeneration(Compilation compilation, List<ClassDeclarationSyntax> classes, SourceProductionContext context) {

        var w = new Writer()
           .WithNamespace("CSScriptingLang.RuntimeValues.Values")
           .WithImports("CSScriptingLang.RuntimeValues.Types")
           .WithImports("CSScriptingLang.Parsing.AST")
           .WithImports("System.Collections.Generic");

        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;
            var classType = model.GetTypeInfo(classDeclaration).Type;

            var className = classSymbol.Name;

            var typeInfo  = TypeInfos[className];
            var castsData = typeInfo.Casts;

            if (castsData.Count == 0)
                continue;

            using (w.B($"public partial class {className}")) {

                var allOperators = castsData.SelectMany(c => c.MathOperators).Distinct().ToList();

                /*
                void Operator(string op) {
                    using (w.B($"public static {className} operator {op}({className} left, BaseValue other)")) {
                        using (w.B($"if (other is null)")) {
                            w._($"return left;");
                        }

                        var intermediate = typeInfo.ValueType;
                        if(typeInfo.MathOperators.TryGetValue(op, out var mathOpInfo)) {
                            intermediate = mathOpInfo.CastToIntermediary;
                        }

                        using (w.B($"if(left.Type == other.Type)")) {
                            w._($"return new {className}(({intermediate})left.Value {op} ({intermediate})other.GetUntypedValue());");
                        }

                        using (w.B($"return other switch")) {

                            foreach (var t in castsData) {
                                if (op == "/") {
                                    w._($"{t.CastTo} c when ({intermediate})c.Value == 0 => throw new DivideByZeroException(),");
                                }

                                w._($"{t.CastTo} c => new {className}(({intermediate})left.Value {op} ({intermediate})c.Value),");
                            }

                            w._($"_ => throw new ArgumentException($\"Cannot {op} {className} and {{other.GetType()}}\"),");
                        }

                        w._(";");
                    }
                }


                if (allOperators.Contains("+"))
                    Operator("+");
                if (allOperators.Contains("-"))
                    Operator("-");
                if (allOperators.Contains("*"))
                    Operator("*");
                if (allOperators.Contains("/"))
                    Operator("/");
                if (allOperators.Contains("%"))
                    Operator("%");
                    */

                using (w.B("public override bool CanCastTo<T>()")) {
                    w._("return CanCastTo(typeof(T));");
                }
                using (w.B("public override bool CanCastTo(Type t)")) {
                    w._("if (base.CanCastTo(t)) return true;");
                    w._($"if (t == typeof({className})) return true;");

                    foreach (var t in castsData) {
                        w._($"if (t == typeof({t.CastTo})) return true;");
                    }

                    w._("return false;");
                }

                using (w.B("public override T CastTo<T>()")) {
                    w._("return (T) CastTo(typeof(T));");
                }
                
                using (w.B("public override BaseValue CastTo(Type t)")) {
                    w._($"var baseResult = base.CastTo(t);");
                    w._($"if (baseResult != null) return baseResult;");
                    w._($"if (t == typeof({className})) return this;");

                    foreach (var t in castsData) {
                        w._($"if (t == typeof({t.CastTo})) return new {t.CastTo}(Value);");
                    }

                    w._($"throw new ArgumentException($\"Cannot cast {className} to {{t}}\");");
                }

            }

        }

        context.AddSource($"Values.CastsAndOperators.g.cs", SourceText.From(w.ToString(), Encoding.UTF8));
    }
    private void ExecuteClassAdditionalGeneration(Compilation compilation, List<ClassDeclarationSyntax> classes, SourceProductionContext context) {

        var w = new Writer()
           .WithNamespace("CSScriptingLang.RuntimeValues.Values")
           .WithImports("CSScriptingLang.RuntimeValues.Types")
           .WithImports("CSScriptingLang.Parsing.AST")
           .WithImports("System.Collections.Generic");

        foreach (var classDeclaration in classes) {
            var model       = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = model.GetDeclaredSymbol(classDeclaration);
            if (classSymbol is null)
                continue;
            var classType = model.GetTypeInfo(classDeclaration).Type;

            var className = classSymbol.Name;

            var typeInfo = TypeInfos[className];

            var definedFields = classDeclaration.Members
                   .OfType<PropertyDeclarationSyntax>()
                   .Select(p => new {
                        Name = p.Identifier.Text,
                        Type = model.GetTypeInfo(p.Type).Type
                    })
                   .ToDictionary(p => p.Name, p => p.Type)
                ;


            using (w.B($"public partial class {className}")) {
                if (!definedFields.ContainsKey("Type")) {
                    w._($"public override RTVT Type => {typeInfo.RTVT};");
                }

                if (!definedFields.ContainsKey("RuntimeValueType")) {
                    w._($"public override Type RuntimeValueType => typeof({typeInfo.RuntimeType});");
                }

                if (!definedFields.ContainsKey("ValueType")) {
                    w._($"public override Type ValueType => typeof({typeInfo.ValueType});");
                }

                if (!definedFields.ContainsKey("RuntimeType")) {
                    using (w.B($"public new {typeInfo.RuntimeType} RuntimeType")) {
                        w._($"get => ({typeInfo.RuntimeType}) base.RuntimeType;");
                        w._($"set => base.RuntimeType = value;");
                    }
                }

                var ctors = classDeclaration.Members.OfType<ConstructorDeclarationSyntax>().ToList();

                if (ctors.All(c => c.ParameterList.Parameters.Count != 0)) {
                    using (w.B($"public {className}()")) { }
                }

                if (!ctors
                       .Any(c => c.ParameterList.Parameters.Count == 1 && c.ParameterList.Parameters[0].Type!.ToString() == typeInfo.ValueType)) {
                    using (w.B($"public {className}({typeInfo.ValueType} value) : base(value)")) { }
                }

                if (!ctors
                       .Any(c => c.ParameterList.Parameters.Count == 1 && c.ParameterList.Parameters[0].Type!.ToString() == typeInfo.RuntimeType)) {
                    using (w.B($"public {className}({typeInfo.RuntimeType} value) : base(value)")) { }
                }

                if (className != "Null") {
                    if (!typeInfo.AllAttributes.Any(a => a.HasName("NoGeneratedConversionOperators"))) {
                        w._($"public static explicit operator {className}({typeInfo.ValueType} value) => new {className}(value);");
                        w._($"public static explicit operator {typeInfo.ValueType}({className} value) => value.Value;");
                    }
                }
            }

        }

        context.AddSource($"Values.Extensions.g.cs", SourceText.From(w.ToString(), Encoding.UTF8));
    }

    public const string FactoryPrefix = "ValueTypes";

    public struct ValueAttribute
    {
        public AttributeSyntax Attribute    { get; set; }
        public TypeInfo        TypeInfo     { get; set; }
        public string          ClassType    { get; set; }
        public string          RuntimeType  { get; set; }
        public string          ValueType    { get; set; }
        public string          RTVT         { get; set; }
        public string          PrefixedName => $"{FactoryPrefix}.{ClassType}";

        public string AttributeName { get; set; }
    }

    public struct CastAttribute
    {
        public string   CastTo        { get; set; }
        public string   CastFrom      { get; set; }
        public string[] MathOperators { get; set; }

        public TypeInformation Info => TypeInfos[CastTo];
    }
}