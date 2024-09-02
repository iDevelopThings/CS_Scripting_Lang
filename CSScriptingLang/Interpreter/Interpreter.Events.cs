using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using CSScriptingLang.VM;
using CSScriptingLang.VM.Tables;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter;

public static class InterpreterEvents
{
    public static Action<InterpreterExecutionContext> OnExecutionScopePushed;
    public static Action<InterpreterExecutionContext> OnExecutionScopePopped;

    public static Action<FunctionExecutionFrame> OnFunctionFramePushed;
    public static Action<FunctionExecutionFrame> OnFunctionFramePopped;
}

public partial class Interpreter { }