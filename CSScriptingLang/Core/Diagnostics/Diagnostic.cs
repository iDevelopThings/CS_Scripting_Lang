using CSScriptingLang.Parsing.AST.NamedSymbol;
using CSScriptingLang.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace CSScriptingLang.Core.Diagnostics;

public class Diagnostic
{
    private NamedSymbolRange _range;
    private DocumentUri      _file;
    private int              _fileVersion = -1;

    public DiagnosticSeverity Severity { get; }
    public DiagnosticCode     Code     { get; }
    public string             Message  { get; }

    public bool IsFatal { get; set; }

    public NamedSymbolRange Range {
        get => _range;
        set {
            _range       = value;
            File         = value?.Script?.Uri;
            _fileVersion = value?.Script?.File?.Version ?? -1;
        }
    }

    public DocumentUri File {
        get => Range?.Script?.Uri ?? _file;
        set => _file = value;
    }

    public int FileVersion => _fileVersion;

    public Caller SourceCallerInfo { get; set; }

    public Diagnostic(
        DiagnosticSeverity severity,
        string             message,
        NamedSymbolRange   range
    ) {
        Severity = severity;
        Message  = message;
        Range    = range;
    }

    public Diagnostic(
        DiagnosticSeverity severity,
        string             message,
        NamedSymbolRange   range,
        DiagnosticCode     code
    ) : this(severity, message, range) {
        Code = code;
    }

    public Diagnostic(
        DiagnosticSeverity severity,
        string             message,
        NamedSymbolRange   range,
        Caller             sourceCallerInfo,
        DiagnosticCode     code = DiagnosticCode.None
    ) : this(severity, message, range, code) {
        SourceCallerInfo = sourceCallerInfo;
    }

    public static implicit operator LSPDiagnostic(Diagnostic diagnostic) {
        return new LSPDiagnostic {
            Message  = diagnostic.Message,
            Range    = diagnostic.Range,
            Severity = diagnostic.Severity,
            Source   = diagnostic.SourceCallerInfo.ToString(),
        };
    }

    public override string ToString() {
        var str = $"{Severity.ToString().ToUpper()}: {Message}";

        if (File != null) {

            str += $" at {File}";

            if (Range != null) {
                str += $":{Range.Start.Line}:{Range.Start.Character}";
            }
        }

        str += $" (version {FileVersion})";

        if (SourceCallerInfo.IsValid()) {
            str += $"\nfrom {SourceCallerInfo}";
        }

        return str;
    }
}