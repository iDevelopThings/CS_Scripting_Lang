using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Bindings;

public enum TypeMetaKind
{
    Class,
    Module,
    Prototype,
}

public class TypeMeta_Method
{
    public string            Name;
    public string            Definition;
    public DocumentationData Documentation;

    public struct Parameter
    {
        public string Name;
        public string Type;
        public bool   IsVariadic;

        public (string name, string type, bool isVariadic) ToTuple() {
            return (Name, Type, IsVariadic);
        }

        public static implicit operator (string name, string type, bool isVariadic)(Parameter p) => p.ToTuple();

        public static implicit operator Parameter((string name, string type, bool isVariadic) p) => new() {
            Name       = p.name,
            Type       = p.type,
            IsVariadic = p.isVariadic,
        };
    }

    public List<Parameter> Parameters { get; set; } = new();

    public ValueTypeHint ReturnType { get; set; }
    public bool          IsAsync    { get; set; }
    public bool          IsGlobal   { get; set; }

    public TypeMeta_Method(ISymbol symbol) {
        Name          = symbol.Name;
        Documentation = symbol.GetDocumentationData();
    }
    public TypeMeta_Method(ISymbol symbol, string definition) : this(symbol) {
        Definition = definition;
    }
    public TypeMeta_Method(ISymbol symbol, string name, string definition) : this(symbol) {
        Name       = name;
        Definition = definition;
    }

    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Definition);
    }
}

public class TypeMeta_Property : TypeMeta_Method
{
    public bool IsInstanceGetterProperty;
    public bool IsGetter;
    public bool IsSetter;

    public ValueTypeHint Type { get; set; }
    public new string Definition {
        get {
            if (base.Definition != null)
                return base.Definition;

            if (Type != null) {
                return $"{Name} {Type.Name}";
            }
            return null;
        }
        set => base.Definition = value;
    }

    public TypeMeta_Property(ISymbol symbol) : base(symbol) { }
    public TypeMeta_Property(ISymbol symbol, string definition) : base(symbol, definition) { }
    public TypeMeta_Property(ISymbol symbol, string name, string definition) : base(symbol, name, definition) { }

}

public class TypeMeta_Constructor : TypeMeta_Method
{
    public TypeMeta_Constructor(ISymbol symbol) : base(symbol) { }
    public TypeMeta_Constructor(ISymbol symbol, string definition) : base(symbol, definition) { }
    public TypeMeta_Constructor(ISymbol symbol, string name, string definition) : base(symbol, name, definition) { }
}

[DebuggerDisplay("BaseTypeMeta({Name} -> {Kind}, BoundToModule: {Module})")]
public abstract class TypeMeta
{
    public string Name      { get; set; }
    public string Namespace { get; set; }
    public string Module    { get; set; }
    public string SuperType { get; set; }

    public TypeMetaKind Kind { get; set; }

    public string RelativePath {
        get {
            if (Kind == TypeMetaKind.Module) {
                return Name;
            }

            if (Kind == TypeMetaKind.Class) {
                return $"{Module}/{Name}";
            }

            if (Kind == TypeMetaKind.Prototype) {
                return $"Prototypes/{Name}";
            }

            throw new InvalidOperationException();
        }
    }

    public string Definition { get; set; }
    
    /*private sealed class TypeMetaEqualityComparer : IEqualityComparer<TypeMeta>
    {
        public bool Equals(TypeMeta x, TypeMeta y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name && x.Namespace == y.Namespace && x.Module == y.Module && x.Kind == y.Kind;
        }
        public int GetHashCode(TypeMeta obj) {
            return HashCode.Combine(obj.Name, obj.Namespace, obj.Module, (int) obj.Kind);
        }
    }
    public static IEqualityComparer<TypeMeta> TypeMetaComparer { get; } = new TypeMetaEqualityComparer();*/

}

[DebuggerDisplay("TypeMeta_ClassBased({Name}, BoundToModule: {Module}, Properties: {Properties.Count}, Constructors: {Constructors.Count}, Methods: {Methods.Count})")]
public class TypeMeta_ClassBased : TypeMeta
{
    public List<TypeMeta_Property>    Properties   { get; set; } = new();
    public List<TypeMeta_Constructor> Constructors { get; set; } = new();
    public List<TypeMeta_Method>      Methods      { get; set; } = new();
    public List<string>               Aliases      { get; set; } = new();

    public void AddMethods(List<MethodData> methods) {
        foreach (var methodTable in methods) {
            foreach (var methodGroup in methodTable.Methods) {
                foreach (var m in methodGroup) {
                    var def  = m.Info.GetAttributeArgument<string>(Attributes.MetaDefinition);
                    var meta = new TypeMeta_Method(m.Info, m.Name, def);

                    foreach (var p in m.Parameters) {
                        if (p.Type != ParameterType.Value && p.Type != ParameterType.Params) {
                            continue;
                        }
                        var param = new TypeMeta_Method.Parameter {
                            Name       = p.Name,
                            Type       = p.TypeHint.Name,
                            IsVariadic = p.Type == ParameterType.Params,
                        };

                        meta.Parameters.Add(param);
                    }

                    meta.ReturnType = m.ReturnTypeHint;
                    meta.IsAsync    = m.Info.IsAsync;
                    meta.IsGlobal   = m.IsGlobal;

                    Methods.Add(meta);
                }
            }
        }
    }
    public void AddConstructors(List<Method> constructors) {
        foreach (var m in constructors) {
            var def  = m.Info.GetAttributeArgument<string>(Attributes.MetaDefinition);
            var meta = new TypeMeta_Constructor(m.Info, m.Name, def);

            Constructors.Add(meta);
        }
    }
    public void AddProperties(List<PropertyBind> properties, bool isInstanceGetterFunctions = false) {
        foreach (var property in properties) {

            if (isInstanceGetterFunctions) {
                // Instance getter functions are both getters and setters
                var p = new TypeMeta_Property(property.Property) {
                    IsInstanceGetterProperty = true,
                    Name                     = property.Identifier,
                    Documentation            = property.GetMethodDocumentation ?? property.SetMethodDocumentation ?? property.Documentation,
                    Definition               = property.MetaDefinition,
                    IsGetter                 = true,
                    IsSetter                 = true,
                    Type                     = property.ValueTypeHint,
                };

                Properties.Add(p);
                continue;
            }

            if (property.HasGetter) {
                var p = new TypeMeta_Property(property.Property) {
                    Name          = property.BindingGetterName,
                    Documentation = property.GetMethodDocumentation ?? property.Documentation,
                    Definition    = property.GetMethodMetaDefinition ?? property.MetaDefinition,
                    IsGetter      = true,
                    Type          = property.ValueTypeHint,
                };

                Properties.Add(p);
            }

            if (property.HasSetter) {
                var p = new TypeMeta_Property(property.Property) {
                    Name          = property.BindingSetterName,
                    Documentation = property.SetMethodDocumentation,
                    Definition    = property.SetMethodMetaDefinition,
                    IsSetter      = true,
                    Type          = property.ValueTypeHint,
                };

                Properties.Add(p);
            }
        }

        // Properties = Properties
        // .Where(p => p.IsValid())
        // .ToList();

    }

    private sealed class TypeMetaClassBasedEqualityComparer : IEqualityComparer<TypeMeta_ClassBased>
    {
        public bool Equals(TypeMeta_ClassBased x, TypeMeta_ClassBased y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null) return false;
            if (y is null) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Name == y.Name && x.Namespace == y.Namespace && x.Module == y.Module && x.Kind == y.Kind && x.Definition == y.Definition && Equals(x.Properties, y.Properties) && Equals(x.Constructors, y.Constructors) && Equals(x.Methods, y.Methods);
        }
        public int GetHashCode(TypeMeta_ClassBased obj) {
            return HashCode.Combine(obj.Name, obj.Namespace, obj.Module, (int) obj.Kind, obj.Definition, obj.Properties, obj.Constructors, obj.Methods);
        }
    }

    public static IEqualityComparer<TypeMeta_ClassBased> TypeMetaClassBasedComparer { get; } = new TypeMetaClassBasedEqualityComparer();

}

[DebuggerDisplay("Module({Name}, Classes: {Classes.Count}, Prototypes: {Prototypes.Count}, Properties: {Properties.Count}, Constructors: {Constructors.Count}, Methods: {Methods.Count})")]
public class TypeMeta_Module : TypeMeta_ClassBased
{
    public List<TypeMeta_ClassBased> Classes    { get; set; } = new();
    public List<TypeMeta_ClassBased> Prototypes { get; set; } = new();

    public TypeMeta_Module() {
        Kind = TypeMetaKind.Module;
    }
}

[DebuggerDisplay("Prototype({Name}, BoundToModule: {Module}, Properties: {Properties.Count}, Constructors: {Constructors.Count}, Methods: {Methods.Count})")]
public class TypeMeta_Prototype : TypeMeta_ClassBased
{
    public TypeMeta_Prototype() {
        Kind = TypeMetaKind.Prototype;
    }
}

[DebuggerDisplay("Class({Name}, BoundToModule: {Module}, Properties: {Properties.Count}, Constructors: {Constructors.Count}, Methods: {Methods.Count})")]
public class TypeMeta_Class : TypeMeta_ClassBased
{
    public TypeMeta_Class() {
        Kind = TypeMetaKind.Class;
    }
}
