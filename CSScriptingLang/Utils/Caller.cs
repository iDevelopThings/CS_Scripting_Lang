using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace CSScriptingLang.Utils;

public struct Caller : IEquatable<Caller>
{
    public static Caller Empty => new();

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

            var methodStr       = $"{method.Name}({string.Join(", ", paramNames)})";
            var methodWithClass = $"{method.DeclaringType?.Name}.{methodStr}";

            _methodFullName    = methodWithClass;
            _methodFullNameSet = true;

            return _methodFullName;
        }
    }


    public bool IsValid() {
        return !string.IsNullOrEmpty(File) && Line > 0;
    }


    public static Caller GetCaller([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string method = "")
        => FromAttributes(file, line, method);

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

    public override string ToString() => $"{Path.GetFullPath(File)}:{Line}:0\n{MethodFullName}";

    public static Caller FromException(Exception ex) {
        var site  = ex.TargetSite;
        var trace = new StackTrace(ex, fNeedFileInfo: true);

        return new Caller {
            File       = trace.GetFrame(0)?.GetFileName(),
            Line       = trace.GetFrame(0)?.GetFileLineNumber() ?? 0,
            Method     = site,
            MethodName = site?.Name
        };
    }

    public bool Equals(Caller other) {
        return File == other.File && Line == other.Line && Equals(Method, other.Method) && MethodName == other.MethodName;
    }
    public override bool Equals(object obj) {
        return obj is Caller other && Equals(other);
    }
    public override int GetHashCode() {
        return HashCode.Combine(File, Line, Method, MethodName);
    }
}

public struct CallerList : IEnumerable<Caller>
{
    private List<Caller> _callers;

    public CallerList(params Caller[] callers) {
        _callers = new List<Caller>(callers);
    }
    
    public bool IsEmpty() => _callers is {Count: 0};

    public static void Dump(string contextStr = "") {
        var list = Get(0, 8);

        Console.WriteLine(new string('-', 80));
        Console.WriteLine(!string.IsNullOrEmpty(contextStr) ? contextStr : "Dumped Callers: ");
        Console.WriteLine(list);
        Console.WriteLine(new string('-', 80));
    }

    public static CallerList Get(int skipFrames = 1, int frameCount = 4, List<Type> dontCountTypes = null) {
        var trace   = new StackTrace(skipFrames, fNeedFileInfo: true);
        var callers = new List<Caller>();

        var count = 0;

        var frames = trace.GetFrames();
        foreach (var frame in frames) {
            if (count >= frameCount)
                break;

            var method = frame.GetMethod();

            // skip any methods that are not part of the user code(ie, system methods)
            if (method == null || method.DeclaringType?.Assembly != typeof(Caller).Assembly)
                continue;

            callers.Add(new Caller {
                File       = frame.GetFileName(),
                Line       = frame.GetFileLineNumber(),
                Method     = method,
                MethodName = method?.Name
            });

            if (dontCountTypes != null && dontCountTypes.Contains(method.DeclaringType))
                continue;

            count++;
        }

        /*
        for (var i = 0; i < frameCount; i++) {
            var frame  = trace.GetFrame(i);
            var method = frame.GetMethod();
            callers.Add(new Caller {
                File       = frame.GetFileName(),
                Line       = frame.GetFileLineNumber(),
                Method     = method,
                MethodName = method?.Name
            });
        }
        */

        return new CallerList(callers.ToArray());
    }

    public void Add(Caller caller) => _callers.Add(caller);

    public IEnumerator<Caller> GetEnumerator() => _callers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() {
        if(_callers is {Count: 0} or null)
            return "No callers";
        
        var sb = new StringBuilder();

        foreach (var caller in _callers) {
            sb.AppendLine(caller.ToString());
        }

        return sb.ToString();
    }
}