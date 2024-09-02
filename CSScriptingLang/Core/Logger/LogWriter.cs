using System;
using System.Runtime.CompilerServices;
using System.Text;
using CSScriptingLang.Utils;

namespace Engine.Engine.Logging;

public class LogWriter
{
    public void Write(StringBuilder output, LogMessage message) {
        var args = message.Args.ToArray();

        output.Append(message.Timestamp.ToShortTimeString().BrightGray());
        output.Append(" ");
        output.Append(message.Severity.ToColoredString());
        output.Append(" ");
        output.Append("[");
        output.Append(message.Logger.GetColoredName());
        output.Append("] ");
        output.AppendFormat(message.Message, args);
        if(message.Severity >= LogLevel.Error) {
            output.AppendLine();
            output.Append("Called from:".Bold().BrightGray());
            output.Append(" ");
            output.Append(message.Caller.ToString());
            output.AppendLine();
        }
        /*var str = $"{message.Timestamp.ToShortTimeString().BrightGray()} " +
                  $"{message.Severity.ToColoredString()} " +
                  $"[{message.Logger.GetColoredName()}] " +
                  $"{message.Message}";*/
    }
    public void Write(LogMessage message) {
        var output = new StringBuilder();
        Write(output, message);
        
        Console.WriteLine(output.ToString());

        // Console.WriteLine(str, args);
        
        // if(message.Severity >= LogLevel.Error) {
        //     Console.WriteLine($"{"Called from:".Bold().BrightGray()} {message.Caller.ToString()}");
        // }

        // var document = new Document();
        // document.Children.Add(message.Timestamp.ToShortTimeString().Gray());
        // document.Children.Add(" ");
        // document.Children.Add(message.Severity.ToSpan());
        // document.Children.Add(" ");
        // document.Children.Add("[".DarkGray());
        // document.Children.Add(message.Logger.GetName().Gray());
        // document.Children.Add("] ".DarkGray());
        //
        // if (message.ElCollection == null) {
        //     document.Children.Add(message.Message);
        //     document.Children.Add(args);
        // } else {
        //     document.Children.Add(message.ElCollection);
        // }
        //
        // ConsoleRenderer.RenderDocument(document);
    }
}