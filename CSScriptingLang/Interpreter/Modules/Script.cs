using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Parsing;
using CSScriptingLang.Parsing.AST;

namespace CSScriptingLang.Interpreter.Modules;

public class Script
{
    public const string Extension = ".js";

    private string _filePath;
    public string FilePath {
        get => File?.Abs ?? _filePath;
        set => _filePath = value;
    }
    public bool IsMain { get; set; }

    public IVirtualFile File   { get; set; }
    public Module       Module { get; set; }


    private string _source;
    public string Source {
        get => _source ??= File.Content;
        set => _source = value;
    }
    public ProgramNode Program { get; set; }
    public Parser      Parser  { get; set; }
}