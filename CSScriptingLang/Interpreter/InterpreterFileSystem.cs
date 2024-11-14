using System.ComponentModel;
using CSScriptingLang.Core;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Core.Logging;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter;

public partial class InterpreterFile : INotifyPropertyChanged
{
    protected string _content;

    public InterpreterFileSystem FS { get; set; }

    public string Path { get; set; }
    public string Content {
        get => _content;
        set => _content = value.NormalizeLineEndings();
    }

    public string Name => System.IO.Path.GetFileName(Path);
    public string Dir  => System.IO.Path.GetDirectoryName(Abs);
    public string Abs  => System.IO.Path.Combine(FS.Root, Path);
    public string Rel  => System.IO.Path.GetRelativePath(FS.Root, Abs);

    public Action<InterpreterFile> OnChanged { get; set; }

    public int Version { get; set; }

    public virtual void UpdateContent(string content) {
        Content = content.NormalizeLineEndings();
        FS.OnFileChanged?.Invoke(this);
        OnChanged?.Invoke(this);
    }

    public virtual void ReloadContent() {
        try {
            using var fs         = File.OpenRead(Abs);
            using var reader     = new StreamReader(fs);
            var       newContent = reader.ReadToEnd().NormalizeLineEndings();

            if (newContent != Content) {
                Content = newContent;
                FS.OnFileChanged?.Invoke(this);
                OnChanged?.Invoke(this);
            }
        }
        // handle `process cannot access the file because it is being used by another process`
        catch (IOException) {
            // InterpreterFileSystem.Logger.Warning($"File '{Path}' is being used by another process");
        }
    }

}

public partial class VirtualInterpreterFile : InterpreterFile
{
    public VirtualInterpreterFile() { }
    public VirtualInterpreterFile(string path, string content) {
        Path    = path;
        Content = content;
    }

    public override void ReloadContent() { }
}

public class InterpreterFileSystem
{
    public static Logger Logger = Logs.Get<InterpreterFileSystem>();

    public Dictionary<string, InterpreterFile> Files { get; set; } = new();

    public string Root       { get; set; }
    public bool   IsPhysical { get; set; }

    public Action<InterpreterFile> OnFileChanged { get; set; }
    public Action<InterpreterFile> OnFileAdded   { get; set; }
    public Action<InterpreterFile> OnFileRemoved { get; set; }

    public FileSystemWatcher Watcher { get; set; }

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

        if (IsPhysical && InterpreterConfig.WatchRootDirectory) {
            Watcher                       =  new FileSystemWatcher();
            Watcher.Path                  =  Root;
            Watcher.IncludeSubdirectories =  true;
            Watcher.NotifyFilter          =  NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            Watcher.Filter                =  $"*{Script.Extension}";
            Watcher.Changed               += Watcher_OnChanged;
            Watcher.Created               += Watcher_OnCreated;
            Watcher.Deleted               += Watcher_OnDeleted;
            Watcher.Renamed               += Watcher_OnRenamed;
            Watcher.EnableRaisingEvents   =  true;

        }
    }

    private void Watcher_OnCreated(object sender, FileSystemEventArgs e) {
        AddFile(e.Name!);
    }

    private void Watcher_OnChanged(object sender, FileSystemEventArgs e) {
        if (Files.TryGetValue(e.Name!, out var file)) {
            file.ReloadContent();
        } else {
            AddFile(e.Name!);
        }
    }
    private void Watcher_OnDeleted(object sender, FileSystemEventArgs e) {
        if (Files.TryGetValue(e.Name!, out var file)) {
            Files.Remove(e.Name!);
            OnFileRemoved?.Invoke(file);
        }
    }
    private void Watcher_OnRenamed(object sender, RenamedEventArgs e) {
        if (Files.TryGetValue(e.OldName!, out var file)) {
            Files.Remove(e.OldName!);
            file.Path      = e.Name!;
            Files[e.Name!] = file;
        } else {
            AddFile(e.Name!);
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

    public IEnumerable<InterpreterFile> GetAllAtPath(string path) {
        path = RelativePath(path);

        // We should only check `Files` and not the actual file system

        var files = Files.Where(f => f.Key.StartsWith(path))
           .Select(f => f.Value);

        return files;
    }

    public VirtualInterpreterFile AddVirtualFile(string content) {
        var contentHash = content.GetHashCode();
        var path        = $"__virtual_file_{contentHash}.vlt";

        var file = new VirtualInterpreterFile(path, content);
        Files[path] = file;

        OnFileAdded?.Invoke(file);

        return file;
    }

    public InterpreterFile AddFile(string path, string content = null) {
        path = RelativePath(path);

        if (Files.TryGetValue(path, out var addFile)) {
            return addFile;
        }

        var file = new InterpreterFile {
            FS      = this,
            Path    = path,
            Content = (content ?? string.Empty).NormalizeLineEndings(),
        };

        Files[path] = file;

        if (IsPhysical && content == null) {
            if (File.Exists(file.Abs)) {
                file.Content = File.ReadAllText(file.Abs).NormalizeLineEndings();
            }
        }

        OnFileAdded?.Invoke(file);

        // file.OnChanged?.Invoke(file);

        return file;
    }

    public InterpreterFile GetFile(string path) {
        path = RelativePath(path);
        return Files[path!];
    }
    public InterpreterFile GetFromAbsPath(string absPath) {
        var relPath = Path.GetRelativePath(Root, absPath);
        return Files[relPath];
    }

    public bool Exists(string path) {
        path = RelativePath(path);
        return Files.ContainsKey(path!);
    }
    public string Combine(string dir, string name) {
        if (Path.IsPathRooted(dir)) {
            dir = Path.GetRelativePath(Root, dir);
        } else {
            dir = Path.Combine(Root, dir!);
        }

        dir = Path.GetFullPath(dir);

        return Path.Combine(dir!, name);
    }
    public string RelativePath(string path) {
        if (Path.IsPathRooted(path)) {
            return Path.GetRelativePath(Root, path);
        }
        return path;
    }
    public string AbsolutePath(string rawPath) {
        if (Path.IsPathRooted(rawPath)) {
            return rawPath;
        }
        return Path.GetFullPath(Path.Combine(Root, rawPath!));
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