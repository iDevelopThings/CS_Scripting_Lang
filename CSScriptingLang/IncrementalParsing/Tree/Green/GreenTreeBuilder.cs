using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;
using CSScriptingLang.Utils;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Diagnostic = CSScriptingLang.Core.Diagnostics.Diagnostic;
using UnreachableException = System.Diagnostics.UnreachableException;

namespace CSScriptingLang.IncrementalParsing.Tree.Green;

public class GreenTreeBuilder(IncrementalParser parser)
{
    public IncrementalParser Parser { get; set; } = parser;

    private GreenNodeBuilder NodeBuilder { get; } = new();

    private List<Diagnostic> Diagnostics { get; } = [];

    public (GreenNode, List<Diagnostic>, int) Build() {
        Parser.Parse();
        // Diagnostics.AddRange(Parser.Lexer.Diagnostics);

        using var _ = TimedScope.Scoped_Print("GreenTreeBuilder::Build");

        var (root, count) = BuildTree();
        return (root, Diagnostics, count);
    }


    private (GreenNode, int) BuildTree() {
        StartNode(SyntaxKind.Source);

        var parents = new List<SyntaxKind>();
        for (var i = 0; i < Parser.Events.Count; i++) {
            var markEvent = Parser.Events[i];
            switch (markEvent) {
                case MarkEvent.NodeStart(_, SyntaxKind.None, _):
                    break;
                case MarkEvent.NodeStart nodeStart: {
                    parents.Add(nodeStart.Kind);
                    var pPosition = nodeStart.Parent;
                    while (pPosition > 0) {
                        if (Parser.Events[pPosition] is MarkEvent.NodeStart pEvent) {
                            if (pEvent.Kind == SyntaxKind.None) {
                                break;
                            }

                            parents.Add(pEvent.Kind);
                            // // Do not reverse the order
                            Parser.Events[pPosition] = pEvent with {Kind = SyntaxKind.None, Parent = 0};
                            pPosition                = pEvent.Parent;
                        } else {
                            break;
                        }
                    }

                    // Traverse parents in reverse order
                    for (var j = parents.Count - 1; j >= 0; j--) {
                        var parent = parents[j];
                        StartNode(parent);
                    }

                    parents.Clear();
                    break;
                }
                case MarkEvent.EatToken token: {
                    EatToken(token);
                    break;
                }
                case MarkEvent.RemapTokenType remapToken: {
                    var lastPushed = NodeBuilder.Children.LastOrDefault();
                    if (lastPushed is not null) {

                        if (lastPushed.IsToken && lastPushed.TokenKind == remapToken.FromType) {
                            lastPushed.RawKind = (long) remapToken.ToType;
                        }

                        foreach (var child in lastPushed.Children) {
                            if (child.IsToken && child.TokenKind == remapToken.FromType) {
                                child.RawKind = (long) remapToken.ToType;
                            }
                        }

                    }
                    break;
                }
                case MarkEvent.Error error: {
                    var nextTokenIndex = -1;
                    for (var j = i; j >= 0; j--) {
                        if (Parser.Events[j] is MarkEvent.EatToken) {
                            nextTokenIndex = j;
                            break;
                        }
                    }

                    if (nextTokenIndex > 0) {
                        var range = Parser.Events[nextTokenIndex] switch {
                            MarkEvent.EatToken(var tkRange, var srcRange, _, _) => tkRange,

                            _ => throw new UnreachableException(),
                        };
                        Diagnostics.Add(
                            new Diagnostic(
                                DiagnosticSeverity.Error,
                                error.Err,
                                range,
                                error.Caller,
                                Core.Diagnostics.DiagnosticCode.SyntaxError
                            )
                        );
                    } else {
                        Diagnostics.Add(
                            new Diagnostic(
                                DiagnosticSeverity.Error,
                                error.Err,
                                new TokenRange() {
                                    Start       = Parser.Lexer.InputSource.Length,
                                    StartColumn = 0,
                                    StartLine   = Parser.Lexer.InputSource.Lines.Length,
                                    End         = Parser.Lexer.InputSource.Length,
                                    EndColumn   = 0,
                                    EndLine     = Parser.Lexer.InputSource.Lines.Length,
                                },
                                error.Caller,
                                Core.Diagnostics.DiagnosticCode.SyntaxError
                            )
                        );
                    }

                    break;
                }
                case MarkEvent.NodeEnd: {
                    FinishNode();
                    break;
                }
            }
        }

        FinishNode();
        return NodeBuilder.Finish();
    }


    private void StartNode(SyntaxKind kind) {
        NodeBuilder.StartNode(kind);
    }

    private void FinishNode() {
        NodeBuilder.FinishNode();
    }

    private void EatToken(MarkEvent.EatToken token) {
        NodeBuilder.EatToken(token.Kind, token.SourceRange);
    }
}