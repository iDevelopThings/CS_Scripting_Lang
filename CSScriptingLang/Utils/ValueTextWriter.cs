using System.Globalization;
using System.Text;
using CSScriptingLang.Common.Extensions;

namespace CSScriptingLang.Utils;

public class ValueTextWriter : TextWriter
{
    private TextWriter writer;
    private int        indentLevel;
    private bool       tabsPending;
    private string     tabString;

    private const string DefaultTabString = "  ";

    public ValueTextWriter() : base(CultureInfo.InvariantCulture) {
        writer      = new StringWriter();
        tabString   = DefaultTabString;
        indentLevel = 0;
        tabsPending = false;
    }

    public ValueTextWriter(ValueTextWriter parent) : this() {
        // writer      = parent;
        indentLevel = parent.indentLevel;
        tabString   = parent.tabString;
        tabsPending = parent.tabsPending;
    }
    public ValueTextWriter(TextWriter inWriter) : this() {
        writer = inWriter;
    }
    public ValueTextWriter(StringBuilder inWriter) : this(new StringWriter(inWriter)) { }

    public ValueTextWriter SetTabString(string tabStr) {
        tabString = tabStr;
        return this;
    }
    public ValueTextWriter SetIndent(int indent) {
        indentLevel = indent;
        return this;
    }

    public virtual StringBuilder GetStringBuilder() {
        if (writer is StringWriter sw)
            return sw.GetStringBuilder();

        throw new InvalidOperationException("Cannot get string from non-StringBuilder writer");
    }

    private void UpdateFromChild(ValueTextWriter c) {
        indentLevel = c.indentLevel;
        tabString   = c.tabString;
        tabsPending = c.tabsPending;
    }

    public override string ToString() {
        if (writer is StringWriter sw)
            return sw.GetStringBuilder().ToString();

        throw new InvalidOperationException("Cannot get string from non-StringBuilder writer");
    }

    public override Encoding Encoding => writer.Encoding;

    public override string NewLine {
        get => writer.NewLine;
        set => writer.NewLine = value;
    }

    public int Indent {
        get => indentLevel;
        set {
            if (value < 0)
                value = 0;
            indentLevel = value;
        }
    }

    public TextWriter InnerWriter => writer;

    internal string TabString => tabString;

    public override void Close() => writer.Close();

    public override void Flush() => writer.Flush();

    protected virtual void OutputTabs() {
        if (!tabsPending)
            return;
        for (int index = 0; index < indentLevel; ++index)
            writer.Write(tabString);
        tabsPending = false;
    }

    public override void Write(string s) {
        OutputTabs();
        writer.Write(s);
    }

    public override void Write(bool value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(char value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(char[] buffer) {
        OutputTabs();
        writer.Write(buffer);
    }

    public override void Write(char[] buffer, int index, int count) {
        OutputTabs();
        writer.Write(buffer, index, count);
    }

    public override void Write(double value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(float value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(int value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(long value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(object value) {
        OutputTabs();
        writer.Write(value);
    }

    public override void Write(string format, object arg0) {
        OutputTabs();
        writer.Write(format, arg0);
    }

    public override void Write(string format, object arg0, object arg1) {
        OutputTabs();
        writer.Write(format, arg0, arg1);
    }

    public override void Write(string format, params object[] arg) {
        OutputTabs();
        writer.Write(format, arg);
    }

    public void WriteLineNoTabs(string s) => writer.WriteLine(s);

    public override void WriteLine(string s) {
        OutputTabs();
        writer.WriteLine(s);
        tabsPending = true;
    }

    public override void WriteLine() {
        OutputTabs();
        writer.WriteLine();
        tabsPending = true;
    }

    public override void WriteLine(bool value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(char value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(char[] buffer) {
        OutputTabs();
        writer.WriteLine(buffer);
        tabsPending = true;
    }

    public override void WriteLine(char[] buffer, int index, int count) {
        OutputTabs();
        writer.WriteLine(buffer, index, count);
        tabsPending = true;
    }

    public override void WriteLine(double value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(float value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(int value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(long value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(object value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    public override void WriteLine(string format, object arg0) {
        OutputTabs();
        writer.WriteLine(format, arg0);
        tabsPending = true;
    }

    public override void WriteLine(string format, object arg0, object arg1) {
        OutputTabs();
        writer.WriteLine(format, arg0, arg1);
        tabsPending = true;
    }

    public override void WriteLine(string format, params object[] arg) {
        OutputTabs();
        writer.WriteLine(format, arg);
        tabsPending = true;
    }

    public override void WriteLine(uint value) {
        OutputTabs();
        writer.WriteLine(value);
        tabsPending = true;
    }

    protected override void Dispose(bool disposing) {
        if (disposing && writer is ValueTextWriter w) {
            w.UpdateFromChild(this);
        }
    }


    internal void InternalOutputTabs() {
        for (int index = 0; index < indentLevel; ++index)
            writer.Write(tabString);
    }

    public void OpenBlock(char openChar = '{', bool newLineBeforeOpen = true, bool newLineOnOpen = true) {
        if (newLineBeforeOpen)
            WriteLine();

        if (newLineOnOpen) {
            WriteLine(openChar);
        } else
            Write(openChar);

        Indent++;
    }

    public void CloseBlock(char closeChar = '}', bool newLineBeforeClose = true, bool newLineOnClose = true) {
        if (newLineBeforeClose)
            WriteLine();

        Indent--;

        if (newLineOnClose) {
            WriteLine(closeChar);
        } else
            Write(closeChar);
    }
    public UsingCallbackHandle Block(
        char openChar           = '{',
        char closeChar          = '}',
        bool newLineBeforeOpen  = true, bool newLineOnOpen  = true,
        bool newLineBeforeClose = true, bool newLineOnClose = true
    ) {
        OpenBlock(openChar, newLineBeforeOpen, newLineOnOpen);

        return new UsingCallbackHandle(() => {
            CloseBlock(closeChar, newLineBeforeClose, newLineOnClose);
        });
    }


    public void WriteDocumentation(string comment) {
        // if comment is multiline, write it as a block comment
        if (comment.Contains(Environment.NewLine)) {
            WriteLine("/**");
            comment.Split(Environment.NewLine).ForEach(l => WriteLine($" * {l}"));
            WriteLine(" */");
        } else {
            WriteLine($"// {comment}");
        }
    }


    public class ObjectWriter : ValueTextWriter
    {
        public struct ObjectField
        {
            public string       Key;
            public string       Value;
            public Func<string> ValueWriterFn;
            public bool         IsField   = true;
            public bool         IsComment = false;

            public ObjectField() {
                Key   = null;
                Value = null;
            }
            public ObjectField(string key, string value) {
                Key   = key;
                Value = value;
            }
            public ObjectField(string key, Func<string> valueWriterFn) {
                Key           = key;
                ValueWriterFn = valueWriterFn;
            }
        }

        private ValueTextWriter ParentWriter { get; set; }
        private bool            wasBuilt = false;

        public List<ObjectField> Fields { get; } = new();

        public bool NewLineBeforeOpen { get; set; }
        public bool NewLineOnOpen     { get; set; }

        public bool NewLineBeforeClose { get; set; }
        public bool NewLineOnClose     { get; set; }

        public ObjectWriter(
            ValueTextWriter writer,
            bool            newLineBeforeOpen  = true,
            bool            newLineOnOpen      = true,
            bool            newLineBeforeClose = true,
            bool            newLineOnClose     = true
        ) : base(writer) {
            ParentWriter       = writer;
            NewLineBeforeOpen  = newLineBeforeOpen;
            NewLineOnOpen      = newLineOnOpen;
            NewLineBeforeClose = newLineBeforeClose;
            NewLineOnClose     = newLineOnClose;
        }

        public ObjectWriter Add(ValueTextWriter w) {
            return Add(() => {
                if (w is ObjectWriter or ArrayWriter) {
                    w.Dispose(true);
                }
                return w.ToString();
            });
        }
        public ObjectWriter Add(string key, ValueTextWriter w) {
            return Add(key, () => {
                if (w is ObjectWriter or ArrayWriter) {
                    w.Dispose(true);
                }
                return w.ToString();
            });
        }
        public ObjectWriter Add(Func<string> valueWriterFn) => Add(null, valueWriterFn);
        public ObjectWriter Add(string key, Func<string> valueWriterFn) {
            Fields.Add(new ObjectField(key, valueWriterFn));
            return this;
        }
        public ObjectWriter Add(string key, string value) {
            Fields.Add(new ObjectField(key, value));
            return this;
        }
        public ObjectWriter AddComment(string value) {
            Fields.Add(new ObjectField {
                Key       = null,
                Value     = value,
                IsField   = false,
                IsComment = true,
            });
            return this;
        }

        private void Build() {
            if (wasBuilt) return;
            wasBuilt = true;

            if (Fields.Count == 0) {
                Write($"{{ {"<empty>".BoldBrightGray()} }}");

                return;
            }

            OpenBlock('{', NewLineBeforeOpen, NewLineOnOpen);

            var maxKeyLength = Fields
               .Where(x => x.IsField)
               .Max(x => x.Key.Length);

            for (int i = 0; i < Fields.Count; i++) {
                var field = Fields[i];
                var value = field.ValueWriterFn?.Invoke() ?? field.Value;

                // if value ends with new line, remove it
                if (value.EndsWith(ParentWriter.NewLine)) {
                    value = value[..^ParentWriter.NewLine.Length];
                }

                if (field.IsComment) {
                    WriteLine($"{"// ".BrightGreen()}{value.BoldBrightGray()}");
                    continue;
                }
                Write(field.Key.PadRight(maxKeyLength));
                Write(" : ");
                Write(value);
                if (i < Fields.Count - 1)
                    WriteLine(",");
            }

            CloseBlock('}', NewLineBeforeClose, NewLineOnClose);
        }
        public override string ToString() {
            Build();

            return GetStringBuilder().ToString();
        }

        protected override void Dispose(bool disposing) {
            if (disposing && !wasBuilt) {
                Build();
            }
        }


    }


    /// <summary>
    /// We want to write a json like object
    /// <code>
    /// {
    ///     key1: value1,
    /// }
    /// </code>
    /// </summary>
    public ValueTextWriter WriteObject(
        Action<ObjectWriter> writeAction,
        bool                 newLineBeforeOpen  = true,
        bool                 newLineOnOpen      = true,
        bool                 newLineBeforeClose = true,
        bool                 newLineOnClose     = false
    ) {
        indentLevel++;
        var w = new ObjectWriter(
            this,
            newLineBeforeOpen,
            newLineOnOpen,
            newLineBeforeClose,
            newLineOnClose
        );
        writeAction(w);

        indentLevel--;

        return w;
    }
    public ValueTextWriter InstantWriteObject(
        Action<ObjectWriter> writeAction,
        bool                 newLineBeforeOpen  = true,
        bool                 newLineOnOpen      = true,
        bool                 newLineBeforeClose = true,
        bool                 newLineOnClose     = false
    ) {
        using var w = new ObjectWriter(
            this,
            newLineBeforeOpen,
            newLineOnOpen,
            newLineBeforeClose,
            newLineOnClose
        );
        writeAction(w);

        Write(w.ToString());

        return this;
    }

    public class ArrayWriter : ValueTextWriter
    {
        public List<string> Values { get; } = new();

        public bool NewLineBeforeOpen { get; set; }
        public bool NewLineOnOpen     { get; set; }

        public bool NewLineBeforeClose { get; set; }
        public bool NewLineOnClose     { get; set; }

        public ArrayWriter(
            ValueTextWriter writer,
            bool            newLineBeforeOpen  = false,
            bool            newLineOnOpen      = true,
            bool            newLineBeforeClose = true,
            bool            newLineOnClose     = false
        ) : base(writer) {
            NewLineBeforeOpen  = newLineBeforeOpen;
            NewLineOnOpen      = newLineOnOpen;
            NewLineBeforeClose = newLineBeforeClose;
            NewLineOnClose     = newLineOnClose;
        }

        public ArrayWriter Add(string value) {
            Values.Add(value);
            return this;
        }

        protected override void Dispose(bool disposing) {
            if (Values.Count == 0) {
                WriteLine($"{{ {"<empty>".BoldBrightGray()} }}");
                return;
            }

            OpenBlock('[', NewLineBeforeOpen, NewLineOnOpen);

            // use the item index as the key
            var maxKeyLength = Values.Count.ToString().Length;

            for (int i = 0; i < Values.Count; i++) {
                var item = Values[i];

                Write($"{i}".PadRight(maxKeyLength));
                Write(" : ");
                Write(item);
                if (i < Values.Count - 1)
                    WriteLine(",");
            }

            CloseBlock(']', NewLineBeforeClose, NewLineOnClose);
        }


        public void Add(ValueTextWriter w) { }
    }

    public ValueTextWriter WriteArray(
        Action<ArrayWriter> writeAction,
        bool                newLineBeforeOpen  = true,
        bool                newLineOnOpen      = true,
        bool                newLineBeforeClose = true,
        bool                newLineOnClose     = false
    ) {
        using var w = new ArrayWriter(
            this,
            newLineBeforeOpen,
            newLineOnOpen,
            newLineBeforeClose,
            newLineOnClose
        );

        writeAction(w);

        return w;
    }


    public struct StructMemberParameter
    {
        public string Name;
        public string Type;
        public bool   IsVariadic;

        public StructMemberParameter(string name, string type, bool isVariadic) {
            Name       = name;
            Type       = type;
            IsVariadic = isVariadic;
        }
        public StructMemberParameter((string name, string type, bool isVariadic) p) {
            Name       = p.name;
            Type       = p.type;
            IsVariadic = p.isVariadic;
        }

        public (string name, string type, bool isVariadic) ToTuple() => (Name, Type, IsVariadic);

        public static implicit operator (string name, string type, bool isVariadic)(StructMemberParameter p) => p.ToTuple();

        public static implicit operator StructMemberParameter((string name, string type, bool isVariadic) p) => new() {
            Name       = p.name,
            Type       = p.type,
            IsVariadic = p.isVariadic,
        };

        public string ToDefinitionString() {
            return $"{(IsVariadic ? "..." : "")}{Type} {Name}";
        }
    }

    public class StructWriter : ValueTextWriter
    {
        public struct StructMember
        {
            public enum StructMemberType
            {
                Field,
                Constructor,
                Method,
            }

            public StructMemberType            Kind;
            public string                      Name;
            public string                      Type;
            public string                      Body;
            public List<StructMemberParameter> Parameters;
            public string                      Comment;
            public bool                        IsDef = false;

            public string DefinitionString = null;

            public StructMember() {
                Kind    = StructMemberType.Field;
                Name    = null;
                Type    = null;
                Comment = null;
            }
            public StructMember(string name, string type) {
                Kind = StructMemberType.Field;
                Name = name;
                Type = type;
            }
            public StructMember SetComment(string comment) {
                Comment = comment;
                return this;
            }

            public void Write(ValueTextWriter w) {
                if (!string.IsNullOrWhiteSpace(Comment)) {
                    w.WriteLine(Comment);
                }

                switch (Kind) {
                    case StructMemberType.Field: {
                        if (!string.IsNullOrWhiteSpace(DefinitionString)) {
                            w.WriteLine(DefinitionString);
                            return;
                        }
                        w.WriteLine($"{Name} {Type}");
                        break;
                    }
                    case StructMemberType.Constructor:
                    case StructMemberType.Method: {
                        if (!string.IsNullOrWhiteSpace(DefinitionString)) {
                            w.WriteLine(DefinitionString);
                            return;
                        }

                        w.WriteLine("");

                        if (IsDef)
                            w.Write("def ");

                        w.Write(
                            Kind == StructMemberType.Constructor
                                ? $"{Name}"
                                : Name
                        );
                        w.Write("(");
                        w.Write(Parameters.Select(p => p.ToDefinitionString()).Join(", "));
                        w.Write(") ");

                        if (!string.IsNullOrWhiteSpace(Type)) {
                            w.Write(Type);
                        }

                        if (IsDef) {
                            w.Write(";");
                        } else {
                            if (!string.IsNullOrWhiteSpace(Body)) {
                                w.WriteLine(" {");
                                w.WriteLine(Body);
                                w.WriteLine("}");
                            } else {
                                w.WriteLine(";");
                            }
                        }

                        break;
                    }
                }
            }
        }

        private ValueTextWriter ParentWriter { get; set; }
        private bool            wasBuilt = false;

        public string Name { get; set; }

        public List<StructMember> Members { get; set; } = new();

        public StructWriter(
            ValueTextWriter writer
        ) : base(writer) {
            ParentWriter = writer;
        }

        public StructWriter AddField(string definition, Func<StructMember, StructMember> onAdded = null) {
            var member = new StructMember() {
                Kind             = StructMember.StructMemberType.Field,
                DefinitionString = definition,
            };
            member = onAdded?.Invoke(member) ?? member;
            Members.Add(member);
            return this;
        }
        public StructWriter AddField(string key, string type) {
            Members.Add(new StructMember(key, type));
            return this;
        }
        public StructWriter AddField(string key, string type, string comment) {
            Members.Add(new StructMember(key, type).SetComment(comment));
            return this;
        }

        public StructWriter AddConstructor(string definition, Action<StructMember> onAdded = null) {
            Members.Add(new StructMember() {
                Kind             = StructMember.StructMemberType.Constructor,
                DefinitionString = definition,
            });
            onAdded?.Invoke(Members[^1]);
            return this;
        }
        public StructWriter AddConstructor(
            IEnumerable<(string name, string type, bool isVariadic)> parameters,
            bool                                                     isDef   = false,
            Action<StructMember>                                     onAdded = null
        ) {
            Members.Add(new StructMember() {
                Kind       = StructMember.StructMemberType.Constructor,
                Name       = Name,
                Parameters = parameters.Select(p => (StructMemberParameter) p).ToList(),
                IsDef      = isDef,
            });
            onAdded?.Invoke(Members[^1]);
            return this;
        }

        public StructWriter AddMethod(string definition, Func<StructMember, StructMember> onAdded = null) {
            Members.Add(new StructMember() {
                Kind             = StructMember.StructMemberType.Method,
                DefinitionString = definition,
            });

            var member = Members[^1];
            member = onAdded?.Invoke(member) ?? member;
            Members[^1] = member;

            return this;
        }
        public StructWriter AddMethod(
            string                                                   name,
            IEnumerable<(string name, string type, bool isVariadic)> parameters,
            string                                                   returnType,
            string                                                   body    = null,
            bool                                                     isDef   = false,
            Func<StructMember, StructMember>                         onAdded = null
        ) {
            Members.Add(new StructMember() {
                Kind       = StructMember.StructMemberType.Method,
                Name       = name,
                Type       = returnType,
                Parameters = parameters.Select(p => (StructMemberParameter) p).ToList(),
                IsDef      = isDef,
                Body       = body,
            });

            var member = Members[^1];
            member      = onAdded?.Invoke(member) ?? member;
            Members[^1] = member;

            return this;
        }

        public string Build(bool rebuild = false) {
            if (wasBuilt && !rebuild)
                return GetStringBuilder().ToString();

            wasBuilt = true;

            WriteLine();
            Write($"type {Name} struct");

            OpenBlock();

            // Order by kind, in order of Field, Constructor, Method
            Members = Members.OrderBy(m => m.Kind).ToList();

            foreach (var member in Members) {
                member.Write(this);
            }

            CloseBlock();

            WriteLine();

            return GetStringBuilder().ToString();
        }

        public override string ToString() => Build();

        protected override void Dispose(bool disposing) {
            if (disposing && !wasBuilt) {
                Build();
            }
        }


    }

    public ValueTextWriter WriteStruct(
        string               name,
        Action<StructWriter> writeAction
    ) {
        using var w = new StructWriter(this) {
            Name = name,
        };
        writeAction(w);

        Write(w.ToString());

        return this;
    }
    public StructWriter Struct(string name) {
        var w = new StructWriter(this) {
            Name = name,
        };
        return w;
    }

    public class MethodWriter : ValueTextWriter
    {
        private ValueTextWriter ParentWriter { get; set; }
        private bool            wasBuilt = false;

        public List<StructMemberParameter> Parameters { get; set; } = new();

        public string Name       { get; set; }
        public string ReturnType { get; set; }
        public string Body       { get; set; }
        public bool   IsDef      { get; set; }
        public bool   IsAsync    { get; set; }
        public string Comment    { get; set; }

        public bool IsConstructor  { get; set; }
        public bool IsMemberMethod { get; set; }

        public string Definition { get; set; }

        public MethodWriter(
            ValueTextWriter writer
        ) : base(writer) {
            ParentWriter = writer;
        }

        public MethodWriter SetName(string name) {
            Name = name;
            return this;
        }
        public MethodWriter SetReturnType(string returnType) {
            ReturnType = returnType;
            return this;
        }

        public MethodWriter AddParameters(
            IEnumerable<StructMemberParameter> parameters
        ) {
            Parameters.AddRange(parameters);
            return this;
        }
        public MethodWriter SetBody(string body) {
            Body = body;
            return this;
        }
        public MethodWriter SetComment(string comment) {
            Comment = comment;
            return this;
        }
        public MethodWriter SetDefinition(string definition) {
            Definition = definition;
            return this;
        }
        public MethodWriter SetIsDef(bool isDef) {
            IsDef = isDef;
            return this;
        }
        public MethodWriter SetIsAsync(bool isAsync) {
            IsAsync = isAsync;
            return this;
        }
        public MethodWriter SetIsConstructor(bool isConstructor) {
            IsConstructor = isConstructor;
            return this;
        }
        public MethodWriter SetIsMemberMethod(bool isMemberMethod) {
            IsMemberMethod = isMemberMethod;
            return this;
        }

        private void Build() {
            if (wasBuilt) return;
            wasBuilt = true;

            WriteLine();

            if (Comment.IsNotNullOrWhiteSpace()) {
                WriteDocumentation(Comment);
            }

            if (Definition.IsNotNullOrWhiteSpace()) {
                WriteLine(Definition);
                return;
            }

            if (IsDef) {
                Write("def ");
            }

            if (IsAsync) {
                Write("async ");
            }

            if (!IsMemberMethod) {
                Write("function ");
            }

            Write(Name);
            Write("(");
            Write(Parameters.Select(p => p.ToDefinitionString()).Join(", "));
            Write(")");

            if (ReturnType.IsNotNullOrWhiteSpace()) {
                Write(" " + ReturnType);
            }

            if (IsDef || Body.IsNullOrWhiteSpace()) {
                Write(";");
            } else {
                using var _ = Block();
                Body.Split('\n').ForEach(l => WriteLine(l));
            }

        }
        public override string ToString() {
            Build();
            return GetStringBuilder().ToString();
        }

        protected override void Dispose(bool disposing) {
            if (disposing && !wasBuilt) {
                Build();
            }
        }

    }

    public ValueTextWriter WriteMethod(
        Action<MethodWriter>         writeAction,
        Action<MethodWriter, string> onDispose = null
    ) {
        using var w = new MethodWriter(this);
        writeAction(w);

        Write(w.ToString());

        onDispose?.Invoke(w, w.ToString());

        return this;
    }
}