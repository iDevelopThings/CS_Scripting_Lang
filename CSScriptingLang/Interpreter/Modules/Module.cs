namespace CSScriptingLang.Interpreter.Modules;

public class Module
{
    public string Name          { get; set; }
    public string DirectoryPath { get; set; }

    public bool IsMainModule { get; set; }

    public List<Script> Scripts { get; set; } = new();

    public Script MainScript { get; set; }
    public Script TryGetMainScript() {
        // Try to find the first with a `main` function
        var s = Scripts.FirstOrDefault(script => script.IsMain);
        if (s != null) {
            return s;
        }

        // Otherwise next we'll try to use an `index` script
        s = Scripts.FirstOrDefault(script => script.NameWithoutExtension.ToLower() == "index");
        if (s != null) {
            return s;
        }

        // Otherwise we'll just use the first script
        return Scripts.FirstOrDefault();
    }

    public Module(string name) {
        Name         = name;
        IsMainModule = name.ToLower() == "main";
    }

    public void AddScript(Script script) {
        Scripts.Add(script);
        script.Module = this;

        TrySetOuterMostDirectory();
    }

    public void TrySetOuterMostDirectory() {
        DirectoryPath = Scripts.Select(script => script.FilePath)
           .OrderByDescending(path => path.Length)
           .FirstOrDefault();
    }

    public Script GetScriptByPath(string filePath)
        => Scripts.FirstOrDefault(script => script.FilePath == filePath);

    public Script GetScriptByName(string scriptName)
        => Scripts.FirstOrDefault(script => script.Name == scriptName);
}