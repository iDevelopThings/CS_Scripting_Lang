using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace CSScriptingLangGenerators.Utils;

public class DocumentationData
{
    public string Summary;
    public string CodeExample;

    public DocumentationData(string summary, string codeExample) {
        Summary     = summary;
        CodeExample = codeExample;
    }
    public DocumentationData(string xml) {
        var element = XElement.Parse(xml, LoadOptions.None);
        Summary     = element.Element("summary")?.Value.Trim();
        CodeExample = element.Element("code")?.Value.Trim();
    }
    public bool IsValid() {
        return !string.IsNullOrWhiteSpace(Summary);
    }
    public override string ToString() {
        // if we only have `summary` and summary is a single line then return it as `// summary`
        if (string.IsNullOrWhiteSpace(CodeExample) && Summary.Contains('\n') == false) {
            return $"// {Summary}";
        }
        // if we have multiple summary lines, or code example, we'll output as:
        // ```
        // /**
        //  * summary line 1
        //  * summary line 2
        //  * ``` code ```
        //  */
        // ```

        var lines = Summary.Split('\n').Select(x => $" * {x.Trim()}");
        var codeExampleLines = string.IsNullOrWhiteSpace(CodeExample)
            ? ""
            : CodeExample.Split('\n')
               .Select(x => $" * {x.Trim()}")
               .Prepend(" * ```")
               .Append(" * ```")
               .Join("\n");

        var summary = string.Join("\n", lines);

        var result = $"/**\n{summary}\n{codeExampleLines}\n */";

        return result;
    }
}

public static partial class SymbolExtensions
{
    public static string GetDocumentation(this ISymbol symbol) {
        var doc = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(doc)) {
            return null;
        }

        return doc;
    }


    public static DocumentationData GetDocumentationData(this ISymbol symbol) {
        var doc = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(doc)) {
            return default;
        }
        return new DocumentationData(doc);
    }

}