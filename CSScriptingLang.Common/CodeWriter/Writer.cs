using System.Text;

namespace CSScriptingLang.Common.CodeWriter;

public class Writer
{
    private CodeWriterSettings _settings;
    private StringBuilder      _sb;
    private bool               _newLineOnBlockEnd;

    private int _currentBlockDepth;
    public int CurrentBlockDepth {
        get => _parent?.CurrentBlockDepth ?? _currentBlockDepth;
        set {
            if (_parent != null) {
                _parent.CurrentBlockDepth = value;
            } else {
                _currentBlockDepth = value;
            }
        }
    }

    private int _indent;
    public int Indent {
        get => _parent?.Indent ?? _indent;
        set {
            if (_parent != null) {
                _parent.Indent = value;
            } else {
                _indent = value;
            }
        }
    }

    public CodeWriterSettings Settings  => _settings;
    public List<string>       HeadLines { get; set; }

    private Writer _parent;

    public Writer() {
        _settings = CodeWriterSettings.CSharpDefault;
        _sb       = new StringBuilder();
        _indent   = 0;
    }

    public Writer(CodeWriterSettings settings) {
        _settings = settings;
    }
    public Writer(Writer parent) {
        if (parent == null) {
            _settings = CodeWriterSettings.CSharpDefault;
            _sb       = new StringBuilder();
            return;
        }

        _settings = parent._settings;
        _sb       = new StringBuilder();
        _parent   = parent;
    }

    public void SetSettings(CodeWriterSettings settings) {
        _settings = settings;
    }

    public void WriteRaw(string str = null) {
        if (str != null)
            _sb.Append(str);
    }

    public void WriteInlineIndented(string str = null) {
        if (_newLineOnBlockEnd) {
            if (str != null)
                _sb.Append(_settings.NewLine);

            _newLineOnBlockEnd = false;
        }

        if (str != null) {
            _sb.Append(GetIndentString());
            _sb.Append(str);
        } else {
            _sb.Append(_settings.NewLine);
        }
    }
    public void WriteInline(string str = null) {
        if (_newLineOnBlockEnd) {
            if (str != null)
                _sb.Append(_settings.NewLine);

            _newLineOnBlockEnd = false;
        }

        if (str != null) {
            _sb.Append(str);
        } else {
            _sb.Append(_settings.NewLine);
        }
    }

    public void Write(string str = null) {
        if (_newLineOnBlockEnd) {
            if (str != null)
                _sb.Append(_settings.NewLine);

            _newLineOnBlockEnd = false;
        }

        WriteInternal(str);
    }

    public void Write(string str, params string[] strs) {
        Write(str);
        foreach (var s in strs)
            Write(s);
    }

    private void WriteInternal(string str = null) {
        if (str != null) {
            _sb.Append(GetIndentString());
            _sb.Append(str);
            _sb.Append(_settings.NewLine);
        } else {
            _sb.Append(_settings.NewLine);
        }
    }

    public UsingHandle OpenBlock(string[] strs, bool newLineAfterBlockEnd = false, bool indent = true) {
        if (_newLineOnBlockEnd) {
            _sb.Append(_settings.NewLine);
            _newLineOnBlockEnd = false;
        }

        if (strs.Any()) {
            for (var i = 0; i < strs.Length; i++) {
                if (indent) {
                    _sb.Append(GetIndentString(i == 0 ? 0 : 1));
                }

                _sb.Append(strs[i]);
                if (i < strs.Length - 1)
                    _sb.Append(_settings.NewLine);
            }

            if (_settings.NewLineBeforeBlockBegin) {
                _sb.Append(_settings.NewLineBeforeBlockBegin ? _settings.NewLine : " ");
                Write(_settings.BlockBegin);
                CurrentBlockDepth++;
            } else {
                _sb.Append(" ");
                _sb.Append(_settings.BlockBegin);
                _sb.Append(_settings.NewLine);
                CurrentBlockDepth++;
            }
        } else {
            Write(_settings.BlockBegin);
            CurrentBlockDepth++;
        }

        IncIndent();
        return new UsingHandle(() => {
            DecIndent();
            WriteInternal(_settings.BlockEnd);

            CurrentBlockDepth--;

            _newLineOnBlockEnd = newLineAfterBlockEnd;
        });
    }


    public void WriteArray<T>(IEnumerable<T> elements, Action<T, Writer> writeElement, string separator = ", ") {
        var temp = new Writer(this);

        var first = true;
        foreach (var element in elements) {
            if (!first)
                temp.WriteInline(separator);

            writeElement(element, temp);
            first = false;
        }

        Write(temp.ToString());
    }

    public void Array<T>(IEnumerable<T> elements, Action<T, Writer> writeElement, bool newLineBetweenElements = false) {
        if (_newLineOnBlockEnd) {
            _sb.Append(_settings.NewLine);
            _newLineOnBlockEnd = false;
        }

        var strsWriter = new Writer(this);
        var first      = true;
        foreach (var element in elements) {
            if (!first)
                strsWriter.WriteInline(", ");
            writeElement(element, strsWriter);
            first = false;
        }

        _sb.Append($"Array({elements.Count()}) {_settings.ArrayBlockBegin} ");
        _sb.Append(strsWriter);
        _sb.Append($" {_settings.ArrayBlockEnd}");
    }

    public UsingHandle OpenIndent(string begin, string end, bool newLineAfterBlockEnd = false) {
        if (_newLineOnBlockEnd) {
            _sb.Append(_settings.NewLine);
            _newLineOnBlockEnd = false;
        }

        if (begin != null) {
            _sb.Append(GetIndentString());
            _sb.Append(begin);
            _sb.Append(_settings.NewLine);
        }

        IncIndent();
        return new UsingHandle(() => {
            DecIndent();
            if (end != null)
                WriteInternal(end);

            _newLineOnBlockEnd = newLineAfterBlockEnd;
        });
    }

    public void IncIndent() {
        Indent += 1;
    }

    public void DecIndent() {
        if (Indent == 0)
            throw new InvalidOperationException("Cannot decrease indent.");

        Indent -= 1;
    }

    public string GetIndentString(int additional = 0) {
        return string.Concat(Enumerable.Repeat(_settings.Indent, Indent + additional));
    }

    public override string ToString() {
        var headComment = HeadLines != null
            ? string.Join(_settings.NewLine, HeadLines) + _settings.NewLine
            : "";

        var text = headComment + _sb.ToString();
        if (_settings.TranslationMapping != null) {
            foreach (var i in _settings.TranslationMapping) {
                text = text.Replace(i.Key, i.Value);
            }
        }

        return text;
    }

    public bool WriteAllText(string path, bool skipNotChanged = true) {
        return WriteAllText(path, skipNotChanged, Encoding.UTF8);
    }

    public bool WriteAllText(string path, bool skipNotChanged, Encoding encoding) {
        var text = ToString();

        if (skipNotChanged && File.Exists(path)) {
            var existingText = File.ReadAllText(path);
            if (existingText == text) {
                return false;
            }
        }

        File.WriteAllText(path, text, encoding);
        return true;
    }
}