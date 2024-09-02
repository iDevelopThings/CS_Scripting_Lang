using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.RuntimeValues;
using CSScriptingLang.Utils;
using Engine.Engine.Logging;

namespace CSScriptingLang.Interpreter;

public class SymbolTable : IDisposable
{
    private Logger Logger = Logs.Get<SymbolTable>();
    public  Logger RefCountLogger { get; set; } = Logs.Get("RefCount", LogLevel.Warning);

    public InterpreterExecutionContext Context { get; set; }
    public SymbolTable                 Parent  { get; set; }

    public Dictionary<string, Symbol>                  Symbols              = new();
    public Dictionary<string, FunctionDeclarationNode> FunctionDeclarations = new();

    // public static Dictionary<string, FunctionDeclarationNode> NativeFunctions = new();

    public SymbolTable(InterpreterExecutionContext context, SymbolTable parent = null) {
        Context = context;
        Parent  = parent;
    }

    public Symbol this[string name] {
        get => Get(name);
        set => Set(name, value.Value);
    }

    /// <summary>
    /// Compared to set and such, this just adds a symbol without it's type/value etc
    /// </summary>
    public Symbol Define(string name) {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        if (Symbols.TryGetValue(name, out var define))
            return define;

        if (Parent != null)
            return Parent.Define(name);

        var symbol = new Symbol(name, null);
        symbol.AddReference(this);

        Symbols[name] = symbol;

        return symbol;
    }
    public Symbol AddToScope(string name, Symbol symbol) {
        if (Contains(name, out var existing)) {
            existing.Value = symbol.Value;
            existing.AddReference(this);
            return existing;
        }

        Symbols[name] = symbol;
        symbol.AddReference(this);

        return symbol;
    }
    public Symbol AddToScope(string name, RuntimeValue value) {
        if (Contains(name, out var symbol, false)) {
            symbol.Value = value;
            symbol.AddReference(this);
            return symbol;
        }

        Symbols[name] = symbol = new Symbol(name, value);
        symbol.AddReference(this);

        return symbol;
    }

    public Symbol Set(string name, RuntimeValue value) {
        if (Contains(name, out var symbol)) {
            symbol.Value = value;
            symbol.AddReference(this);
            return symbol;
        }

        Symbols[name] = symbol = new Symbol(name, value);
        symbol.AddReference(this);

        return symbol;
    }
    public bool Contains(string name, bool includeParent = true) {
        if (Symbols.ContainsKey(name))
            return true;
        if (Parent != null && includeParent)
            return Parent.Contains(name);
        return false;
    }
    public bool Contains(string name, out Symbol symbol, bool includeParent = true) {
        if (Symbols.TryGetValue(name, out symbol))
            return true;
        if (Parent != null && includeParent)
            return Parent.Contains(name, out symbol);
        return false;
    }

    public bool Get(string name, out Symbol symbol) {
        if (Symbols.TryGetValue(name, out symbol))
            return true;
        if (Parent != null)
            return Parent.Get(name, out symbol);
        return false;
    }
    public T Get<T>(string name) where T : RuntimeValue {
        if (Symbols.TryGetValue(name, out var symbol))
            return symbol.Value as T;
        if (Parent != null)
            return Parent.Get<T>(name);
        return null;
    }

    public Symbol Get(string name) {
        if (Symbols.TryGetValue(name, out var symbol))
            return symbol;
        if (Parent != null)
            return Parent.Get(name);
        return null;
    }


    public bool GetFunctionDeclaration(string name, out FunctionDeclarationNode declaration) {
        if (FunctionDeclarations.TryGetValue(name, out declaration))
            return true;
        if (Parent != null)
            return Parent.GetFunctionDeclaration(name, out declaration);

        if (Context.ModuleRegistry.TryGetFunction(name, out declaration))
            return true;

        return false;
    }
    public FunctionDeclarationNode GetFunctionDeclaration(string name) {
        if (FunctionDeclarations.TryGetValue(name, out var declaration))
            return declaration;
        if (Parent != null)
            return Parent.GetFunctionDeclaration(name);
        if (Context.ModuleRegistry.TryGetFunction(name, out declaration))
            return declaration;

        return null;

    }

    // public static void DeclareNativeFunction(Func<FunctionDeclarationNode> Factory) {
    //     var decl = Factory();
    //     NativeFunctions.TryAdd(decl.Name, decl);
    // }
    public void DeclareFunction(FunctionDeclarationNode declaration) => DeclareFunction(declaration.Name, declaration);
    public void DeclareFunction(string name, FunctionDeclarationNode declaration) {
        if (!FunctionDeclarations.TryAdd(name, declaration)) {

            var existing = FunctionDeclarations[name];


            Logger.Error(
                $"Function '{name.BoldBrightRed()}' is already declared in module '{existing.Cursor.First.Parent<Module>()?.Name.BoldBrightWhite()}', and re-declared in module '{declaration.Cursor.First.Parent<Module>()?.Name.BoldBrightWhite()}'");
        }
    }

    public void Dispose() { }

    
    public override string ToString() {
        return $"SymbolTable({Context.Module.Name} - depth={Context.GetDepth()}) Symbols={Symbols.Count} {(Context?.ContextObject != null ? $"ContextObject: {Context?.ContextObject}" : "")}";
    }

    public List<string> DecrementReferences() {
        var toRemove = new List<string>();

        foreach (var symbol in Symbols.Values) {
            symbol.RemoveReference(this);
            if (symbol.ReferenceCount == 0) {
                toRemove.Add(symbol.Name);
            }
        }

        foreach (var name in toRemove) {
            Symbols.Remove(name);
        }

        return toRemove;
    }
    public List<Symbol> GetReferenced(int greaterThan = 0) {
        return Symbols.Values.Where(s => s.ReferenceCount > greaterThan).ToList();
    }

    // Return true if we have disposed all values and have no references
    public bool TryDisposeValues() {
        var toRemove        = DecrementReferences();
        var referenceCounts = GetReferenced();
        
        if (toRemove.Count > 0) {
            RefCountLogger.Debug($"Removed {toRemove.Count} symbols from '{ToString().BoldBrightWhite()}'");
            foreach (var r in toRemove) {
                RefCountLogger.Debug($"\t- {r}");
            }
        }

        var referenced = referenceCounts.Where(s => s.ReferenceCount > 0).ToList();
        if (referenced.Count > 0) {
            var debugStr = "";
            foreach (var r in referenced) {
                debugStr += $"\t- {r.Name} RefCount = {r.ReferenceCount}\n" +
                            $"{r.GetReferencesString()}\n" +
                            $"\n";
            }
            
            RefCountLogger.Debug($"Table('{ToString().BoldBrightWhite()}') still has references: {referenceCounts.Count}\n{debugStr}");
            
            return false;
        }
        
        return true;
    }
}