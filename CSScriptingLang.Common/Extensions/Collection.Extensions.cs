namespace CSScriptingLang.Common.Extensions;

public static class Collection_Extensions
{
    public static string Join<TSource>(this IEnumerable<TSource> source, string separator) {
        return string.Join(separator, source);
    }
    public static Dictionary<T, S> AddRange<T, S>(this Dictionary<T, S> source, KeyValuePair<T, S> pair) {
        source.Add(pair.Key, pair.Value);
        return source;
    }
    public static Dictionary<T, S> AddRange<T, S>(this Dictionary<T, S> source, IEnumerable<KeyValuePair<T, S>> pairs) {
        if (pairs == null)
            return source;
        
        foreach (var pair in pairs)
            source.Add(pair.Key, pair.Value);
        return source;
    }
    public static Dictionary<T, S> AddRange<T, S>(this Dictionary<T, S> source, IEnumerable<(T, S)> pairs) {
        if (pairs == null)
            return source;
        
        foreach (var pair in pairs)
            source.Add(pair.Item1, pair.Item2);
        return source;
    }
    
    public static Dictionary<T, S> AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> pairs) {
        if (pairs == null)
            return source;
        
        foreach (var pair in pairs)
            source.Add(pair.Key, pair.Value);
        return source;
    }
    
    

    /*public static TSource AddRange<TSource, T>(this TSource target, IEnumerable<T> source) where TSource : ICollection<T> {
        if (target == null)
            throw new ArgumentNullException(nameof(target));
        if (source == null)
            return target;
        
        foreach (var element in source)
            target.Add(element);
        
        return target;
    }*/
}