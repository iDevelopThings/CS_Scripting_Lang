using System.Collections;
using System.Text;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.Interpreter.Libraries;

public interface ILibraryCollection
{
    IEnumerable<ILibrary> Create(ExecContext ctx);
}

public interface ILibrary
{
    IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx);
}

public class LibraryManager : IEnumerable<ILibraryCollection>
{
    private List<ILibraryCollection> Factories = new();

    public Action<ExecContext, LibraryCollection> OnConfigure { get; set; }
    public Action<LibraryManager>                 OnPreLoad   { get; set; }

    public LibraryManager() { }

    public LibraryManager Configure(Action<ExecContext, LibraryCollection> configAction) {
        ArgumentNullException.ThrowIfNull(configAction);

        if (OnConfigure == null) {
            OnConfigure = configAction;
        } else {
            OnConfigure += configAction;
        }

        return this;
    }

    public void Load(ExecContext ctx, Action<LibraryCollection> configAction = null) {

        var definitionNames = new HashSet<string>();

        OnPreLoad?.Invoke(this);

        var libs = new LibraryFactoryCollection(Factories);
        libs.Setup(ctx);

        foreach (var (name, value) in libs.Definitions) {
            if (!definitionNames.Add(name))
                throw new Exception($"Duplicate definition for '{name}'");

            ctx.Variables.Set(name, value);
        }

        var libraryCollection = new LibraryCollection(libs);

        OnConfigure?.Invoke(ctx, libraryCollection);

        configAction?.Invoke(libraryCollection);
    }

    public void Add(ILibraryCollection item) {
        ArgumentNullException.ThrowIfNull(item);

        Factories.Add(item);
    }

    public void Add(ILibrary item) {
        ArgumentNullException.ThrowIfNull(item);

        Factories.Add(new SingleLibraryCollection(item));
    }

    public IEnumerator<ILibraryCollection> GetEnumerator() {
        return Factories.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}

public class LibraryEqualityComparer : IEqualityComparer<ILibrary>
{
    public bool Equals(ILibrary x, ILibrary y) {
        return x?.GetType() == y?.GetType();
    }

    public int GetHashCode(ILibrary obj) {
        return obj.GetType().GetHashCode();
    }
}

public class LibraryFactoryCollection
{
    public List<ILibrary>                        Libraries { get; set; }
    public List<ILibraryCollection>              Factories { get; set; }
    public List<KeyValuePair<string, Value>> Definitions = new();

    public LibraryFactoryCollection(List<ILibraryCollection> factories) {
        Factories = factories ?? throw new ArgumentNullException(nameof(factories));
    }

    public LibraryFactoryCollection(List<ILibrary> libraries) {
        Libraries = libraries ?? throw new ArgumentNullException(nameof(libraries));
    }

    public void Setup(ExecContext ctx) {
        if (Factories != null)
            Libraries = Factories
               .SelectMany(f => f.Create(ctx))
               .ToList();

        if (Libraries != null) {
            Definitions = Libraries
               .Distinct(new LibraryEqualityComparer())
               .SelectMany(l => l.GetDefinitions(ctx))
               .ToList();
        }
    }
}

public class SingleLibraryCollection : ILibraryCollection
{
    private ILibrary library { get; set; }

    public SingleLibraryCollection(ILibrary lib) {
        library = lib ?? throw new ArgumentNullException(nameof(lib));
    }

    public IEnumerable<ILibrary> Create(ExecContext ctx) {
        yield return library;
    }
}

public class LibraryCollection : IEnumerable<ILibrary>
{
    private List<ILibrary>                        Libraries   { get; set; }
    public  List<KeyValuePair<string, Value>> Definitions { get; set; }

    internal LibraryCollection(List<ILibrary> libraries) {
        Libraries = libraries;
    }
    public LibraryCollection(LibraryFactoryCollection libs) {
        Libraries   = libs.Libraries;
        Definitions = libs.Definitions;
    }

    public T Get<T>() where T : ILibrary {
        return Libraries.OfType<T>().FirstOrDefault();
    }

    public IEnumerator<ILibrary> GetEnumerator() {
        return Libraries.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}