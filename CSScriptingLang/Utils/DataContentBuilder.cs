using CSScriptingLang.Common.Extensions;
using JetBrains.Annotations;

namespace CSScriptingLang.Utils;

struct DataContentBuilder
{
    public string Content = string.Empty;
    public string Prefix  = string.Empty;

    public DataContentBuilder() {
        Content = string.Empty;
        Prefix  = string.Empty;
    }

    public static DataContentBuilder Create(string prefix = null) => new() {
        Prefix = prefix ?? string.Empty,
    };

    public static implicit operator string(DataContentBuilder builder) => builder.ToString();
    public static implicit operator DataContentBuilder(string content) => new() {Content = content};

    public override string ToString() {
        return (Prefix.IsNotNullOrWhiteSpace() ? $"{Prefix} -> " : "") + Content;
    }

    public DataContentBuilder ClearTrailingSpace() {
        if (Content.Length > 0 && Content[^1] == ' ') {
            Content = Content[..^1];
        }
        return this;
    }

    public DataContentBuilder Choice(bool condition, Action<DataContentBuilder> a, Action<DataContentBuilder> b) {
        if (condition) {
            a(this);
        } else {
            b(this);
        }

        return this;
    }
    
    public DataContentBuilder Add([CanBeNull] string content, bool addSpaceAfter = true) {
        if (content.IsNotNullOrWhiteSpace()) {
            Content += content;
            if (addSpaceAfter) Content += " ";
        }

        return this;
    }
    public DataContentBuilder AddIf(bool condition, [CanBeNull] string content, bool addSpaceAfter = true) {
        if (condition) {
            Add(content, addSpaceAfter);
        }

        return this;
    }

    public static DataContentBuilder operator +(DataContentBuilder builder, string[] content) {
        builder.Content += content.Join(" ");
        return builder;
    }

    public static DataContentBuilder operator +(DataContentBuilder builder, string content) {
        builder.Content += content;
        return builder;
    }

    public static DataContentBuilder operator +(DataContentBuilder builder, DataContentBuilder content) {
        builder.Content += content.Content;
        return builder;
    }

    public static DataContentBuilder operator +(string content, DataContentBuilder builder) {
        builder.Content = content + builder.Content;
        return builder;
    }

    public static DataContentBuilder operator +(DataContentBuilder builder, char content) {
        builder.Content += content;
        return builder;
    }

    public static DataContentBuilder operator +(char content, DataContentBuilder builder) {
        builder.Content = content + builder.Content;
        return builder;
    }


}