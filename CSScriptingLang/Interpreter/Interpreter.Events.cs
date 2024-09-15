using CSScriptingLang.Interpreter.Context;

namespace CSScriptingLang.Interpreter;

public static class InterpreterEvents
{
    // public static Action<InterpreterExecutionContext> OnExecutionScopePushed;
    // public static Action<InterpreterExecutionContext> OnExecutionScopePopped;

    public static Action<Frame> OnFunctionFramePushed;
    public static Action<Frame> OnFunctionFramePopped;
}

public partial class Interpreter { }