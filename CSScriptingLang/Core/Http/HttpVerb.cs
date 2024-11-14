using System.Net;
using System.Text;
using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Core.Http;

public enum HttpVerb
{
    Get,
    Post,
    Put,
    Patch,
    Delete,
    Head,
    Options,
}

public static class HttpVerbUtils
{
    public static HttpVerb FromString(string verb) {
        return verb.ToLower() switch {
            "get"     => HttpVerb.Get,
            "post"    => HttpVerb.Post,
            "put"     => HttpVerb.Put,
            "delete"  => HttpVerb.Delete,
            "patch"   => HttpVerb.Patch,
            "head"    => HttpVerb.Head,
            "options" => HttpVerb.Options,
            _         => throw new ArgumentException($"Invalid HTTP verb: {verb}")
        };
    }

    public static string ToColoredString(this HttpVerb verb) => verb switch {
        HttpVerb.Get     => "GET".BrightGreen(),
        HttpVerb.Post    => "POST".BrightBlue(),
        HttpVerb.Put     => "PUT".BrightYellow(),
        HttpVerb.Delete  => "DELETE".BrightRed(),
        HttpVerb.Patch   => "PATCH".BrightMagenta(),
        HttpVerb.Head    => "HEAD".BrightCyan(),
        HttpVerb.Options => "OPTIONS".BrightGray(),
        _                => throw new ArgumentOutOfRangeException(nameof(verb), verb, null),
    };
    public static string ToColoredString(this List<HttpVerb> verbs) {
        return verbs.Select(verb => verb.ToColoredString()).Join(" | ");
    }
    public static HttpVerb Verb(this HttpListenerRequest request) {
        return request.HttpMethod.ToLower() switch {
            "get"     => HttpVerb.Get,
            "post"    => HttpVerb.Post,
            "put"     => HttpVerb.Put,
            "delete"  => HttpVerb.Delete,
            "patch"   => HttpVerb.Patch,
            "head"    => HttpVerb.Head,
            "options" => HttpVerb.Options,
            _         => throw new ArgumentException($"Invalid HTTP verb: {request.HttpMethod}")
        };
    }
}

public static class HttpStatusCodeUtils
{
    public static ReadOnlyMemory<byte> ToHtml(this HttpStatusCode code, string message = null) {
        return Encoding.UTF8.GetBytes($"<html><head><title>{(int) code} {code}</title></head><body><h1>{(int) code} {code}</h1>{message}</body></html>");
    }

    public static ReadOnlyMemory<byte> ToHtml(this HttpStatusCode code, string message, string title) {
        return Encoding.UTF8.GetBytes($"<html><head><title>{title}</title></head><body><h1>{(int) code} {code}</h1>{message}</body></html>");
    }
    public static ReadOnlyMemory<byte> ToJson(this HttpStatusCode code, string message = null) {
        return Encoding.UTF8.GetBytes($"{{\"status\":{(int) code},\"message\":\"{message ?? code.ToString()}\"}}");
    }

    public static ReadOnlyMemory<byte> ToJson(this HttpStatusCode code, string message, string title) {
        return Encoding.UTF8.GetBytes($"{{\"status\":{(int) code},\"message\":\"{message ?? code.ToString()}\",\"title\":\"{title}\"}}");
    }


}