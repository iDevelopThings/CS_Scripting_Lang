using System.Diagnostics;
using SharpX;

namespace CSScriptingLang.Utils;

public static class MaybeExtensions
{
    [DebuggerStepThrough]
    public static T Value<T>(this Maybe<T> maybe) {
        if (maybe.MatchJust(out var value)) {
            return value;
        }
        return default!;
    }
    
    [DebuggerStepThrough]
    public static bool IsNull<T>(this Maybe<T> maybe) {
        if(maybe.MatchNothing()) {
            return true;
        }
        
        if (maybe.MatchJust(out var value)) {
            return value == null;
        }
        
        return false;
    }
    
    [DebuggerStepThrough]
    public static T ValueOrElse<T>(this Maybe<T> maybe, T defaultValue = default!) {
        if (maybe.MatchJust(out var value)) {
            return value;
        }
        return defaultValue;
    }
    [DebuggerStepThrough]
    public static T ValueOrElse<T>(this Maybe<T> maybe, Func<T> defaultValue) {
        if (maybe.MatchJust(out var value)) {
            return value;
        }
        return defaultValue();
    }

}