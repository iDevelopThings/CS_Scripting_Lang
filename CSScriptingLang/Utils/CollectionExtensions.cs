namespace CSScriptingLang.Utils;

public static class CollectionExtensions
{
    public static IEnumerable<T> PopRange<T>(this Stack<T> stack, int count) {
        for (int i = 0; i < count; i++) {
            yield return stack.Pop();
        }
    }
}

public static class DictionaryExtensions
{
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
}