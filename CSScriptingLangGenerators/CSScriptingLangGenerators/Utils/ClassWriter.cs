using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Utils;

public static class String_Extensions
{
    public static void ForEach(this string str, Action<char> action) {
        foreach (var c in str) {
            action(c);
        }
    }
}

public class ClassWriter_ClassData
{
    public string        Name          { get; set; }
    public Accessibility Accessibility { get; set; } = Accessibility.Public;

    public List<ClassWriter_FieldData>               Fields       { get; set; } = new();
    public Dictionary<string, ClassWriter_FieldData> FieldsByName { get; set; } = new();

    public List<ClassWriter_MethodData>               Methods       { get; set; } = new();
    public Dictionary<string, ClassWriter_MethodData> MethodsByName { get; set; } = new();

    public bool IsInterface { get; set; } = false;
    public bool IsStatic    { get; set; }
    public bool IsPartial   { get; set; } = true;
    public bool IsSealed    { get; set; }

    public NameOrTypeData          BaseClass  { get; set; }
    public HashSet<NameOrTypeData> Interfaces { get; set; } = new();

    private Dictionary<string, ClassWriter_ClassData> _classesByName = new();
    private List<ClassWriter_ClassData>               _classes       = new();

    public ClassWriter_ClassData Class(string name, Action<ClassWriter_ClassData> action) {
        var c = new ClassWriter_ClassData {
            Name          = name,
            Accessibility = Accessibility.Public,
        };

        _classesByName.Add(c.Name, c);
        _classes.Add(c);

        action(c);

        return c;
    }
    public ClassWriter_ClassData Class(INamedTypeSymbol symbol, Action<ClassWriter_ClassData> action) {
        var c = new ClassWriter_ClassData {
            Name          = symbol.Name,
            Accessibility = symbol.DeclaredAccessibility,
            IsStatic      = symbol.IsStatic,
        };

        _classesByName.Add(c.Name, c);
        _classes.Add(c);

        action(c);

        return c;
    }

    public NameOrTypeData Extends(string name) {
        var b = new NameOrTypeData(name);
        BaseClass = b;
        return b;
    }
    public NameOrTypeData Extends(INamedTypeSymbol type) {
        var b = new NameOrTypeData(type);
        BaseClass = b;
        return b;
    }

    public NameOrTypeData Implements(string name) {
        var i = new NameOrTypeData(name);
        Interfaces.Add(i);
        return i;
    }
    public NameOrTypeData Implements(INamedTypeSymbol type) {
        var i = new NameOrTypeData(type);
        Interfaces.Add(i);
        return i;
    }

    public ClassWriter_FieldData Property(string name) {
        var p = new ClassWriter_FieldData {
            Name          = name,
            Accessibility = Accessibility.Public,
        };

        Fields.Add(p);
        FieldsByName.Add(name, p);

        return p;
    }
    public ClassWriter_ClassData Property(string name, Action<ClassWriter_FieldData> action) {
        var p = Property(name);
        action(p);
        return this;
    }

    public ClassWriter_MethodData Method(string name) {
        var m = new ClassWriter_MethodData {
            Name              = name,
            Accessibility     = Accessibility.Public,
            IsInterfaceMethod = IsInterface,
        };

        Methods.Add(m);
        MethodsByName.Add(name, m);

        return m;
    }
    public ClassWriter_MethodData Method(string name, Action<ClassWriter_MethodData> action) {
        var m = Method(name);
        action(m);
        return m;
    }
    public void WriteTo(ClassWriter w) {
        w.Write($"{Accessibility.ToString().ToLower()} ");
        if (IsStatic)
            w.Write("static ");
        if (IsSealed)
            w.Write("sealed ");
        if (IsPartial)
            w.Write("partial ");

        if (IsInterface)
            w.Write("interface ");
        else
            w.Write("class ");

        w.Write(Name);

        if (Interfaces.Count > 0 || BaseClass != null) {
            w.Write(" : ");

            var exts = new List<NameOrTypeData>();
            if (BaseClass != null)
                exts.Add(BaseClass);
            exts.AddRange(Interfaces);

            w.Write(string.Join(", ", exts.Select(i => i.ToString())));
        }

        using (w.Block()) {
            foreach (var c in _classes) {
                c.WriteTo(w);
            }

            foreach (var f in Fields) {
                f.WriteTo(w);
            }

            w.WriteLine();

            foreach (var m in Methods) {
                m.WriteTo(w);
            }
        }
    }
}

public record NameOrTypeData
{
    public string           Name { get; set; }
    public INamedTypeSymbol Type { get; set; }

    public NameOrTypeData() { }
    public NameOrTypeData(string name) {
        Name = name;
    }
    public NameOrTypeData(INamedTypeSymbol type) {
        Type = type;
        Name = type.Name;
    }

    public NameOrTypeData With(string name) {
        Name = name;
        return this;
    }
    public NameOrTypeData With(INamedTypeSymbol type) {
        Type = type;
        return this;
    }

    public override string ToString() {
        return Name ?? Type.Name ?? throw new Exception("NameOrTypeData is not initialized");
    }
}

public class ClassWriter_TypedIdentifierData
{
    public INamedTypeSymbol Type     { get; set; }
    public string           TypeName { get; set; }
    public string           Name     { get; set; }

    public string DefaultValue { get; set; }

    public ClassWriter_TypedIdentifierData WithType(INamedTypeSymbol type) {
        Type     = type;
        TypeName = type.Name;
        return this;
    }

    public ClassWriter_TypedIdentifierData WithType(string type) {
        TypeName = type;
        return this;
    }

    public ClassWriter_TypedIdentifierData WithName(string name) {
        Name = name;
        return this;
    }

    public ClassWriter_TypedIdentifierData WithDefaultValue(string value) {
        DefaultValue = value;
        return this;
    }
}

public class ClassWriter_FieldData : ClassWriter_TypedIdentifierData
{
    public Accessibility Accessibility { get; set; }

    public bool HasGetter  { get; set; } = true;
    public bool HasSetter  { get; set; } = true;
    public bool IsStatic   { get; set; }
    public bool IsAbstract { get; set; }
    public bool IsVirtual  { get; set; }
    public bool IsOverride { get; set; }


    public ClassWriter_FieldData WithGetterSetter(bool getter = true, bool setter = true) {
        HasGetter = getter;
        HasSetter = setter;
        return this;
    }
    public ClassWriter_FieldData WithAccessibility(Accessibility accessibility) {
        Accessibility = accessibility;
        return this;
    }
    public ClassWriter_FieldData WithStatic(bool isStatic = true) {
        IsStatic = isStatic;
        return this;
    }
    public new ClassWriter_FieldData WithType(INamedTypeSymbol type) {
        Type     = type;
        TypeName = type.Name;
        return this;
    }

    public new ClassWriter_FieldData WithType(string type) {
        TypeName = type;
        return this;
    }

    public new ClassWriter_FieldData WithName(string name) {
        Name = name;
        return this;
    }
    public virtual void WriteTo(ClassWriter w) {
        w.Write($"{Accessibility.ToString().ToLower()} ");

        if (IsOverride) {
            w.Write("override ");
        } else if (IsVirtual) {
            w.Write("virtual ");
        } else if (IsAbstract) {
            w.Write("abstract ");
        }

        if (IsStatic)
            w.Write("static ");

        w.Write($"{TypeName} {Name}");

        if (HasGetter || HasSetter) {
            w.Write(" {");
            if (HasGetter)
                w.Write(" get;");
            if (HasSetter)
                w.Write(" set;");
            w.Write(" }");
        }

        if (DefaultValue != null) {
            w.Write($" = {DefaultValue}");
            w.Write(";");
        }

        w.WriteLine();
    }
}

public class ClassWriter_MethodData : ClassWriter_FieldData
{
    public List<ClassWriter_TypedIdentifierData> Parameters { get; set; } = new();

    public bool IsInterfaceMethod { get; set; }

    public ClassWriter Body { get; set; } = new();

    public ClassWriter_MethodData Parameter(string name, Action<ClassWriter_TypedIdentifierData> action) {
        var p = new ClassWriter_TypedIdentifierData {
            Name = name,
        };

        action(p);

        Parameters.Add(p);

        return this;
    }
    public ClassWriter_MethodData Parameter(string name, string type, Action<ClassWriter_TypedIdentifierData> action) =>
        Parameter(
            name, p => {
                p.WithType(type);
                action(p);
            }
        );
    public ClassWriter_MethodData Parameter(string name, INamedTypeSymbol type, Action<ClassWriter_TypedIdentifierData> action) =>
        Parameter(
            name, p => {
                p.WithType(type);
                action(p);
            }
        );

    public override void WriteTo(ClassWriter w) {
        w.Write($"{Accessibility.ToString().ToLower()} ");

        if (IsOverride)
            w.Write("override ");
        else if (IsVirtual)
            w.Write("virtual ");
        else if (IsAbstract)
            w.Write("abstract ");

        if (IsStatic)
            w.Write("static ");

        w.Write($"{(string.IsNullOrWhiteSpace(TypeName) ? "void" : TypeName)} ");
        w.Write(Name);
        w.Write("(");
        w.Write(string.Join(", ", Parameters.Select(p => $"{p.TypeName} {p.Name}")));
        w.Write(")");

        if (IsInterfaceMethod) {
            w.Write(";");
            w.WriteLine();
            return;
        }

        using (w.Block()) {
            var output = Body.ToString();
            if (!string.IsNullOrWhiteSpace(output)) {
                w.Write(output);
            }
        }
    }

}

public class ClassWriter : StringWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    private       int    _indentLevel = 0;
    private       bool   _isNewLine   = true;
    private const string IndentString = "    "; // 4 spaces for indentation

    private string       _namespace;
    private List<string> _imports     = new();
    private List<string> _headerLines = new();

    private HashSet<ISymbol> _usingSymbols = new(SymbolEqualityComparer.Default);

    private Dictionary<string, ClassWriter_ClassData> _classesByName = new();
    private List<ClassWriter_ClassData>               _classes       = new();

    public ClassWriter() { }

    private ClassWriter Apply(Action action) {
        action();
        return this;
    }

    public void IncreaseIndent() => _indentLevel++;
    public void DecreaseIndent() => _indentLevel = Math.Max(0, _indentLevel - 1);

    private void WriteIndent() => base.Write(new string(' ', _indentLevel * IndentString.Length));

    private void WriteIndentIfNeeded() {
        if (!_isNewLine)
            return;

        WriteIndent();

        _isNewLine = false;
    }

    public override void Write(char value) {
        WriteIndentIfNeeded();
        base.Write(value);
        if (value == '\n') {
            _isNewLine = true;
        }
    }
    public override void WriteLine() {
        base.WriteLine();
        _isNewLine = true;
    }

    public override void Write(string value) => value.ForEach(Write);

    public ClassWriter Import(string    import)  => Apply(() => _imports.Add(import));
    public ClassWriter Imports(string[] imports) => Apply(() => _imports.AddRange(imports));
    public ClassWriter Using(ISymbol symbol) {
        if (symbol is INamedTypeSymbol namedTypeSymbol) {
            _usingSymbols.Add(namedTypeSymbol);
        }
        return this;
    }
    public ClassWriter Using(IEnumerable<ISymbol> symbols) {
        foreach (var symbol in symbols) {
            Using(symbol);
        }
        return this;
    }
    public ClassWriter Namespace(string @namespace) => Apply(() => _namespace = @namespace);

    public ClassWriter Namespace(ISymbol symbol) {
        var ns = (symbol as INamedTypeSymbol)?.GetFullNamespace();
        if (ns != null)
            Namespace(ns);
        return this;
    }

    public ClassWriter AddHeaderLine(string    line)  => Apply(() => _headerLines.Add(line));
    public ClassWriter AddHeaderLines(string[] lines) => Apply(() => _headerLines.AddRange(lines));


    public ClassWriter_ClassData Class(INamedTypeSymbol symbol, Action<ClassWriter_ClassData> action) {
        var c = new ClassWriter_ClassData {
            Name          = symbol.Name,
            Accessibility = symbol.DeclaredAccessibility,
            IsStatic      = symbol.IsStatic,
        };

        _classesByName.Add(c.Name, c);
        _classes.Add(c);

        action(c);

        return c;
    }
    public ClassWriter_ClassData Class(string name, Action<ClassWriter_ClassData> action) {
        var c = new ClassWriter_ClassData {
            Name = name,
        };

        _classesByName.Add(c.Name, c);
        _classes.Add(c);

        action(c);

        return c;
    }

    public ClassWriter_ClassData Interface(INamedTypeSymbol symbol, Action<ClassWriter_ClassData> action) {
        return Class(
            symbol, data => {
                data.IsInterface = true;
                action(data);
            }
        );
    }
    public ClassWriter_ClassData Interface(string name, Action<ClassWriter_ClassData> action) {
        return Class(
            name, data => {
                data.IsInterface = true;
                action(data);
            }
        );
    }

    public IDisposable Block() {
        OpenBlock();
        return new BlockHelper(this);
    }
    public IDisposable Block(string line) {
        WriteLine(line);
        return Block();
    }

    public void OpenBlock() {
        WriteLine("{");
        IncreaseIndent();
    }
    public void CloseBlock() {
        DecreaseIndent();
        WriteLine("}");
    }

    public override string ToString() {
        var importStrings = _usingSymbols
           .Select(
                s => {
                    if (s is INamespaceSymbol ns)
                        return ns.ToDisplayString();
                    if (s is INamedTypeSymbol namedType)
                        return namedType.GetFullNamespace();
                    return s.ToDisplayString();
                }
            )
           .Concat(_imports.Select(i => i))
           .Distinct()
           .ToList();

        if (importStrings.Count > 0) {
            foreach (var import in importStrings) {
                WriteLine($"using {import};");
            }
            WriteLine();
        }

        if (_namespace != null) {
            WriteLine($"namespace {_namespace};");
            WriteLine();
        }

        if (_headerLines.Count > 0) {
            foreach (var line in _headerLines) {
                WriteLine(line);
            }
            WriteLine();
        }

        foreach (var c in _classes) {
            c.WriteTo(this);
            WriteLine();
        }

        return base.ToString();
    }

    private class BlockHelper : IDisposable
    {
        private readonly ClassWriter _writer;

        public BlockHelper(ClassWriter writer) {
            _writer = writer;
        }

        public void Dispose() {
            _writer.CloseBlock();
        }
    }


}