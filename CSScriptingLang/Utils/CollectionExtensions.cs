namespace CSScriptingLang.Utils;

public static class CollectionExtensions
{
    public static IEnumerable<T> PopRange<T>(this Stack<T> stack, int count) {
        for (int i = 0; i < count; i++) {
            yield return stack.Pop();
        }
    }
    public static void PushRange<T>(this List<T> list, Stack<T> stack) {
        while (stack.Count > 0) {
            list.Add(stack.Pop());
        }
    }
}

public static class DictionaryExtensions
{
    public static Dictionary<TKey, TValue> Append<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, IEnumerable<(TKey, TValue)> entries) {
        foreach (var (key, value) in entries) {
            dictionary[key] = value;
        }

        return dictionary;
    }
    
    public static Dictionary<TKey, TValue> Append<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,IEnumerable<KeyValuePair<TKey, TValue>> entries) {
        foreach (var (key, value) in entries) {
            dictionary[key] = value;
        }

        return dictionary;
    }
    
    
    public static Dictionary<TKey, TValue> Append<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,Dictionary<TKey, TValue> entries) {
        foreach (var (key, value) in entries) {
            dictionary[key] = value;
        }

        return dictionary;
    }


    public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueFactory) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }

        value           = valueFactory();
        dictionary[key] = value;
        return value;
    }

    public static List<T> GetOrAdd<TKey, T>(this Dictionary<TKey, List<T>> dictionary, TKey key) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }

        value           = [];
        dictionary[key] = value;
        return value;
    }

    public static HashSet<T> GetOrAdd<TKey, T>(this Dictionary<TKey, HashSet<T>> dictionary, TKey key) {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }

        value           = [];
        dictionary[key] = value;
        return value;
    }
}