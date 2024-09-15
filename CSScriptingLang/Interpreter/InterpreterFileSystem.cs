using CSScriptingLang.Core.FileSystem;
using CSScriptingLang.Interpreter.Modules;

namespace CSScriptingLang.Interpreter;

public class InterpreterFile
{
    public InterpreterFileSystem FS { get; set; }

    public string Path    { get; set; }
    public string Content { get; set; }

    public string Name => System.IO.Path.GetFileName(Path);
    public string Dir  => System.IO.Path.GetDirectoryName(Path);
    public string Abs  => System.IO.Path.Combine(FS.Root, Path);
    public string Rel  => System.IO.Path.GetRelativePath(FS.Root, Abs);
}

public class InterpreterFileSystem
{
    public Dictionary<string, InterpreterFile> Files { get; set; } = new();

    public string Root       { get; set; }
    public bool   IsPhysical { get; set; }

    public Action<InterpreterFile> OnFileChanged { get; set; }
    public Action<InterpreterFile> OnFileAdded   { get; set; }
    public Action<InterpreterFile> OnFileRemoved { get; set; }

    public InterpreterFileSystem() { }
    public InterpreterFileSystem(string root, bool isPhysical, bool loadAllFiles = true) {
        Initialize(root, isPhysical, loadAllFiles);
    }

    public virtual void Initialize(string root, bool isPhysical, bool loadAllFiles = true) {
        Root       = Path.GetFullPath(root);
        IsPhysical = isPhysical;

        if (IsPhysical && loadAllFiles) {
            LoadAllFiles();
        }
    }

    private void LoadAllFiles() {
        var files = Directory.GetFiles(Root, $"*{Script.Extension}", SearchOption.AllDirectories);

        foreach (var file in files) {
            var relativePath = Path.GetRelativePath(Root, file);

            AddFile(relativePath, File.ReadAllText(file));
        }
    }

    public IEnumerable<InterpreterFile> AllAtPath(string path) {
        var files = Directory.GetFiles(Path.Combine(Root, path), $"*{Script.Extension}", SearchOption.AllDirectories);

        foreach (var file in files) {
            var relativePath = Path.GetRelativePath(Root, file);
            var f            = AddFile(relativePath, File.ReadAllText(file));

            yield return f;
        }
    }

    public InterpreterFile AddFile(string path, string content = null) {
        path = (Path.IsPathRooted(path) ? Path.GetRelativePath(Root, path) : path)!;

        if (Files.TryGetValue(path, out var addFile)) {
            return addFile;
        }

        var file = new InterpreterFile {
            FS      = this,
            Path    = path,
            Content = content ?? string.Empty
        };

        Files[path] = file;

        if (IsPhysical && content == null) {
            if (File.Exists(file.Abs)) {
                file.Content = File.ReadAllText(file.Abs);
            }
        }

        OnFileAdded?.Invoke(file);

        return file;
    }
    
    public InterpreterFile GetFromAbsPath(string absPath) {
        var relPath = Path.GetRelativePath(Root, absPath);
        return Files[relPath];
    }
}

public class IncrementalInterpreterFileSystem : InterpreterFileSystem
{
    public IncrementalInterpreterFileSystem() { }
    public IncrementalInterpreterFileSystem(string root, bool isPhysical) : base(root, isPhysical, false) { }

    public override void Initialize(string root, bool isPhysical, bool loadAllFiles = true) {
        base.Initialize(root, isPhysical, loadAllFiles);
    }
}