using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CSScriptingLang.Utils;

public struct Caller
{
    public string     File       { get; set; }
    public int        Line       { get; set; }
    public MethodBase Method     { get; set; }
    public string     MethodName { get; set; }

    private string _methodFullName;
    private bool   _methodFullNameSet;

    public string MethodFullName {
        get {
            if (_methodFullNameSet)
                return _methodFullName;

            // Create method name with param info, for ex: 
            // UsingCallbackHandle PushValue(RuntimeValue value)
            var method = Method;
            if (method == null) {
                _methodFullName    = MethodName;
                _methodFullNameSet = true;
                return _methodFullName;
            }

            var parameters = method.GetParameters();
            var paramNames = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; i++) {
                var param = parameters[i];
                paramNames[i] = $"{param.ParameterType.Name} {param.Name}";
            }

            _methodFullName    = $"{method.Name}({string.Join(", ", paramNames)})";
            _methodFullNameSet = true;

            return _methodFullName;
        }
    }

    public static Caller GetCaller(
        [CallerFilePath]
        string file = "",
        [CallerLineNumber]
        int line = 0,
        [CallerMemberName]
        string method = ""
    ) => FromAttributes(file, line, method);

    public static Caller FromAttributes(
        string file = "", int line = 0, string method = ""
    ) {
        return new Caller {
            File       = file,
            Line       = line,
            MethodName = method
        };
    }

    public static Caller GetFromFrame(
        int frameNumber = 1
    ) {
        var frame  = new StackFrame(frameNumber, true);
        var method = frame.GetMethod();
        return new Caller {
            File       = frame.GetFileName(),
            Line       = frame.GetFileLineNumber(),
            Method     = method,
            MethodName = method?.Name
        };
    }

    public override string ToString() => $"{File}:{Line} {MethodFullName}";
}