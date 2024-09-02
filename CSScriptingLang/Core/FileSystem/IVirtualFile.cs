namespace CSScriptingLang.Core.FileSystem;

public interface IVirtualFile : IVirtualEntry
{
    string Content { get; set; }

    string Ext { get; }
    string NameWithoutExt { get; }
}

public class VirtualFile : IVirtualFile
{
    protected bool _isInitialized = false;

    public IVirtualFileSystem Parent { get; set; }

    public string Root { get; protected set; }
    public string Name { get; protected set; }
    public string Dir  { get; protected set; }

    public string Abs => Path.Combine(Root, Dir);

    // If root is `C:\`, and dir is `C:\Users\file.txt`, then the relative path is `Users\file.txt`
    public string Rel => Path.GetRelativePath(Root, Abs);
    
    public string NameWithoutExt => Path.GetFileNameWithoutExtension(Name);
    public string Ext => Path.GetExtension(Name);

    public virtual string Content { get; set; }

    public VirtualFile(string path, string root) {
        Name    = Path.GetFileName(path);
        Dir     = path;
        Root    = root;
        Content = string.Empty;
    }

    public virtual void Initialize() {
        if (_isInitialized)
            return;

        _isInitialized = true;
    }
}

public class PhysicalFile : VirtualFile
{
    public override string Content { get; set; }

    public PhysicalFile(string path, string root) : base(path, root) {
        Name = Path.GetFileName(path);
        Dir  = path;
    }

    public override void Initialize() {
        if (_isInitialized)
            return;

        base.Initialize();

        Content = File.ReadAllText(Abs);
    }
}