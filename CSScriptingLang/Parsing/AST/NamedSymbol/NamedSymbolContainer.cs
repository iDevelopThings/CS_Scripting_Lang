using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Interpreter.Modules;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Parsing.AST.NamedSymbol;

public class NamedSymbols : SortedList<NamedSymbolPosition, NamedSymbolInformation>
{
    private NamedSymbolContainer _container;

    public NamedSymbols(NamedSymbolContainer container) {
        _container = container;
    }

    public IEnumerable<NamedSymbolInformation> All() => Values;

    public void Add(NamedSymbolInformation info) {
        Add(info.Position, info);
        //_container.GetScriptList(info.Script.RelativePath).Add(info.Position, info);
    }

    public bool TryGetByPosition(Position position, out NamedSymbolInformation info) {
        if (TryGetValue(position.ToNamedSymbolPosition(), out info)) {
            return true;
        }
        if (TryGetValue(position.ToNamedSymbolPosition(true), out info)) {
            return true;
        }

        for (var i = 0; i < Count; i++) {
            var value = Values[i];
            if (value.Range.Contains(position)) {
                info = value;
                return true;
            }
        }

        info = NamedSymbolInformation.Empty;
        return false;
    }

    public NamedSymbols this[string scriptPath] => _container.GetScriptList(scriptPath);
    public NamedSymbols this[Script script] => _container.GetScriptList(script.RelativePath);

    public bool TryGetByScript(string scriptPath, Position position, out NamedSymbolInformation info) {
        if (_container.ByScript.TryGetValue(scriptPath, out var byScript)) {
            if (byScript.TryGetByPosition(position, out info)) {
                return true;
            }
        }
        info = NamedSymbolInformation.Empty;
        return false;
    }
}

public class NamedSymbolContainer
{
    // public NamedSymbols                     ByPosition;
    public Dictionary<string, NamedSymbols> ByScript;

    public static NamedSymbolContainer Instance => ModuleResolver.NamedSymbols;

    // public IEnumerable<NamedSymbolInformation> All() => ByPosition.Values;

    public NamedSymbolContainer() {
        // ByPosition = new NamedSymbols(this);
        ByScript = new Dictionary<string, NamedSymbols>();
    }

    public NamedSymbols GetScriptList(string scriptPath) {
        if (!ByScript.TryGetValue(scriptPath, out var byScript)) {
            byScript             = new NamedSymbols(this);
            ByScript[scriptPath] = byScript;
        }

        return byScript;
    }

    public void ClearScriptSymbols(string scriptPath) {
        if (ByScript.TryGetValue(scriptPath, out var byScript)) {
            // foreach (var info in byScript.Values) {
            // ByPosition.Remove(info.Position);
            // }
            byScript.Clear();
        }
    }

    public NamedSymbols this[string scriptPath] => GetScriptList(scriptPath);
    public NamedSymbols this[Script script] => GetScriptList(script.RelativePath);

    public void Add(INamedSymbolProvider provider) {
        if (provider is BaseNode node) {
            var s = GetScriptList(node.GetScript().RelativePath);
            foreach (var info in provider.GetNamedSymbols()) {
                s.Add(info);
            }
        }
        if (provider is SyntaxNode snode) {
            var s = GetScriptList(snode.GetScript().RelativePath);
            foreach (var info in provider.GetNamedSymbols()) {
                s.Add(info);
            }
        }
    }

}