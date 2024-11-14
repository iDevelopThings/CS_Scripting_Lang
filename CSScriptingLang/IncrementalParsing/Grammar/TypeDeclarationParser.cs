using CSScriptingLang.Lexing;
using CSScriptingLang.IncrementalParsing.Syntax;

namespace CSScriptingLang.IncrementalParsing.Grammar;

public class TypeDeclarationParser
{
    public static CompleteMarker Parse(IncrementalParser p) {
        StatementParser.Attributes(p);

        var m = p.Marker();

        if (!p.Token.IsTypeKeyword)
            return m.Fail(SyntaxKind.TypeDeclaration, "Expected 'type' keyword");

        p.Advance();

        var typeNameIdent = p.Token.Value;

        ExpressionParser.Identifier(p);


        var kind = p.Token switch {
            {IsStructKeyword   : true} => SyntaxKind.StructDeclaration,
            {IsInterfaceKeyword: true} => SyntaxKind.InterfaceDeclaration,
            {IsEnumKeyword     : true} => SyntaxKind.EnumDeclaration,
            _                          => SyntaxKind.None,
        };
        if (kind == SyntaxKind.None)
            return m.Fail(SyntaxKind.TypeDeclaration, "Expected 'struct', 'interface' or 'enum' keyword got: " + p.Token.Value);

        if (m.EnsureAndConsume(Keyword.TypeDeclaration, "Expected 'struct', 'interface' or 'enum' keyword got: " + p.Token.Value, out var cm))
            return cm;
        if (m.EnsureAndConsume(TokenType.LBrace, "Expected opening brace for type declaration", out cm))
            return cm;

        while (p.Token is {IsRBrace: false, IsEOF: false}) {
            Member(p, kind, typeNameIdent);
        }

        if (m.EnsureAndConsume(TokenType.RBrace, "Expected closing brace for type declaration", out cm))
            return cm;

        return m.Complete(kind);
    }
    public static CompleteMarker Member(IncrementalParser p, SyntaxKind kind, string typeNameIdent) {
        var            m  = p.Marker();
        CompleteMarker cm = CompleteMarker.Empty;

        StatementParser.Attributes(p);

        if (kind == SyntaxKind.EnumDeclaration) {
            if (EnumMember(p, typeNameIdent, ref m, ref cm)) {
                return cm;
            }
        }

        var modifiers            = FunctionParser.FunctionModifiers(p, false, true);
        var hasFnModifierKeyword = modifiers.Any(mod => mod.Token.IsFunctionKeyword);
        if (kind == SyntaxKind.StructDeclaration && hasFnModifierKeyword) {
            return m.Fail(kind, "Incorrect method declaration syntax, usage is as follows: `type MyStruct struct { MyMethod(..) void; }`");
        }

        if (modifiers.Count > 0 || p.Token.IsIdentifier && p.Next.IsLParen) {
            FunctionParser.FunctionDeclarationWithKeywords(
                p,
                out var fnName,
                requiresFunctionKeyword: false,
                requiresBody: kind == SyntaxKind.StructDeclaration
                // applyModifiers: modifiers
            );

            p.AdvanceIfSemicolon();

            if (typeNameIdent == fnName) {
                return m.Complete(
                    kind == SyntaxKind.EnumDeclaration
                        ? SyntaxKind.EnumMemberConstructor
                        : SyntaxKind.StructDeclarationConstructor
                );
            }

            return m.Complete(
                kind == SyntaxKind.EnumDeclaration
                    ? SyntaxKind.EnumMemberDeclaration
                    : SyntaxKind.StructDeclarationMethod
            );
        }

        ExpressionParser.Identifier(p);
        ExpressionParser.TypeIdentifier(p);

        if (kind != SyntaxKind.StructDeclaration)
            return m.Fail(SyntaxKind.StructDeclarationField, $"Fields are only allowed in struct declarations; found in {kind}");

        return m.Complete(SyntaxKind.StructDeclarationField);
    }

    public static bool EnumMember(IncrementalParser p, string typeNameIdent, ref Marker m, ref CompleteMarker cm) {
        if (p.Token.IsIdentifier && p.Next.IsLParen && p.Token.Value == typeNameIdent) {
            cm = CompleteMarker.Empty;
            return false;
        }

        ExpressionParser.Identifier(p);

        // We can have the following formats:
        //  - `EnumMember`
        //  - `EnumMember = 10`
        //  - `EnumMember(value, value2)`

        // `EnumMember = 10`
        if (p.Token.IsAssignmentOperator) {
            p.Advance();
            ExpressionParser.Expression(p);

            cm = m.Complete(SyntaxKind.EnumMemberDeclaration_WithValue);
            return true;
        }

        // `EnumMember(value, value2)`
        if (p.Token.IsLParen) {
            ExpressionParser.CallArgumentList(p);

            cm = m.Complete(SyntaxKind.EnumMemberDeclaration_WithValueCtor);
            return true;
        }

        // `EnumMember`
        cm = m.Complete(SyntaxKind.EnumMemberDeclaration);
        return true;
    }
}