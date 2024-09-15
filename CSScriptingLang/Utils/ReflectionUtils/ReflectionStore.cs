using System.Reflection;
using Engine.Engine.Logging;
using JetBrains.Annotations;

namespace CSScriptingLang.Utils.ReflectionUtils;

public static class ReflectionStore
{
    private static HashSet<Assembly> ProcessedAssemblies = new();

    private static Logger Logger = Logs.Get("ReflectionStore", LogLevel.Warning);

    public static ClassScopedTimerInst Timer = ClassScopedTimerInst.Create(Logger)
       .SetColorFn(n => n.BoldGreen())
       .SetName("Reflection Store");


    public static bool IsLoading { get; }

    public static readonly Dictionary<Type, List<Type>> ReflectedTypes = new();

    // Groups types by attribute, for ex
    // [AssetTypeDefinition] -> [Scene, Prefab, ScriptableObject]
    public static readonly Dictionary<Type, List<Type>>              TypesWithAttribute   = new();
    public static readonly Dictionary<Type, List<(Attribute, Type)>> TypesWithAttributeKv = new();

    public static readonly List<Type> AllTypes = new();

    public static readonly HashSet<Type> ProjectAssemblyTypes = new();

    public static readonly HashSet<string> ProjectAssemblyNames = new() {
        "CSScriptingLang",
        "LanguageTests",
    };


    static ReflectionStore() {
        using var _ = Timer.NewWith("Load Reflection Types");

        IsLoading = true;

        var allAssemblies  = AppDomain.CurrentDomain.GetAssemblies();
        var projAssemblies = allAssemblies.Where(a => ProjectAssemblyNames.Contains(a.GetName().Name));
        foreach (var assembly in projAssemblies) {
            try {
                AddTypes(assembly, assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException e) {
                AddTypes(assembly, e.Types);
            }
        }

        /*foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            try {
                AddTypes(assembly, assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException e) {
                AddTypes(assembly, e.Types);
            }
        }*/

        Logger.Debug($"Loaded {AllTypes.Count} types");

        IsLoading = false;
    }

    private static void AddTypes(Assembly assembly, Type[] types) {
        if (ProcessedAssemblies.Contains(assembly))
            return;

        foreach (var type in types) {
            if (type == null)
                continue;

            ProcessedAssemblies.Add(assembly);

            AllTypes.Add(type);

            if (ProjectAssemblyNames.Contains(type.Assembly.GetName().Name)) {
                ProjectAssemblyTypes.Add(type);
            }

            if (!ReflectedTypes.ContainsKey(type))
                ReflectedTypes[type] = new List<Type>();

            var attributes = type.GetCustomAttributes();
            foreach (var attribute in attributes) {
                TypesWithAttributeKv.GetOrAdd(attribute.GetType()).Add((attribute, type));
                TypesWithAttribute.GetOrAdd(attribute.GetType()).Add(type);
            }

            foreach (var t in types) {
                if (type.IsAssignableFrom(t))
                    ReflectedTypes[type].Add(t);
                // Also support generic types, for example adding children of SomeType<T> to SomeType<>
                if (type.IsGenericType && t!.IsGenericType && type.GetGenericTypeDefinition() == t.GetGenericTypeDefinition())
                    ReflectedTypes[type].Add(t);
            }
        }
    }

    private static void EnsureLoaded<T>() {
        var assembly = typeof(T).Assembly;
        if (ProcessedAssemblies.Contains(assembly))
            return;

        using var _ = Timer.NewWith($"Ensure Loaded {typeof(T).Name}, {assembly.GetName().Name}");

        try {
            AddTypes(assembly, assembly.GetTypes());
        }
        catch (ReflectionTypeLoadException e) {
            AddTypes(assembly, e.Types);
        }
    }

    public static IEnumerable<MethodInfo> AllMethodsWithAttribute<T>(BindingFlags flags = BindingFlags.Public) where T : Attribute {
        EnsureLoaded<T>();

        return AllTypes
           .SelectMany(t => t.GetMethods())
           .Where(
                m => m.GetCustomAttributes<T>().Any()
            );
    }
    public static IEnumerable<StandaloneObjectMember> AllFieldsWithAttribute<T>(BindingFlags flags = BindingFlags.Public) where T : Attribute {
        EnsureLoaded<T>();

        return AllTypes
           .SelectMany(t => t.GetPropertiesAndFields())
           .Where(
                m => m.GetCustomAttributes<T>().Any()
            )
           .Select(f => new StandaloneObjectMember(f));
    }

    public static List<Type> AllTypesWithAttribute<T>() {
        EnsureLoaded<T>();
        return TypesWithAttribute.GetValueOrDefault(typeof(T), new List<Type>());
    }
    [CanBeNull]
    public static IEnumerable<(Type type, T attribute)> AllTypesWithAttributeIncludingAttr<T>() where T : Attribute {
        EnsureLoaded<T>();

        foreach (var t in TypesWithAttributeKv.GetOrAdd(typeof(T)))
            yield return (t.Item2, (T) t.Item1);
        
    }

    public static List<Type> AllTypesWithAttribute<TA, TB>() {
        EnsureLoaded<TA>();
        EnsureLoaded<TB>();

        return TypesWithAttribute.GetValueOrDefault(typeof(TA), new List<Type>())
           .Concat(TypesWithAttribute.GetValueOrDefault(typeof(TB), new List<Type>()))
           .ToList();
    }

    public static List<Type> AllTypesOf<T>() {
        EnsureLoaded<T>();

        return ReflectedTypes.GetValueOrDefault(typeof(T), new List<Type>());
    }
}