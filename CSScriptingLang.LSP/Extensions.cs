using CSScriptingLang.Interpreter;
using CSScriptingLang.Interpreter.Modules;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.LSP;

public static class Extensions
{
    public static Script GetScript(this Interpreter.Interpreter interpreter, TextDocumentIdentifier request)
        => GetScript(interpreter, request.Uri);

    public static Script GetScript(this Interpreter.Interpreter interpreter, ITextDocumentIdentifierParams request)
        => GetScript(interpreter, request.TextDocument.Uri);
    
    public static Script GetScript(this Interpreter.Interpreter interpreter, DocumentUri uri)
        => interpreter.ModuleResolver.GetScriptByAbsPath(uri.GetFileSystemPath());

    public static InterpreterFile GetFile(this Interpreter.Interpreter interpreter, TextDocumentIdentifier request)
        => GetFile(interpreter, request.Uri);

    public static InterpreterFile GetFile(this Interpreter.Interpreter interpreter, ITextDocumentIdentifierParams request)
        => GetFile(interpreter, request.TextDocument.Uri);

    public static InterpreterFile GetFile(this Interpreter.Interpreter interpreter, DocumentUri uri)
        => interpreter.FileSystem.GetFile(uri.GetFileSystemPath());

}