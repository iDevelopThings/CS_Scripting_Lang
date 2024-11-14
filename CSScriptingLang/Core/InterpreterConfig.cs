using CommandLine;
using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Lexing;

namespace CSScriptingLang.Core;

public enum InterpreterMode
{
    Execution,
    Repl,
    Lsp
}

public enum InterpreterExecMode
{
    Original,
    IncrementalSyntaxTree
}

public static class InterpreterConfig
{
    public static InterpreterMode     Mode     { get; set; } = InterpreterMode.Execution;
    public static InterpreterExecMode ExecMode { get; set; } = InterpreterExecMode.Original;

    public static string     ExecutionPath   { get; set; }
    public static ModulePath ExecutionScript { get; set; }
    public static string     ExecutionModule { get; set; }

    public static bool WatchRootDirectory { get; set; } = true;

    public static bool         FunctionCallDebugging  { get; set; }
    public static List<string> FunctionCallsToDebug   { get; set; } = new();
    public static List<string> FunctionCallsToExclude { get; set; } = new();

    public static InterpreterFileSystem FileSystem { get; set; }

    public static bool CanDebugFunction(string functionName) {
        if (FunctionCallDebugging == false)
            return false;

        if (FunctionCallsToExclude.Count > 0 && FunctionCallsToExclude.Contains(functionName))
            return false;

        if (FunctionCallsToDebug.Count > 0)
            return FunctionCallsToDebug.Contains(functionName);

        return true;
    }

    public static FatalErrorHandlingMethodType ErrorHandlingMethod {
        get => ErrorWriter.FatalErrorHandlingMethod;
        set => ErrorWriter.FatalErrorHandlingMethod = value;
    }

    public static void Apply(BaseOptions baseOptions) {
        ExecutionPath   = baseOptions.Path;
        ExecutionModule = baseOptions.Module;

        FileSystem = new InterpreterFileSystem(ExecutionPath, true);

        switch (baseOptions) {
            case RunOptions runOptions: {
                FunctionCallDebugging  = runOptions.FunctionCallDebugging;
                FunctionCallsToDebug   = runOptions.FunctionCallsToDebug.ToList();
                FunctionCallsToExclude = runOptions.FunctionCallsToExclude.ToList();

                ErrorHandlingMethod = runOptions.FatalErrorHandlingMethod;
                break;
            }
            case ReplOptions replOptions: {
                ErrorHandlingMethod = replOptions.FatalErrorHandlingMethod;
                break;
            }
        }

        ExecutionScript = new ModulePath(FileSystem, ExecutionPath);
    }
}