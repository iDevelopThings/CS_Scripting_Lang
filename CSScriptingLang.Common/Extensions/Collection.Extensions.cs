using JetBrains.Annotations;

namespace CSScriptingLang.Common.Extensions;

public static class Collection_Extensions
{
    // List `IsValidIndex` extension method
    public static bool IsValidIndex<T>(this List<T> list, int index) {
        return index >= 0 && index < list.Count;
    }
    public static int ClampedIndex<T>(this List<T> list, int index, int min = 0) {
        if (index < min)
            return min;
        if (index >= list.Count)
            return list.Count - 1;
        return index;
    }
    
    
    
    
    /// <summary>
    /// Immediately executes the given action on each element in the source sequence.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence of elements</param>
    /// <param name="action">The action to execute on each element</param>

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        foreach (var element in source)
            action(element);
    }

    /// <summary>
    /// Immediately executes the given action on each element in the source sequence.
    /// Each element's index is used in the logic of the action.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the sequence</typeparam>
    /// <param name="source">The sequence of elements</param>
    /// <param name="action">The action to execute on each element; the second parameter
    /// of the action represents the index of the source element.</param>

    public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var index = 0;
        foreach (var element in source)
            action(element, index++);
    }
    
    
    public static Stack<T> ToStack<T>(this IEnumerable<T> source) {
        return new Stack<T>(source);
    }
    
    public static string Join<TSource>(this IEnumerable<TSource> source, string separator = ", ") {
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