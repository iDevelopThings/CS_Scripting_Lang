using System.Reflection;

namespace CSScriptingLang.Utils.ReflectionUtils;

public struct StandaloneObjectMember
{
    public MemberInfo MemberInfo { get; set; }

    public StandaloneObjectMember(MemberInfo memberInfo) {
        MemberInfo = memberInfo;
    }

    public T GetValue<T>(object instance) => (T) GetValue(instance);

    public object GetValue(object instance) {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.GetValue(instance),
            FieldInfo fieldInfo       => fieldInfo.GetValue(instance),
            _                         => default
        };
    }

    public bool CanWrite() {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.CanWrite,
            FieldInfo fieldInfo       => true,
            _                         => false
        };
    }

    public bool CanRead() {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.CanRead,
            FieldInfo fieldInfo       => true,
            _                         => false
        };
    }

    public void SetValue(object instance, object value) {
        switch (MemberInfo) {
            case PropertyInfo propertyInfo:
                propertyInfo.SetValue(instance, value);
                break;
            case FieldInfo fieldInfo:
                fieldInfo.SetValue(instance, value);
                break;
        }
    }

    public Type Type => MemberInfo switch {
        PropertyInfo propertyInfo => propertyInfo.PropertyType,
        FieldInfo fieldInfo       => fieldInfo.FieldType,
        _                         => default
    };

    public string Name => MemberInfo.Name;

    public bool GetAttribute<T>(out T attribute) where T : Attribute {
        attribute = MemberInfo.GetCustomAttribute<T>();
        return attribute != null;
    }
    public bool HasAttribute<T>() where T : Attribute => MemberInfo.GetCustomAttribute<T>() != null;
}

public struct ObjectMember
{
    public MemberInfo MemberInfo { get; set; }

    // This is the object instance that contains the member
    public object ContainingObjectInstance { get; set; }

    public ObjectMember(MemberInfo memberInfo, object value) {
        MemberInfo               = memberInfo;
        ContainingObjectInstance = value;
    }

    public T GetValue<T>() => (T) GetValue();
    public object GetValue() {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.GetValue(ContainingObjectInstance),
            FieldInfo fieldInfo       => fieldInfo.GetValue(ContainingObjectInstance),
            _                         => default
        };
    }

    public bool CanWrite() {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.CanWrite,
            FieldInfo fieldInfo       => true,
            _                         => false
        };
    }

    public bool CanRead() {
        return MemberInfo switch {
            PropertyInfo propertyInfo => propertyInfo.CanRead,
            FieldInfo fieldInfo       => true,
            _                         => false
        };
    }

    public void SetValue(object value) {
        switch (MemberInfo) {
            case PropertyInfo propertyInfo:
                propertyInfo.SetValue(ContainingObjectInstance, value);
                break;
            case FieldInfo fieldInfo:
                fieldInfo.SetValue(ContainingObjectInstance, value);
                break;
        }
    }

    public Type Type => MemberInfo switch {
        PropertyInfo propertyInfo => propertyInfo.PropertyType,
        FieldInfo fieldInfo       => fieldInfo.FieldType,
        _                         => default
    };

    public string Name => MemberInfo.Name;

    public bool HasAttribute<T>() where T : Attribute => MemberInfo.GetCustomAttribute<T>() != null;
}

public struct ObjectPropertiesProxy
{
    public object ValueInstance { get; set; }
    public Type   Type          { get; set; }

    public List<Attribute> ClassAttributes { get; set; } = new();

    public List<ObjectMember>               AllFields { get; set; } = new();
    public Dictionary<string, ObjectMember> Fields    { get; set; } = new();

    public ObjectPropertiesProxy(Type type, object value) {
        ValueInstance = value;
        Type          = type;

        ClassAttributes.AddRange(type.GetCustomAttributes());

        AllFields.AddRange(
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
               .Select(property => new ObjectMember(property, value))
        );
        AllFields.AddRange(
            type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
               .Select(field => new ObjectMember(field, value))
        );

        Fields = AllFields.ToDictionary(member => member.MemberInfo.Name);
    }

    public IEnumerator<ObjectMember> GetEnumerator() => AllFields.GetEnumerator();

    // Casted T Attribute iterator
    public IEnumerable<T> GetClassAttributes<T>() where T : Attribute {
        return ClassAttributes.OfType<T>();
    }
}