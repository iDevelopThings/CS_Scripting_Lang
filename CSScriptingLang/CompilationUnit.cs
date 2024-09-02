using CSScriptingLang.Interpreter.Modules;

namespace CSScriptingLang;


public class CompilationUnit
{
    public List<Script>               Scripts   { get; } = new();
    public Dictionary<string, Script> ScriptMap { get; } = new();

    public CompilationUnit() { }

    public static CompilationUnit FromDirectory(string directoryPath) {
        var unit = new CompilationUnit();
        unit.AddDirectory(directoryPath);
        return unit;
    }

    public Script AddScript(string filePath) {
        var script = new Script {
            FilePath = filePath
        };

        script.Source = File.ReadAllText(filePath);

        ScriptMap[filePath] = script;
        Scripts.Add(script);

        return script;
    }

    public List<Script> AddDirectory(string directoryPath) {
        var scripts = new List<Script>();

        var relPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), directoryPath);
        var files   = Directory.GetFiles(relPath, $"*{Script.Extension}");

        foreach (var filePath in files) {
            var script = AddScript(filePath);
            if (scripts.Count == 0) {
                script.IsMain = true;
            }

            scripts.Add(script);
        }

        return scripts;
    }
    
    public Script GetMainScript() {
        return Scripts.FirstOrDefault(script => script.IsMain);
    }
}