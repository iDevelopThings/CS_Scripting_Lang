using CSScriptingLang.Common.Extensions;
using Force.DeepCloner;

namespace CSScriptingLang.Lexing;

public partial struct Lexer
{
    
    private class Macro
    {
        public string   Name       { get; set; }
        public string[] Parameters { get; set; }
        public string   Body       { get; set; }
        public Token[]  BodyTokens { get; set; }

        public Macro(string name, string[] parameters, string body, Token[] bodyTokens) {
            Name       = name;
            Parameters = parameters;
            Body       = body;
            BodyTokens = bodyTokens;
        }
    }

    
    private string Substring(int start, int end) {
        if (start == end) {
            return "";
        }

        return InputSource.Substring(start, end - start);
    }
    public void PushMarker() {
        PositionMarkerStack.Push(Position.From(Position));
        // PositionMarkerStack.Push(Position.Rent().From(Position));
    }
    public void MarkStart() {
        Position.Start = Position.Current;
        PushMarker();
    }
    public void MarkEnd() {
        Position.End = Position.Current;
    }
    public TokenRange GetMarkerRange() {
        var range = new TokenRange {
            Start       = Position.Start,
            StartColumn = Position.Column,
            StartLine   = Position.Line,

            End       = Position.End,
            EndColumn = Position.Column,
            EndLine   = Position.Line,
        };

        if (PositionMarkerStack.Count != 0) {
            var marker = PositionMarkerStack.Pop();
            range.Start       = marker.Start;
            range.StartColumn = marker.Column;
            range.StartLine   = marker.Line;
            // marker.Return();
        }

        return range;
    }
    
    private void MacroExpansion() {
        var macros = new Dictionary<string, Macro>();

        {
            using var tokenEnumerator = Tokenize().GetEnumerator();

            var tempTokens = new List<Token>();
            while (tokenEnumerator.MoveNext()) {
                var token = tokenEnumerator.Current!;

                if (token.IsMacroKeyword) {
                    var macroName = tokenEnumerator.MoveNext() ? tokenEnumerator.Current!.Value : "";

                    var parameters = new List<string>();
                    while (tokenEnumerator.MoveNext()) {
                        token = tokenEnumerator.Current!;
                        if (token.Type == TokenType.LParen) {
                            while (tokenEnumerator.MoveNext()) {
                                token = tokenEnumerator.Current!;
                                if (token.Type == TokenType.RParen) {
                                    break;
                                }

                                if (token.Type == TokenType.Identifier) {
                                    parameters.Add(token.Value);
                                }
                            }
                        }

                        if (token.Type == TokenType.LBrace) {
                            var body       = "";
                            var bodyTokens = new List<Token>();
                            while (tokenEnumerator.MoveNext()) {
                                token = tokenEnumerator.Current!;
                                if (token.Type == TokenType.RBrace) {
                                    break;
                                }
                                bodyTokens.Add(token);
                                body += token.Value;
                            }

                            macros.Add(macroName, new Macro(macroName, parameters.ToArray(), body, bodyTokens.ToArray()));
                            break;
                        }
                    }
                } else {
                    tempTokens.Add(token);
                }
            }

            Tokens = tempTokens;
        }


        {
            using var tokenEnumerator = Tokens.GetEnumerator();

            // Now we need to perform macro expansion
            var expandedTokens = new List<Token>();
            while (tokenEnumerator.MoveNext()) {
                var token = tokenEnumerator.Current!;
                Token GetNextToken() {
                    return tokenEnumerator.MoveNext() ? tokenEnumerator.Current! : null;
                }

                if (token.Type != TokenType.Identifier) {
                    expandedTokens.Add(token);
                    continue;
                }

                if (!macros.TryGetValue(token.Value, out var macro)) {
                    expandedTokens.Add(token);
                    continue;
                }
                if (token.Previous?.IsMacroKeyword == true) {
                    expandedTokens.Add(token);
                    continue;
                }

                var parameters = new List<Token>();
                {
                    var tok = GetNextToken();
                    tok = GetNextToken();

                    while (tok != null) {
                        parameters.Add(tok);
                        tok = GetNextToken();
                        if (tok?.Type == TokenType.Comma) {
                            tok = GetNextToken();
                        } else if (tok?.Type == TokenType.RParen) {
                            tok = GetNextToken();
                            break;
                        }
                    }
                }


                var macroTokens = new List<Token>();
                macroTokens.AddRange(macro.BodyTokens.Select(t => t.DeepClone()));

                void substituteParam(int paramIdx, string param) {
                    foreach (var t in macroTokens) {
                        if (t.Type == TokenType.Identifier && t.Value == param) {
                            t.Value = parameters[paramIdx].Value;
                        }
                    }
                }
                for (var paramIdx = 0; paramIdx < macro.Parameters.Length; paramIdx++) {
                    substituteParam(paramIdx, macro.Parameters[paramIdx]);
                }

                // handle concatenation
                for (var i = 0; i < macroTokens.Count; i++) {
                    var t = macroTokens[i];
                    if (t.Type != TokenType.HashHash) {
                        continue;
                    }

                    var prev = macroTokens[i - 1];
                    var next = macroTokens[i + 1];

                    var newToken = new Token(prev.Type, prev.Value + next.Value, prev.Range);
                    macroTokens[i - 1] = newToken;
                    macroTokens.RemoveAt(i);
                    macroTokens.RemoveAt(i);
                    i--;
                }

                expandedTokens.AddRange(macroTokens);

            }

            Tokens = expandedTokens;

            Logger.Debug("Lexed all tokens");
        }
    }
    private Token MacroIdentifierState() => IdentifierStateImpl(true);
}