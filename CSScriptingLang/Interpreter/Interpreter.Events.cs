namespace CSScriptingLang.Interpreter;

public static class InterpreterEvents
{
    public static Action<InterpreterExecutionContext> OnExecutionScopePushed;
    public static Action<InterpreterExecutionContext> OnExecutionScopePopped;

    public static Action<FunctionExecutionFrame> OnFunctionFramePushed;
    public static Action<FunctionExecutionFrame> OnFunctionFramePopped;
}

public partial class Interpreter { }