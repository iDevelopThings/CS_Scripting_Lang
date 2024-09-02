using System.Reflection;
using Engine.Engine.Logging;

namespace CSScriptingLang.Utils.ReflectionUtils;

public static class ReflectionStore
{
    private static Logger Logger = Logs.Get("ReflectionStore");

    public static bool IsLoading { get; }

    public static readonly Dictionary<Type, List<Type>> ReflectedTypes = new();

    // Groups types by attribute, for ex
    // [AssetTypeDefinition] -> [Scene, Prefab, ScriptableObject]
    public static readonly Dictionary<Type, List<Type>> TypesWithAttribute = new();

    public static readonly List<Type> AllTypes = new();

    public static readonly HashSet<Type> ProjectAssemblyTypes = new();

    public static readonly List<string> ProjectAssemblyNames = new() {
        "CSScriptingLang",
        "LanguageTests",
        Assembly.GetEntryAssembly()?.GetName().Name
    };


    static ReflectionStore() {
        using var _ = ScopeTimer.NewWith("Load Reflection Types");

        IsLoading = true;
        
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
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

        Console.WriteLine($"Loaded {AllTypes.Count} types");

        IsLoading = false;
    }

    private static void AddTypes(Assembly assembly, Type[] types) {
        foreach (var type in types) {
            if (type == null)
                continue;

            AllTypes.Add(type);

            if (ProjectAssemblyNames.Contains(type.Assembly.GetName().Name)) {
                ProjectAssemblyTypes.Add(type);
            }

            if (!ReflectedTypes.ContainsKey(type))
                ReflectedTypes[type] = new List<Type>();

            var attributes = type.GetCustomAttributes();
            foreach (var attribute in attributes) {
                if (!TypesWithAttribute.ContainsKey(attribute.GetType()))
                    TypesWithAttribute[attribute.GetType()] = new List<Type>();

                TypesWithAttribute[attribute.GetType()].Add(type);
            }

            foreach (var t in types) {
                if (type.IsAssignableFrom(t))
                    ReflectedTypes[type].Add(t);
                // Also support generic types, for example adding children of SomeType<T> to SomeType<>
                if (type.IsGenericType && t.IsGenericType && type.GetGenericTypeDefinition() == t.GetGenericTypeDefinition())
                    ReflectedTypes[type].Add(t);
            }
        }
    }

    public static IEnumerable<MethodInfo> AllMethodsWithAttribute<T>(BindingFlags flags = BindingFlags.Public) where T : Attribute => AllTypes
       .SelectMany(t => t.GetMethods())
       .Where(
            m => m.GetCustomAttributes<T>().Any()
        );

    public static List<Type> AllTypesWithAttribute<T>()
        => TypesWithAttribute.GetValueOrDefault(typeof(T), new List<Type>());

    public static List<Type> AllTypesWithAttribute<TA, TB>()
        => TypesWithAttribute.GetValueOrDefault(typeof(TA), new List<Type>())
           .Concat(TypesWithAttribute.GetValueOrDefault(typeof(TB), new List<Type>()))
           .ToList();

    public static List<Type> AllTypesOf<T>() {
        return ReflectedTypes.GetValueOrDefault(typeof(T), new List<Type>());
    }
}