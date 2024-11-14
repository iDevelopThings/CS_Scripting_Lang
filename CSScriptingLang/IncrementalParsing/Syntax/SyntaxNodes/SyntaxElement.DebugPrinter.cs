using System.Reflection;
using System.Text;
using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;
using CSScriptingLang.Utils.ReflectionUtils;

namespace CSScriptingLang.IncrementalParsing.Syntax;

public abstract partial class SyntaxElement
{
    public virtual string GetDebugName() {
        var t = GetType().Name;
        if (this is PlaceholderSyntaxNode ph) {
            t = $"Placeholder({ph.Kind.ToString()})";
        }
        return t;
    }
    
    public string DebugSyntaxInspect(
        bool includeContent          = true,
        bool includeWhitespaceTokens = true,
        bool thisNodeOnly            = false
    ) {
        var sb    = new StringBuilder();
        var stack = new Stack<(SyntaxElement node, int level)>();

        stack.Push((this, 0));
        while (stack.Count > 0) {
            var (nodeOrToken, level) = stack.Pop();
            sb.Append(' ', level * 2);
            switch (nodeOrToken) {

                case SyntaxNode node: {
                    var t = node.GetDebugName();
                    sb.Append($"{t}({node.SourceRange}) ");
                    if (includeContent) {
                        try {
                            sb.Append(node.DebugContent());
                        }
                        catch (Exception e) {
                            sb.Append("ERROR(" + e.Message + ")");
                        }
                    }
                    sb.AppendLine();

                    if (thisNodeOnly)
                        break;

                    foreach (var child in node.ChildrenWithTokens.Reverse()) {
                        stack.Push((child, level + 1));
                    }

                    break;
                }
                case SyntaxToken token: {
                    if (token is WhitespaceToken or NewLineToken && !includeWhitespaceTokens) {
                        continue;
                    }
                    var detail = "";
                    try {
                        detail = token switch {
                            {
                                Kind: TokenType.Whitespace or TokenType.NewLine or TokenType.LineComment or TokenType.BlockComment
                            } => "",
                            StringToken stringToken => $"\"{stringToken.DebugContent()}\"",
                            Int32Token integerToken => $"{integerToken.DebugContent()}",
                            Int64Token integerToken => $"{integerToken.DebugContent()}",
                            FloatToken floatToken   => $"{floatToken.DebugContent()}",
                            DoubleToken doubleToken => $"{doubleToken.DebugContent()}",
                            NameToken nameToken     => $"{nameToken.RepresentText}",
                            _                       => $"\"{token.Text.ToString().Replace("\r\n", "\n").Replace("\n", "\\n")}\"",
                        };
                    }
                    catch (Exception e) {
                        detail = "ERROR(" + e.Message + ")";
                    }

                    sb.AppendLine(
                        $"{token.GetType().Name}({token.Kind}, {token.SourceRange}) {detail}"
                    );

                    break;
                }
            }
        }

        return sb.ToString();
    }
    public string DebugCompactInspect(
        bool includeContent          = true,
        bool includeWhitespaceTokens = true
    ) {
        var sb    = new StringBuilder();
        var stack = new Stack<(SyntaxElement node, int level)>();
        var processed = new HashSet<int>();

        var syntaxElementType = typeof(SyntaxElement);
        var syntaxNodeType    = typeof(SyntaxNode);
        var syntaxTokenType   = typeof(SyntaxToken);

        // syntaxElementType.GetMembers()
        IEnumerable<ObjectMember> GetNodeMembers(SyntaxElement element) {
            var type = element.GetType();

            var fields = new List<ObjectMember>();

            fields.AddRange(
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                   .Select(property => new ObjectMember(property, element))
            );
            fields.AddRange(
                type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                   .Select(field => new ObjectMember(field, element))
            );


            foreach (var member in fields) {
                if (member.DeclaringType == syntaxElementType) continue;
                if (member.DeclaringType == syntaxNodeType) continue;
                if (member.DeclaringType == syntaxTokenType) continue;

                // We allow IEnumerable<T>, Children of SyntaxElement and SyntaxNode

                if (
                    member.IsGenericType &&
                    member.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                    member.Type.GetGenericArguments().Any(a => a.IsAssignableTo(syntaxElementType))
                ) {
                    yield return member;
                    continue;
                }
                if (member.Type.IsAssignableTo(syntaxElementType)) {
                    yield return member;
                    continue;
                }

                // Console.WriteLine(member.Name);
            }
        }

        var ident = 4;

        void WriteNode(SyntaxElement nodeOrToken, int level) {
            switch (nodeOrToken) {

                case SyntaxNode node: {
                    var t = node.GetDebugName();
                    sb.Append($"{t}({node.SourceRange}) ");
                    if (includeContent) {
                        try
                        {
                            sb.Append(node.DebugContent());
                        }
                        catch (Exception e)
                        {
                            sb.Append("ERROR(" + e.Message + ")");
                        }
                    }
                    sb.AppendLine();

                    var members = GetNodeMembers(nodeOrToken)
                       .OrderByDescending(
                            member => {
                                var value = member.GetValue();
                                if (value is IEnumerable<SyntaxElement> elements) {
                                    var el = elements.OrderBy(e => e.ElementId).FirstOrDefault();
                                    return el?.ElementId ?? 0;
                                }
                                if (value is SyntaxElement element) {
                                    return element.ElementId;
                                }
                                return 0;
                            }
                        )
                       .ToList();

                    foreach (var member in members) {
                        // sb.Append(' ', level * ident);
                        // sb.Append("-> ");
                        // sb.Append(member.Name);
                        // sb.AppendLine();

                        var value = member.GetValue();
                        if (value is IEnumerable<SyntaxElement> elements) {
                            foreach (var element in elements.OrderByDescending(e => e.ElementId)) {
                                stack.Push((element, level + 1));
                            }
                        } else if (value is SyntaxElement element) {
                            stack.Push((element, level + 1));
                        } else if (value is null) {
                            sb.Append(' ', (level + 1) * ident);
                            sb.AppendLine($"{member.Name} -> null");
                        } else {
                            throw new Exception("Unexpected value");
                        }
                    }

                    break;
                }

                case SyntaxToken token: {
                    if (token is WhitespaceToken or NewLineToken && !includeWhitespaceTokens) {
                        return;
                    }
                    var detail = "";
                    try {
                        detail = token switch {
                            {
                                Kind: TokenType.Whitespace or TokenType.NewLine or TokenType.LineComment or TokenType.BlockComment
                            } => "",
                            StringToken stringToken => $"\"{stringToken.DebugContent()}\"",
                            Int32Token integerToken => $"{integerToken.DebugContent()}",
                            Int64Token integerToken => $"{integerToken.DebugContent()}",
                            FloatToken floatToken   => $"{floatToken.DebugContent()}",
                            DoubleToken doubleToken => $"{doubleToken.DebugContent()}",
                            NameToken nameToken     => $"{nameToken.RepresentText}",
                            _                       => $"\"{token.Text.ToString().Replace("\r\n", "\n").Replace("\n", "\\n")}\"",
                        };
                    }
                    catch (Exception e) {
                        detail = "ERROR(" + e.Message + ")";
                    }

                    sb.AppendLine(
                        $"{token.GetType().Name}({token.Kind}, {token.SourceRange}) {detail}"
                    );
                    break;
                }

            }
        }

        stack.Push((this, 0));
        while (stack.Count > 0) {
            if(processed.Contains(stack.Peek().node.ElementId)) {
                stack.Pop();
                continue;
            }
            processed.Add(stack.Peek().node.ElementId);
            
            var (nodeOrToken, level) = stack.Pop();
            sb.Append(' ', level * ident);
            sb.Append($"[{nodeOrToken.ElementId}] ");

            WriteNode(nodeOrToken, level);

        }

        return sb.ToString();
    }
    public string DebugGreenInspect() {
        var sb    = new StringBuilder();
        var stack = new Stack<(SyntaxElement node, int level)>();

        stack.Push((this, 0));
        while (stack.Count > 0) {
            var (luaSyntaxElement, level) = stack.Pop();
            sb.Append(' ', level * 2);
            switch (luaSyntaxElement) {
                case SyntaxNode node: {
                    sb.AppendLine(
                        $"{node.Kind}@{node.SourceRange}"
                    );
                    foreach (var child in node.ChildrenWithTokens.Reverse()) {
                        stack.Push((child, level + 1));
                    }

                    break;
                }
                case SyntaxToken token: {
                    var detail = token.Kind switch {
                        TokenType.Whitespace or TokenType.NewLine or TokenType.LineComment or TokenType.BlockComment => "",

                        _ => $"\"{token.Text}\""
                    };

                    sb.AppendLine(
                        $"{token.Kind}@ {token.SourceRange} {detail}"
                    );
                    break;
                }
            }
        }

        return sb.ToString();
    }
}