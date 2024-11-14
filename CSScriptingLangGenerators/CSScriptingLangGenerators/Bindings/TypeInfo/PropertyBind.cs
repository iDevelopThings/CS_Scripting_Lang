using System;
using System.Linq;
using System.Runtime.Serialization;
using CSScriptingLangGenerators.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace CSScriptingLangGenerators.Bindings;

public class ValueTypeHintJsonConverter : JsonConverter<ValueTypeHint>
{
    public override void WriteJson(JsonWriter writer, ValueTypeHint value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("Name");
        writer.WriteValue(value.Name);
        writer.WritePropertyName("PrototypeType");
        serializer.Serialize(writer, value.PrototypeType.GetFullyQualifiedName());
        writer.WriteEndObject();
    }

    public override ValueTypeHint ReadJson(JsonReader reader, Type objectType, ValueTypeHint existingValue, bool hasExistingValue, JsonSerializer serializer) {
        var hint = new ValueTypeHint();
        while (reader.Read()) {
            if (reader.TokenType == JsonToken.PropertyName) {
                var propertyName = reader.Value.ToString();
                reader.Read();
                switch (propertyName) {
                    case "Name":
                        hint.Name = reader.Value.ToString();
                        break;
                    case "PrototypeType":

                        // hint.PrototypeType = reader.Value.ToString().GetTypeSymbol();
                        break;
                }
            }
        }
        return hint;
    }
}

[JsonConverter(typeof(ValueTypeHintJsonConverter))]
public class ValueTypeHint
{
    public string      Name          { get; set; }
    public ITypeSymbol PrototypeType { get; set; }

    public ITypeSymbol FailureFromType { get; set; }

    public ValueTypeHint() { }
    public ValueTypeHint(string name, ITypeSymbol prototypeType) {
        Name          = name;
        PrototypeType = prototypeType;
    }
    public ValueTypeHint(string name, ITypeSymbol prototypeType, ITypeSymbol type) : this(name, prototypeType) {
        FailureFromType = type;
    }
}

public class PropertyBind
{
    public string Name       { get; set; }
    public string Identifier { get; set; }

    public bool UseGetterPrefix { get; set; } = true;
    public bool UseSetterPrefix { get; set; } = true;

    public string BindingGetterName => UseGetterPrefix ? $"get{Name.ToIdentifierCasing(false)}" : Name;
    public string BindingSetterName => UseSetterPrefix ? $"set{Name.ToIdentifierCasing(false)}" : Name;

    public bool HasGetter { get; set; }
    public bool HasSetter { get; set; }

    public IPropertySymbol Property { get; set; }

    public ITypeSymbol Type => Property.Type;

    public ValueTypeHint ValueTypeHint { get; set; }

    public IMethodSymbol     GetMethod               => HasGetter && Property.DeclaredAccessibility == Accessibility.Public ? Property.GetMethod : null;
    public DocumentationData GetMethodDocumentation  => GetMethod?.GetDocumentationData();
    public string            GetMethodMetaDefinition => GetMethod.GetAttributeArgument<string>(Attributes.MetaDefinition);

    public IMethodSymbol     SetMethod               => HasSetter && Property.DeclaredAccessibility == Accessibility.Public ? Property.SetMethod : null;
    public DocumentationData SetMethodDocumentation  => SetMethod?.GetDocumentationData();
    public string            SetMethodMetaDefinition => SetMethod.GetAttributeArgument<string>(Attributes.MetaDefinition);

    public DocumentationData Documentation  => Property.GetDocumentationData();
    public string            MetaDefinition => Property.GetAttributeArgument<string>(Attributes.MetaDefinition);

    public PropertyBind(IPropertySymbol property, Compilation compilation) {
        Property = property;
        Name     = property.Name;

        var bindTypeHint = property.GetAttribute(Attributes.LanguageBindTypeHint);
        if (bindTypeHint != null) {
            var protoTypeType = ((AttributeSyntax) bindTypeHint.ApplicationSyntaxReference?.GetSyntax())?
               .ArgumentList?.Arguments
               .ElementAtOrDefault(1);

            INamedTypeSymbol protoTypeSymbol = null;

            if (protoTypeType?.Expression is TypeOfExpressionSyntax typeOfExpression) {
                var semanticModel = compilation.GetSemanticModel(property.Locations.First().SourceTree!);
                protoTypeSymbol = semanticModel.GetTypeInfo(typeOfExpression.Type).Type as INamedTypeSymbol;
            }
            ValueTypeHint = new() {
                Name          = bindTypeHint.GetArgument<string>(0),
                PrototypeType = protoTypeSymbol,
            };
        } else {
            ValueTypeHint = BindingsGenerator.ConvertToValueTypeHint(property.Type, property.Type);
        }
    }
}