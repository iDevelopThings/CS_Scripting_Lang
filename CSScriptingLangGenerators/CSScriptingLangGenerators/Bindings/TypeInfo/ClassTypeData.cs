using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using CSScriptingLangGenerators.Utils.CodeWriter;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

[Flags]
public enum AllowedInstanceType
{
    None     = 0,
    Instance = 1,
    Static   = 2,
    Both     = Instance | Static
}

[DebuggerDisplay("ClassTypeData: {Name}")]
public class ClassTypeData : Writer
{
    public INamedTypeSymbol          Symbol  { get; set; }
    public GeneratorExecutionContext Context { get; set; }

    public string Name             { get; set; }
    public string Qualifier        { get; set; }
    public bool   IsDataObjectBind { get; set; }
    public string BoundToModule    { get; set; }

    public List<Method> Constructors            { get; set; } = new();
    public List<Method> ValueConstructorMethods { get; set; } = new();

    public List<PropertyBind> Properties   { get; set; } = new();
    public List<PropertyBind> Getters      { get; set; } = new();
    public List<MethodData>   GettersTable { get; set; }

    public List<Method> Methods   { get; set; } = new();
    public List<Method> Operators { get; set; } = new();
    // Methods + Operators
    public List<Method>     AllMethods           { get; set; } = new();
    public List<MethodData> MethodTable          { get; set; } = new();
    public List<MethodData> InstanceGettersTable { get; set; } = new();

    protected List<MethodKind> AllowedConstructorMethodKinds = [MethodKind.Constructor];

    protected AllowedInstanceType AllowMethodInstanceType   { get; set; } = AllowedInstanceType.Both;
    protected AllowedInstanceType AllowPropertyInstanceType { get; set; } = AllowedInstanceType.Both;

    public TypeMeta_ClassBased Meta { get; set; }

    public ClassTypeData(INamedTypeSymbol klass, GeneratorExecutionContext context, Writer writer) : base(writer) {
        Context       = context;
        Symbol        = klass;
        Name          = klass.Name;
        Qualifier     = $"global::{klass.GetFullyQualifiedName()}";
        BoundToModule = klass.GetAttributeArgument<string>(Attributes.BindToModule, null);
        CreationTime  = DateTime.Now;
    }
    public DateTime CreationTime { get; set; }


    public static ClassTypeData ForClassBinding(INamedTypeSymbol klass, GeneratorExecutionContext context, Writer writer) {
        var data = new ClassTypeData(klass, context, writer) {
            Name                      = klass.GetAttributeArgument<string>(Attributes.Class, klass.Name),
            IsDataObjectBind          = klass.HasAttribute(Attributes.ClassDataObject),
            AllowPropertyInstanceType = AllowedInstanceType.Instance,
            AllowMethodInstanceType   = AllowedInstanceType.Both,
        };

        data.Meta = new TypeMeta_Class() {
            Name      = klass.Name,
            Module    = data.BoundToModule,
            Namespace = klass.GetFullyQualifiedName(),
            Kind      = TypeMetaKind.Class,
        };

        data.Load();

        return data;
    }


    public string GetFullyQualifiedName() => Symbol.GetFullyQualifiedName();

    protected void Load() {
        LoadConstructors();
        LoadProperties();
        LoadPropertyGetters();
        LoadMethods();
        LoadOperators();

        // Remove any operator methods from the methods list
        Methods = Methods.Where(m => Operators.All(o => o.Name != m.Name)).ToList();

        AllMethods = Methods.Concat(Operators).ToList();

        MethodTable = AllMethods.Concat(Constructors)
           .GroupBy(m => m.Name)
           .Select(g => MethodData.BuildMethodTable(Context, g))
           .ToList();

        Meta.AddMethods(AllMethods.Select(m => MethodData.BuildMethodTable(Context, new[] {m})).ToList());
        // Meta.AddMethods(Methods);
        Meta.AddConstructors(Constructors);
        Meta.AddProperties(Properties);
        Meta.AddProperties(Getters, true);
    }

    public void WritePropertyBindings(
        string objVarName,
        string bindFnInstance    = null,
        bool   writeMethodTables = false,
        string typeVarName       = "type",
        bool   bindTypes         = false
    ) {
        bindFnInstance = bindFnInstance != null ? bindFnInstance + "." : null;

        foreach (var property in Properties) {
            var pName = bindFnInstance + property.Name;

            if (property.GetMethod != null) {
                WritePropertyDefinition(objVarName, property.BindingGetterName, $"{pName}__Getter");

                if (bindTypes) {
                    WriteTypeBindDefinition(typeVarName, property.BindingGetterName, property.Type, property.Property);
                    NewLine();
                }

            }

            if (property.SetMethod != null) {
                WritePropertyDefinition(objVarName, property.BindingSetterName, $"{pName}__Setter");

                if (bindTypes) {
                    WriteTypeBindDefinition(typeVarName, property.BindingSetterName, property.Type, property.Property);
                    NewLine();
                }
            }
        }

        if (writeMethodTables) {
            foreach (var table in MethodTable) {
                var mName = bindFnInstance + table.Identifier;
                WritePropertyDefinition(objVarName, table.Identifier, $"{mName}__Dispatch");

                if (bindTypes) {
                    WriteTypeBindDefinition(typeVarName, table.Identifier, table.ReturnType, table.FirstMethod.Info);
                    NewLine();
                }
            }
        }

        WriteGetters(objVarName, GettersTable);
    }

    protected void WriteGetters(
        string           objVarName,
        List<MethodData> tables,
        string           typeVarName = "type",
        bool             bindTypes   = false
    ) {
        foreach (var table in tables) {
            var method = table.GetFirstMethod();

            using (B($"Value __get_{method.Name}(ExecContext ctx, Value instance)")) {
                if (method.Info.ContainingType.HasAttribute(Attributes.ClassDataObject))
                    WriteMethodChecks(method.Name, "instance", this);
                
                _();
                if (method.IsPropertyGetter) {
                    _(
                        $"var result = obj.{method.Name};",
                        $"return {BindingsGenerator.ConvertToValue(Context, "result", method.Info.ReturnType, method.Info)};"
                    );
                } else {
                    _(
                        $"var result = {method.Info.ContainingType.GetFullyQualifiedName()}.{method.Info.Name}(",
                        method.GetArgumentBindings(Context, 100).Select(b => $"{GetIndentString()}{b}").Join(", \n"),
                        ");",
                        $"return {BindingsGenerator.ConvertToValue(Context, "result", method.Info.ReturnType, method.Info)};"
                    );
                }

            }
            _($"// table.Identifier: => {table.Identifier}");
            _($"// table.Name: => {table.Name}");
            _($"// method.Identifier: => {method.Identifier}");
            _($"// method.Name: => {method.Name}");
            _($"{objVarName}[\"{table.Identifier}\"] = Value.InstanceGetterFunction(\"{table.Identifier}\", __get_{method.Name});");

            if (bindTypes)
                WriteTypeBindDefinition(typeVarName, table.Identifier, method.ReturnType, method.Info);
        }
    }

    public void WriteMethodBindings(string objVarName) {
        foreach (var table in MethodTable) {
            _($"{objVarName}[\"{table.Identifier}\"] = Value.Function(\"{table.Identifier}\", {table.Identifier}__Dispatch);");
        }
    }

    public void WriteConstructorDefinition() {
        if (Constructors.Any(m => m.Identifier == "__ctor")) {
            WriteDefinition(Name, $"{Name}__ctor", "__ctor__Dispatch");
        }
    }

    public void WriteDefinition(string name, string fnName, string bindFnName) {
        yieldReturn(
            $"new KeyValuePair<string, Value>(",
            $"\"{name}\",",
            $"Value.Function(\"{fnName}\", {bindFnName})",
            ")"
        );
        // yieldReturn($"new KeyValuePair<string, Value>(\"{name}\", Value.Function(\"{fnName}\", {bindFnName}))");
    }
    public void WritePropertyDefinition(string objVarName, string name, string bindFnName)
        => WritePropertyDefinition(objVarName, name, name, bindFnName);

    public void WritePropertyDefinition(string objVarName, string name, string fnName, string bindFnName) {
        _($"{objVarName}[\"{name}\"] = Value.Function(\"{fnName}\", {bindFnName});");
    }

    public void WriteTypeBindDefinition(string varName, string name, ITypeSymbol symbol, ISymbol typeSource = null) {
        _($"{varName}[\"{name}\"] = {BindingsGenerator.ConvertToType(Context, name, symbol, typeSource)};");

        // _($"{varName}[\"{name}\"] = Value.Function(\"{fnName}\", {bindFnName});");
    }

    protected void LoadMethods() {

        var members = Symbol.GetMembers()
           .OfType<IMethodSymbol>()
           .Where(
                m => m.IsStatic switch {
                    true when AllowPropertyInstanceType.HasFlag(AllowedInstanceType.Static)    => true,
                    false when AllowPropertyInstanceType.HasFlag(AllowedInstanceType.Instance) => true,
                    _                                                                          => false,
                }
            )
           .ToList();

        foreach (var method in members) {
            var attributes  = method.GetAttributes();
            var hasFuncAttr = attributes.TryGetAttribute([Attributes.Function, Attributes.GlobalFunction], out var funcAttr);

            if (!hasFuncAttr) {
                continue;
            }

            if (method.DeclaredAccessibility != Accessibility.Public) {
                Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }
            var name = (funcAttr.GetArgument<string>() ?? method.Name);
            name = name.ToIdentifierCasing();
            Methods.Add(
                new Method(Context, name, name, method)
            );
        }

    }

    protected void LoadOperators() {

        var members = Symbol.GetMembers()
           .OfType<IMethodSymbol>()
           .Where(m => m.Name.ToLower().StartsWith("operator_"))
           .Where(m => m.HasAttribute(Attributes.Operator))
           .ToList();

        foreach (var method in members) {
            if (!method.IsStatic) {
                Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.OperatorOverloadMethodsShouldBeStatic, method.Locations.First()));
                continue;
            }

            // ensure there's a `Value instance` parameter
            if (method.Parameters.Length == 0 || !SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, TypeData.Value)) {
                Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.OperatorOverloadMethodFirstParameterShouldBeInstance, method.Locations.First()));
                continue;
            }
            if (method.DeclaredAccessibility != Accessibility.Public) {
                Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, method.Locations.First()));
                continue;
            }

            var opName = method.Name["operator_".Length..].ToLower();
            if (TypeData.OperatorsByIdent.TryGetValue(opName, out var opData)) {
                Operators.Add(
                    new Method(Context, opData.OperatorOverloadFnName, opData.OperatorOverloadFnName, method)
                );
            } else {
                Context.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.OperatorNotSupported,
                        method.Locations.First(),
                        opName,
                        string.Join(", ", TypeData.OperatorsByIdent.Values.Select(o => $"'{o.OperatorOverloadFnName}'({o.Token})"))
                    )
                );
            }


        }

    }

    protected void LoadProperties() {
        var members = Symbol.GetMembers()
           .OfType<IPropertySymbol>()
           .Where(
                m => m.IsStatic switch {
                    true when AllowPropertyInstanceType.HasFlag(AllowedInstanceType.Static)    => true,
                    false when AllowPropertyInstanceType.HasFlag(AllowedInstanceType.Instance) => true,
                    _                                                                          => false,
                }
            );

        foreach (var member in members) {
            PropertyBind setter = null;
            PropertyBind getter = null;

            if (member.TryGetAttribute([Attributes.PropertyGetter], out var getterAttr)) {

                if (member.DeclaredAccessibility != Accessibility.Public) {
                    Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, member.Locations.First()));
                    continue;
                }
                var getterName = getterAttr.GetArgument<string>() ?? member.Name;
                var usePrefix  = getterAttr.GetArgument<bool>(1);

                // p.Info.GetAttributeArgument<string>(Attributes.MetaDefinition)

                getter = new PropertyBind(member, Context.Compilation) {
                    Name            = member.Name,
                    Identifier      = getterName,
                    UseGetterPrefix = usePrefix,
                    HasGetter       = true,
                    HasSetter       = false,
                    Property        = member,
                };

                // Properties.Add();

                Getters.Add(getter);
            }

            if (member.TryGetAttribute([Attributes.PropertySetter], out var setterAttr)) {

                if (member.DeclaredAccessibility != Accessibility.Public) {
                    Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, member.Locations.First()));
                    continue;
                }
                var setterName = setterAttr.GetArgument<string>() ?? member.Name;
                var usePrefix  = setterAttr.GetArgument<bool>(1);
                Properties.Add(
                    setter = new PropertyBind(member, Context.Compilation) {
                        Name            = member.Name,
                        Identifier      = setterName,
                        UseSetterPrefix = usePrefix,
                        HasGetter       = false,
                        HasSetter       = true,
                        Property        = member,
                    }
                );
            }

            if (getter != null && setter != null) {
                continue;
            }

            if (member.TryGetAttribute([Attributes.Function, Attributes.GlobalFunction], out var functionAttr)) {

                if (member.DeclaredAccessibility != Accessibility.Public) {
                    Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, member.Locations.First()));
                    continue;
                }

                var name      = functionAttr.GetArgument<string>() ?? member.Name;
                var hasGetter = member.GetMethod != null;
                var hasSetter = member.SetMethod != null;

                var p = new PropertyBind(member, Context.Compilation) {
                    Name      = name,
                    HasGetter = hasGetter,
                    HasSetter = hasSetter,
                    Property  = member,
                };

                Properties.Add(p);
            }

        }

        GettersTable = Getters
           .Select(
                p => {
                    return MethodData.BuildMethodTable(
                        Context,
                        [
                            new Method(Context, p.Name, p.Identifier, p.Property.GetMethod) {
                                IsPropertyGetter = true,
                            },
                        ]
                    );
                }
            )
           .ToList();
    }

    // These are basically functions which bind like properties
    protected void LoadPropertyGetters() {
        var members = Symbol.GetMembers()
           .OfType<IMethodSymbol>()
           .Where(m => m.MethodKind == MethodKind.Ordinary)
           .Where(m => m.IsStatic)
           .Where(m => m.HasAttribute(Attributes.InstanceGetterFunction))
           .Where(m => m.DeclaredAccessibility == Accessibility.Public)
           .Select(
                m => {
                    var attr = m.GetAttribute(Attributes.InstanceGetterFunction);
                    return (m, m.Name, attr.GetArgument<string>() ?? m.Name);
                }
            )
           .ToList();

        InstanceGettersTable = MethodData.Build(Context, members);
    }



    protected void LoadConstructors() {
        var members = Symbol.GetMembers()
           .OfType<IMethodSymbol>()
           .Where(m => AllowedConstructorMethodKinds.Contains(m.MethodKind))
           .Where(m => m.HasAttribute(Attributes.Constructor));

        foreach (var member in members) {
            if (member.DeclaredAccessibility != Accessibility.Public) {
                Context.ReportDiagnostic(Diagnostic.Create(Diagnostics.BoundMembersMustBePublic, member.Locations.First()));
                continue;
            }

            var ctor = new Method(Context, "#ctor", "__ctor", member) {
                IsConstructor = true,
            };

            if (member.IsStatic) {
                ValueConstructorMethods.Add(ctor);
            }

            Constructors.Add(ctor);
        }
    }

    protected virtual void WriteMethodChecks(string methodName, string instanceName = "instance", Writer w = null) {
        w ??= this;

        // using (w.B($"if ({instanceName}.Type != RTVT.Object)")) {
        //     w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
        // }

        {
            w._($"{Qualifier} obj = null;");

            var isSubValue = Symbol.HasBaseType(TypeData.Value);

            using var ifChain = w.IfChain();

            ifChain.Condition($"{instanceName} is WrappedValue<{Qualifier}> wv")
               .Then(bw => bw._($"obj = wv.Value;"));

            ifChain.OptionalElseIf(
                isSubValue,
                $"{instanceName} is {Qualifier}",
                (b, bw) => bw._($"obj = ({Qualifier}){instanceName};")
            );

            ifChain.OptionalElseIf(
                !isSubValue,
                $"{instanceName}?.GetUntypedValue() is {Qualifier} _untypedValue",
                (b, bw) => bw._($"obj = _untypedValue;")
            );

            ifChain.ElseIf(
                $"{instanceName}?.DataObject is {Qualifier} _do",
                bw => bw._($"obj = _do;")
            );
        }

        w._($"if (obj == null) throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");

        /*using (w.B($"if ({instanceName} is not WrappedValue<{Qualifier}> wv)")) {
            w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
        }

        if (Symbol.HasBaseType(TypeData.Value)) {
            using (w.B($"if ({instanceName} is not {Qualifier} obj)")) {
                w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
            }
            return;
        }

        using (w.B($"if ({instanceName}?.GetUntypedValue() is not {Qualifier} obj)")) {
            w._($"throw new InterpreterRuntimeException(\"{Name}.{methodName}: can only be called on an instance of {Name}\");");
        }*/
    }

    public void WritePropertyDefinitions() {
        foreach (var property in Properties) {
            if (property.GetMethod != null) {
                using (B($"public static Value {property.Name}__Getter(FunctionExecContext ctx, Value instance, params Value[] args)")) {
                    WriteMethodChecks($"get{property.Name}");
                    _();

                    using (B("if (args.Length != 0)")) {
                        _($"throw new InterpreterRuntimeException(\"{Name}.get{property.Name}: expected 0 arguments\");");
                    }

                    _($"var value = obj.{property.Name};");

                    _($"return {BindingsGenerator.ConvertToValue(Context, "value", property.Type, property.Property)};");
                }
            }

            if (property.SetMethod != null) {
                var parameter = Parameter.Create(Context, property.SetMethod.Parameters[0]);

                using (B($"public static Value {property.Name}__Setter(FunctionExecContext ctx, Value instance, params Value[] args)")) {

                    WriteMethodChecks($"set{property.Name}");
                    _();

                    // if (parameter.RequiresParamTypeCheck()) {
                    parameter.WriteTypeCheck(0, this);
                    // }

                    // using (B($"if (args.Length != 1 || !{BindingsGenerator.CompareArgument(0, parameter)})")) {
                    //     _($"throw new InterpreterRuntimeException(\"{Name}.set{property.Name}: expected 1 argument of type {parameter.TypeName}\");");
                    // }

                    _($"obj.{property.Name} = {BindingsGenerator.ConvertFromValue(Context, 0, property.Type, property.Property)};");

                    _("return Value.Null();");
                }

                _();
            }
        }
    }

    public void WriteMethodDefinitions(bool staticMethods = true, bool paramsMethods = false) {
        foreach (var table in MethodTable) {

            using var mw = MethodWriter($"{table.Identifier}__Dispatch")
               .Static(staticMethods)
               .WithReturnType("Value")
               .WithParameter("ctx", "FunctionExecContext")
               .WithParameterIf("instance", "Value", () => table.RequiresInstanceParameter())
               .WithParameter("args", "params Value[]")
               .Body(
                    writer => {
                        if (
                            table.RequiresInstanceParameter()
                         && table.Name != "#ctor"
                         && !table.IsStatic()
                        ) {
                            WriteMethodChecks(table.Name, "instance", writer);
                            _();
                        }

                        var ignoreChecks = table.IgnoreParamChecks() && table.ParamsMethods.Count > 0;
                        if (!ignoreChecks) {
                            using (writer.B("switch (args.Length)")) {
                                for (var i = 0; i < table.Methods.Count; i++) {
                                    var tableMethods = table.Methods[i];
                                    if (tableMethods.Count == 0) {
                                        continue;
                                    }

                                    using (writer.B($"case {i}:")) {
                                        var hasTrueType = false;
                                        foreach (var method in tableMethods) {

                                            // WriteMethodChecks(w, method.Name);

                                            method.WriteParameterChecks(writer, method, out var isTrueType, i);

                                            if (isTrueType)
                                                hasTrueType = true;

                                            method.WriteCall(Context, writer, "obj", i);
                                        }

                                        if (!hasTrueType)
                                            writer._("break;");
                                    }
                                }
                            }
                        }

                        if ((paramsMethods || table.IgnoreParamChecks()) && table.ParamsMethods.Count > 0) {
                            using (B()) {
                                foreach (var method in table.ParamsMethods) {
                                    method.WriteParameterChecks(writer, method);
                                    method.WriteCall(Context, writer, "obj");

                                    // using (B($"if (args.Length >= {method.RequiredParameterCount} && {CompareArguments(method)})")) {
                                    //     CallMethod(context, w, "obj", method);
                                    // }
                                }
                            }
                        }

                        writer._();
                        var errorPrefix  = $"{Name}.{table.Name}: ";
                        var errorMessage = BindingsGenerator.GetMethodNotMatchedErrorMessage(errorPrefix, table);
                        writer._($"throw new InterpreterRuntimeException(\"{BindingsGenerator.EscapeForStringLiteral(errorMessage)}\");");

                    }
                );

            _();
        }
    }
    public void WriteMethodDefinitionss(Writer w) {
        foreach (var table in MethodTable) {

            var methodStr = $"public static Value {table.Identifier}__Dispatch(";
            methodStr += $"FunctionExecContext ctx, ";

            if (table.RequiresInstanceParameter())
                methodStr += "Value instance, ";

            methodStr += "params Value[] args)";

            using (w.B(methodStr)) {

                using (w.B("switch (args.Length)")) {
                    for (var i = 0; i < table.Methods.Count; i++) {
                        var tableMethods = table.Methods[i];
                        if (tableMethods.Count == 0) {
                            continue;
                        }

                        using (w.B($"case {i}:")) {
                            foreach (var method in tableMethods) {

                                for (var paramIdx = 0; paramIdx < method.Parameters.Count; paramIdx++) {
                                    var param = method.Parameters[paramIdx];
                                    param.WriteTypeCheck(paramIdx, w);
                                }

                                method.WriteCall(Context, w, Qualifier, i);
                            }

                            w._("break;");
                        }
                    }
                }

                foreach (var method in table.ParamsMethods) {
                    using (w.B($"if (args.Length >= {method.RequiredParameterCount})")) {
                        for (var paramIdx = 0; paramIdx < method.Parameters.Count; paramIdx++) {
                            var param = method.Parameters[paramIdx];
                            param.WriteTypeCheck(paramIdx, w);
                        }

                        method.WriteCall(Context, w, Qualifier);
                    }
                }

                w._();

                var errorMessage = BindingsGenerator.GetMethodNotMatchedErrorMessage($"{Name}.{table.Name}: ", table);
                w._($"throw new InterpreterRuntimeException(\"{BindingsGenerator.EscapeForStringLiteral(errorMessage)}\");");
            }
            w._();
        }

    }

}