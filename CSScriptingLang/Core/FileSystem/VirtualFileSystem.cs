using System.Text.RegularExpressions;
using Engine.Engine.Logging;

namespace CSScriptingLang.Core.FileSystem;

using System;
using System.Collections.Generic;
using System.IO;

public interface IVirtualEntry
{
    IVirtualFileSystem Parent { get; set; }

    string Name { get; }
    string Dir  { get; }
    string Abs  { get; }
    string Rel  { get; }

    void Initialize();
}

public interface IVirtualFileSystem : IVirtualEntry
{
    Dictionary<string, IVirtualEntry> All { get; }

    IVirtualFile       CreateFile(string      path, string content = "");
    IVirtualFileSystem CreateDirectory(string path);

    bool FileExists(string path);
    bool Exists(string     path);

    bool               GetDirectory(string    path, out IVirtualFileSystem dir);
    IVirtualFileSystem GetDirectory(string    path);
    bool               DirectoryExists(string path);

    IVirtualFile GetFile(string path);
    bool         GetFile(string path, out IVirtualFile file);

    string ReadFile(string  path);
    void   WriteFile(string path, string content);
    void   Delete(string    path);

    IEnumerable<IVirtualEntry>      Entries(bool     recursive = false);
    IEnumerable<IVirtualFile>       Files(bool       recursive = false);
    IEnumerable<IVirtualFileSystem> Directories(bool recursive = false);


    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <returns></returns>
    public string PatternToRegex(string pattern);

    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <param name="printOutput"></param>
    /// <returns></returns>
    public IEnumerable<IVirtualEntry> List(string pattern, bool printOutput = false);

    void PrintTree();
}

public class VirtualDirectory : IVirtualFileSystem
{
    private Logger Logger = Logs.Get<VirtualDirectory>();

    protected bool _isInitialized = false;

    public FileSystemRootPtr Root { get; set; }

    public string Dir { get; set; }
    public string Abs => Path.Combine(Root, Dir);
    public string Rel => Path.GetRelativePath(Root, Abs);

    public string Name        => Path.GetFileName(Dir);
    public bool   IsDirectory => true;

    public IVirtualFileSystem Parent { get; set; }

    public Dictionary<string, IVirtualEntry> All { get; set; } = new();

    public VirtualDirectory(string path, FileSystemRootPtr root) {
        Dir  = path;
        Root = root;
    }

    public virtual void Initialize() {
        if (_isInitialized)
            return;

        _isInitialized = true;

        Root.OnChange += (newRoot, oldRoot) => {
            Console.WriteLine($"[{(this is PhysicalDirectory ? "PhysicalDirectory" : "VirtualDirectory")}] Root changed: From {oldRoot} to {newRoot.Abs}");

            foreach (var entry in All.Values) {
                entry.Parent = this;
            }
        };
    }

    protected virtual IVirtualFile CreateNewFile(string path) {
        return new VirtualFile(path, Root) {
            Parent = this,
        };
    }

    protected virtual IVirtualFileSystem CreateNewDirectory(string path) {
        return new VirtualDirectory(path, Root) {
            Parent = this,
        };
    }

    public string GetPath(string path) => Path.Combine(Dir, path);

    protected (IVirtualFileSystem dir, string[] parts) NestedDirPath(string path) {
        var parts = path.Split('/');

        IVirtualFileSystem dir = this;
        for (var i = 0; i < parts.Length - 1; i++) {
            var part = parts[i];
            dir = !dir.DirectoryExists(part) ? dir.CreateDirectory(part) : dir.GetDirectory(part);
        }

        return (dir, parts);
    }

    // Create a file
    public virtual IVirtualFile CreateFile(string path, string content = "") {
        if (path.Contains('/')) {
            var (dir, parts) = NestedDirPath(path);
            return dir.CreateFile(parts[^1], content);
        }

        if (All.ContainsKey(path)) {
            Logger.Warning($"File already exists at path: {path}");
            return All[path] as IVirtualFile;
        }

        var file = CreateNewFile(GetPath(path));

        All[path] = file;

        file.Content = content;

        return file;
    }

    // Create a directory
    public virtual IVirtualFileSystem CreateDirectory(string path) {
        if (path.Contains('/')) {
            var (dir, parts) = NestedDirPath(path);
            return dir.CreateDirectory(parts[^1]);
        }

        if (All.ContainsKey(path)) {
            throw new InvalidOperationException($"Directory already exists at path: {path}");
        }

        var newDir = CreateNewDirectory(GetPath(path));

        All[path] = newDir;

        return newDir;
    }
    public virtual bool FileExists(string path) => All.ContainsKey(path) && All[path] is IVirtualFile;

    // Check if file or directory exists
    public virtual bool Exists(string path) {
        return All.ContainsKey(path);
    }

    public virtual bool GetDirectory(string path, out IVirtualFileSystem dir) {
        if (path.Contains('/')) {
            var (dirPath, parts) = NestedDirPath(path);
            dir                  = dirPath.GetDirectory(parts[^1]);
            return dir != null;
        }

        dir = GetDirectory(path);
        return dir != null;
    }
    public virtual IVirtualFileSystem GetDirectory(string path) {
        if (path.Contains('/')) {
            var (dir, parts) = NestedDirPath(path);
            return dir.GetDirectory(parts[^1]);
        }

        return All[path] as IVirtualFileSystem;
    }
    public virtual bool DirectoryExists(string path) {
        return Exists(path) && All[path] is IVirtualFileSystem;
    }
    public virtual bool GetFile(string path, out IVirtualFile file) {
        file = GetFile(path);
        return file != null;
    }
    public virtual IVirtualFile GetFile(string path) {
        return All[path] as IVirtualFile;
    }

    // Read file content
    public virtual string ReadFile(string path) {
        if (!GetFile(path, out var file)) {
            throw new FileNotFoundException($"File not found: {path}");
        }

        return file.Content;
    }

    // Write content to file
    public virtual void WriteFile(string path, string content) {
        if (!GetFile(path, out var file)) {
            throw new FileNotFoundException($"File not found: {path}");
        }

        file.Content = content;
    }

    // Delete file or directory
    public virtual void Delete(string path) {
        if (!All.ContainsKey(path)) {
            throw new FileNotFoundException($"File or directory not found: {path}");
        }

        All.Remove(path);
    }

    public virtual IEnumerable<IVirtualEntry> Entries(bool recursive = false) {
        if (recursive) {
            foreach (var entry in All.Values) {
                yield return entry;

                if (entry is not IVirtualFileSystem dir)
                    continue;

                foreach (var child in dir.Entries(true)) {
                    yield return child;
                }
            }
        } else {
            foreach (var entry in All.Values) {
                yield return entry;
            }
        }
    }
    public IEnumerable<IVirtualFile>       Files(bool       recursive = false) => Entries(recursive).OfType<IVirtualFile>();
    public IEnumerable<IVirtualFileSystem> Directories(bool recursive = false) => Entries(recursive).OfType<IVirtualFileSystem>();

    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <returns></returns>
    public string PatternToRegex(string pattern) {
        // ** matches any number of directories recursively
        // * matches any number of characters except for /
        // **/*.ext matches all files with the extension .ext in all directories recursively
        // **/file.ext matches file.ext in any directory
        // *.ext matches all files with the extension .ext in the current directory

        var regexPattern = pattern
           .Replace("\\", "/")
           .Replace("**/", "<recursiveDir>")
           .Replace("*", "<anyChar>");

        var recursiveDir = "(.*[^/]).";
        var anyChar      = "[^\\/]*";

        var finalPattern = regexPattern
           .Replace("<recursiveDir>", recursiveDir)
           .Replace("<anyChar>", anyChar);

        if (!finalPattern.StartsWith('^'))
            finalPattern = $"^{finalPattern}";

        return finalPattern;
    }

    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <param name="printOutput"></param>
    /// <returns></returns>
    public IEnumerable<IVirtualEntry> List(string pattern, bool printOutput = false) {
        if (printOutput) {
            Console.WriteLine($"\nPattern: {pattern}");
        }

        var regexPattern = PatternToRegex(pattern);

        foreach (var entry in ListRecursive(this)) {
            if (Test(entry)) {
                yield return entry;
            }
        }

        yield break;

        IEnumerable<IVirtualEntry> ListRecursive(IVirtualEntry entry) {
            if (entry is IVirtualFileSystem dir) {
                foreach (var e in dir.Files()) {
                    yield return e;
                }

                foreach (var e in dir.Directories()) {
                    yield return e;

                    foreach (var child in ListRecursive(e)) {
                        yield return child;
                    }
                }
            }
        }

        bool Test(IVirtualEntry entry) {
            var testAgainst = entry.Rel.Replace(@"\", "/");
            var isMatch     = Regex.IsMatch(testAgainst, regexPattern);
            if (printOutput)
                Console.WriteLine($"[{(entry is IVirtualFile ? "File" : "Dir")}] {testAgainst} -> {regexPattern}: {isMatch}");
            return isMatch;
        }
    }

    public void PrintTree() {
        Console.WriteLine($"\nFrom -> {Root}\n");

        var indent = 2;
        var stack  = new Stack<IVirtualFileSystem>();
        stack.Push(this);

        while (stack.Count > 0) {
            var dir = stack.Pop();
            Console.WriteLine($"{new string(' ', indent)}{dir.Rel}/");

            indent += 2;

            dir.Directories().ToList().ForEach(stack.Push);
            dir.Files().ToList().ForEach(f => Console.WriteLine($"{new string(' ', indent)}{f.Rel}"));

            indent -= 2;
        }
    }
}

public class PhysicalDirectory : VirtualDirectory
{
    public PhysicalDirectory(string path, FileSystemRootPtr root) : base(path, root) { }

    public override void Initialize() {
        if (_isInitialized)
            return;

        base.Initialize();

        if (!Directory.Exists(Abs)) {
            throw new DirectoryNotFoundException($"Directory not found: {Abs}");
        }

        foreach (var file in Directory.GetFiles(Abs)) {
            var relPath = Path.GetRelativePath(Root, file);
            var f       = CreateNewFile(relPath);

            if (All.ContainsKey(relPath))
                continue;

            All.Add(relPath, f);
        }

        foreach (var dir in Directory.GetDirectories(Abs)) {
            var relDir    = Path.GetRelativePath(Root, dir);
            var directory = CreateNewDirectory(relDir);

            if (All.ContainsKey(relDir))
                continue;

            All.Add(relDir, directory);
        }

        foreach (var entry in All.Values) {
            entry.Initialize();
        }

    }

    protected override IVirtualFile CreateNewFile(string path) {
        return new PhysicalFile(path, Root) {
            Parent = this
        };
    }

    protected override IVirtualFileSystem CreateNewDirectory(string path) {
        var d = new PhysicalDirectory(path, Root) {
            Parent = this
        };

        return d;
    }
    // public override bool FileExists(string path) => All.ContainsKey(path) /*&& File.Exists(All[path].Abs)*/;
    // public override bool Exists(string path) {
    //     return All.ContainsKey(path) && File.Exists(All[path].Dir);
    // }
    // public override bool DirectoryExists(string path) {
    //     return All.ContainsKey(path) && Directory.Exists(All[path].Abs);
    // }
    public override string ReadFile(string  path)                 => GetFile(path).Content;
    public override void   WriteFile(string path, string content) => GetFile(path).Content = content;

    public override void Delete(string path) {
        All.Remove(path);
    }
}

public class FileSystemRootPtr
{
    private string _root;
    public  string Root => _root;

    public FileSystemRootPtr(string root) {
        _root = root;
    }

    public Action<IVirtualFileSystem, string> OnChange;

    public void SetRoot(string newRoot) {
        _root = newRoot;
    }
    public void SetRoot(IVirtualFileSystem fs) {
        var prev_root = _root;
        _root = fs.Dir;
        OnChange?.Invoke(fs, prev_root);
    }

    public static implicit operator string(FileSystemRootPtr ptr) => ptr.Root;

    public override string ToString() {
        return Root;
    }
}

public class FileSystem : IVirtualFileSystem
{
    public IVirtualFileSystem Implementation { get; set; }
    public bool               IsPhysical     { get; set; }
    public FileSystemRootPtr  Root           { get; set; }

    public IVirtualFileSystem Parent {
        get => Up();
        set => Implementation.Parent = value;
    }
    public string Name => Implementation.Name;
    public string Dir  => Implementation.Dir;
    public string Abs  => Implementation.Abs;
    public string Rel  => Implementation.Rel;

    public Dictionary<string, IVirtualEntry> All => Implementation.All;

    public FileSystem(FileSystem child, string rootPath) {
        IsPhysical = child.IsPhysical;

        Root = child.Root;
        Root.SetRoot(rootPath);

        Implementation = CreateNewDirectory(rootPath, Root);
        child.Parent   = Implementation;

        Implementation.All[child.Name] = child;

        Root.SetRoot(this);

        Root.OnChange += (newRoot, oldRoot) => {
            Console.WriteLine($"[FS] Root changed: From {oldRoot} to {newRoot.Abs}");
        };

        Initialize();
    }

    public FileSystem(string rootPath, bool isPhysical = false) {
        IsPhysical     = isPhysical;
        Root           = new FileSystemRootPtr(rootPath);
        Implementation = CreateNewDirectory(rootPath, Root);

        Root.OnChange += (newRoot, oldRoot) => {
            Console.WriteLine($"[FS] Root changed: From {oldRoot} to {newRoot.Abs}");
        };

        Initialize();
    }

    private IVirtualFileSystem CreateNewDirectory(string path, FileSystemRootPtr root) {
        return IsPhysical
            ? new PhysicalDirectory(path, root)
            : new VirtualDirectory(path, root);
    }

    public static FileSystem FromCwd(bool isPhysical = false) {
        return new FileSystem(Directory.GetCurrentDirectory(), isPhysical);
    }

    public void Initialize() => Implementation.Initialize();

    private IVirtualFileSystem Up() {
        var up = Implementation.Parent;
        if (up != null)
            return up;


        // Go up one(`..`) if the current directory is the root
        var newRootPath = Path.GetFullPath(Path.Combine(Implementation.Dir, ".."));
        var newRootFs   = new FileSystem(this, newRootPath);

        // var parent = CreateNewDirectory(newRootPath, Root);
        Root.SetRoot(newRootFs);

        return newRootFs;
    }

    public IVirtualFile GetFile(string path)                        => Implementation.GetFile(path);
    public bool         GetFile(string path, out IVirtualFile file) => Implementation.GetFile(path, out file);

    public IVirtualFile       CreateFile(string      path, string content = "") => Implementation.CreateFile(path, content);
    public IVirtualFileSystem CreateDirectory(string path) => Implementation.CreateDirectory(path);
    public bool               FileExists(string      path) => Implementation.FileExists(path);
    public bool               Exists(string          path) => Implementation.Exists(path);

    public bool               GetDirectory(string    path, out IVirtualFileSystem dir) => Implementation.GetDirectory(path, out dir);
    public IVirtualFileSystem GetDirectory(string    path) => Implementation.GetDirectory(path);
    public bool               DirectoryExists(string path) => Implementation.DirectoryExists(path);

    public string ReadFile(string  path)                 => Implementation.ReadFile(path);
    public void   WriteFile(string path, string content) => Implementation.WriteFile(path, content);
    public void   Delete(string    path) => Implementation.Delete(path);

    public void PrintTree() => Implementation.PrintTree();

    public IEnumerable<IVirtualEntry>      Entries(bool     recursive = false) => Implementation.Entries(recursive);
    public IEnumerable<IVirtualFile>       Files(bool       recursive = false) => Implementation.Files(recursive);
    public IEnumerable<IVirtualFileSystem> Directories(bool recursive = false) => Implementation.Directories(recursive);


    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <returns></returns>
    public string PatternToRegex(string pattern) => Implementation.PatternToRegex(pattern);

    /// <summary>
    /// List all files/dir in the filesystem matching the search pattern
    /// </summary>
    /// <param name="pattern">
    /// A simple pattern like `file_*_.ext` would match files like `file_1.txt`, `file_2.txt`, etc.
    /// A pattern like `**/*.ext` would match all files with the extension `.ext` in all directories recursively.
    /// </param>
    /// <param name="printOutput"></param>
    /// <returns></returns>
    public IEnumerable<IVirtualEntry> List(string pattern, bool printOutput = false) => Implementation.List(pattern, printOutput);
}