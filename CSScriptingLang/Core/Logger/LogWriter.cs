using System.Text;
using CSScriptingLang.Lexing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Core.Logging;

public class LogWriter
{
    public virtual bool ShouldConsume(ref LogMessage message) {
        return false;
    }

    public void Write(ref LogMessage message) {
        if (message.Consumed) {
            return;
        }

        var sb = new StringBuilder();

        Write(sb, message);

        if (ShouldConsume(ref message)) {
            message.Consume();
        }
    }

    public virtual void Write(StringBuilder output, LogMessage message) {
        var args = message.Args.ToArray();

        if (!message.Logger.HasLogFlag(LoggerFlags.NoTimestamp)) {
            output.Append(message.Timestamp.ToShortTimeString().BrightGray());
            output.Append(' ');
        }

        if (!message.Logger.HasLogFlag(LoggerFlags.NoSeverity)) {
            output.Append(message.Severity.ToColoredString());
            output.Append(' ');
        }

        if (!message.Logger.HasLogFlag(LoggerFlags.NoName)) {
            output.Append('[');
            output.Append(message.Logger.GetColoredName());
            output.Append("] ");
        }

        if (args.Length == 0)
            output.Append(message.Message);
        else
            output.AppendFormat(message.Message, args);

        if (!message.Logger.HasLogFlag(LoggerFlags.NoCaller)) {
            if (message.Severity >= LogLevel.Error) {
                output.AppendLine();
                output.Append("Called from:".Bold().BrightGray());
                output.AppendLine();
                output.Append(message.Caller.ToString());
                output.AppendLine();
            }
        }
        /*var str = $"{message.Timestamp.ToShortTimeString().BrightGray()} " +
                  $"{message.Severity.ToColoredString()} " +
                  $"[{message.Logger.GetColoredName()}] " +
                  $"{message.Message}";*/

        Format(output, message);
        Output(output, message);
    }

    public virtual void Format(StringBuilder output, LogMessage message) {
        
    }
    public virtual void Output(StringBuilder output, LogMessage message) {
        Console.WriteLine(output);
    }

    public virtual bool CanWrite(ref LogMessage msg) {
        return true;
    }
}

public class LogWriter_Syntax : LogWriter
{
    public static HashSet<Type> SyntaxExceptionTypes = [
        // typeof(SyntaxException),
        // typeof(ParserException),
        typeof(InterpreterException),
        // typeof(LexerException),
        typeof(FailedToGetRuntimeTypeException),
    ];

    /*static LogWriter_Syntax() {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
            if (args.ExceptionObject is Exception ex) {
                if (ex.Data.Contains("Logged")) {
                    return;
                }

                if (SyntaxExceptionTypes.Contains(ex.GetType())) {
                    // Logs.Global.Write(new LogMessage(Logs.Global, Caller.FromMethod(MethodBase.GetCurrentMethod()), LogLevel.Error, ex.Message).WithArg(ex));
                    Console.WriteLine(ex.Message);
                }
            }
        };

        AppDomain.CurrentDomain.FirstChanceException += (sender, args) => {
            if (SyntaxExceptionTypes.Contains(args.Exception.GetType())) {
                var caller = Caller.FromException(args.Exception);
                var logger = Logs.Find(caller.Method.DeclaringType);
                if (logger != null) {
                    logger.Create(caller)
                       .WithSeverity(LogLevel.Error)
                       .WithMessage(args.Exception.Message)
                       .WithContext(args.Exception)
                       .Log();

                    args.Exception.Data["Logged"] = true;
                }
            }
        };
    }*/

    public override bool ShouldConsume(ref LogMessage message) {
        return CanWrite(ref message);
    }

    public override bool CanWrite(ref LogMessage msg) {
        if (msg.GetContext<Exception>(out var ex)) {
            return SyntaxExceptionTypes.Contains(ex.GetType());
        }

        return false;
    }

    public override void Format(StringBuilder output, LogMessage message) {
        if (message.GetContext<BaseLanguageException>(out var exception)) {
            var caller = exception.Caller.IsValid() ? exception.Caller : message.Caller;
            var input  = exception.Input;

            var ew = ErrorWriter.Create(input, caller);

            StringBuilder sb;

            ErrorWriter.SetScriptIfUndefined(exception.Script);

            // if (message.GetContext<SyntaxException>(out var ex)) {
                // sb = ew.LogError(ex.Message, ex.Token, ex.Token);
            // } else if (message.GetContext<ParserException>(out var pex)) {
                // sb = ew.LogError(pex.Message, pex.From, pex.To);
            // if (message.GetContext<LexerException>(out var lex)) {
                // sb = ew.LogError(lex.Message, lex.From, lex.To);
             // if (message.GetContext<InterpreterException>(out var iex)) {
                // sb = ew.LogError(iex.Message, iex.Node);
            // } else if (message.GetContext<DeclarationException>(out var dex)) {
                // sb = ew.LogError(dex.Message, dex.Node);
            // } else {
                sb = new StringBuilder();
                sb.AppendLine($"Unknown exception type: {exception.GetType().Name}");
                sb.AppendLine(exception.Message);
            // }

            output.AppendLine();
            output.Append(sb);

            if (!exception.Trace.IsEmpty()) {
                output.AppendLine("Trace:".BrightGray() + "\n");
                output.AppendLine(exception.Trace.ToString());
            }
        } else {
            output.AppendLine(message.Message);
        }

    }
}

public class LogWriter_Exceptions : LogWriter
{
    public override bool ShouldConsume(ref LogMessage message) {
        return CanWrite(ref message);
    }
    public override bool CanWrite(ref LogMessage msg) {
        return msg.Severity >= LogLevel.Error;
    }
}
