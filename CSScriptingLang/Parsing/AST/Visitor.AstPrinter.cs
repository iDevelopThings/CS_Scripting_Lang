using CSScriptingLang.Lexing;
using CSScriptingLang.Common.CodeWriter;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;

namespace CSScriptingLang.Parsing.AST;

public partial class ASTPrintingVisitor : BaseAstVisitor
{
    public Writer w { get; set; }

    public ASTPrintingVisitor() {
        w = new Writer(new CodeWriterSettings(CodeWriterSettings.CSharpDefault) {
            NewLineBeforeBlockBegin = true,
            Indent                  = "    ",
            TranslationMapping = {
                ["`"] = "\""
            }
        });
    }

    public override void VisitProgramExpression(ProgramExpression node) {
        w._($"ProgramExpression");
        base.VisitProgramExpression(node);

        Console.WriteLine(w.ToString());
    }
    public override void VisitStringExpression(StringExpression node) {
        w.WriteInline($"String({node.NativeValue})");
        base.VisitStringExpression(node);
    }
    public override void VisitExpressionListNode(ExpressionListNode node) {
        w.WriteInline($"(");
        base.VisitExpressionListNode(node);
        w.WriteInline($")");
    }
    
    public override void VisitBinaryOpExpression(BinaryOpExpression node) {
        if (!_visitedNodes.Add(node))
            return;

        node.Left?.Accept(this);
        w.WriteInline($" {node.Operator.ToSymbol()} ");
        node.Right?.Accept(this);
    }
    public override void VisitUnaryOpExpression(UnaryOpExpression node) {
        w._($"UnaryOpExpression");
        base.VisitUnaryOpExpression(node);

    }
    public override void VisitObjectLiteralExpression(ObjectLiteralExpression node) {
        using var _ = w.b($"ObjectLiteralExpression");
        base.VisitObjectLiteralExpression(node);
    }
    public override void VisitObjectProperty(ObjectProperty node) {
        w.WriteInlineIndented($"{node.Name}: ");
        base.VisitObjectProperty(node);

    }
    public override void VisitMemberAccessExpression(MemberAccessExpression node) {
        base.VisitMemberAccessExpression(node);

        w.WriteInline($".{node.Identifier}");
    }
    public override void VisitIndexAccessExpression(IndexAccessExpression node) {
        w._($"IndexAccessExpression");
        base.VisitIndexAccessExpression(node);

    }
    public override void VisitBlockExpression(BlockExpression node) {
        using var _ = w.b($"");
        base.VisitBlockExpression(node);
    }

    public override void VisitCallExpression(CallExpression node) {
        if (node.Variable != null) {
            node.Variable.Accept(this);
            if(node.Name != null) {
                w.WriteInline($".{node.Name}");
            }
        } else {
            w.WriteInline($"{node.Name ?? node.Identifier ?? "UNKNOWN FUNCTION NAME??"}");
        }
        
        node.Arguments.Accept(this);

        // base.VisitCallExpression(node);
    }
    public override void VisitIfStatementNode(IfStatementNode node) {
        w._($"IfStatementNode");
        base.VisitIfStatementNode(node);

    }
    public override void VisitForLoopStatement(ForLoopStatement node) {
        w._($"ForLoopStatement");
        base.VisitForLoopStatement(node);

    }
    public override void VisitReturnStatement(ReturnStatement node) {
        if (!_visitedNodes.Add(node))
            return;

        if (node.ReturnValue != null) {
            w.WriteInlineIndented($"return ");
            node.ReturnValue.Accept(this);
            w.WriteInline($";\n");
        } else
            w._($"return;");
    }

    public override void VisitVariableDeclarationNode(VariableDeclarationNode node) {
        w.WriteInline("\n");
        base.VisitVariableDeclarationNode(node);
        w.WriteInline($";\n");
    }
    public override void VisitArgumentDeclarationNode(ArgumentDeclarationNode node) {
        if (!_visitedNodes.Add(node))
            return;
        w.WriteInline($"{node.Type} {node.Name}");
    }
    public override void VisitArgumentListDeclarationNode(ArgumentListDeclarationNode node) {
        if (!_visitedNodes.Add(node))
            return;
        w.WriteInline($"({string.Join(", ", node.Arguments.Select(x => $"{x.Type} {x.Name}"))})");
    }
    public override void VisitInlineFunctionDeclaration(InlineFunctionDeclaration node) {
        if (!_visitedNodes.Add(node))
            return;

        w.WriteInline($"function");
        node.Parameters?.Accept(this);

        // node.Body?.Accept(this);
        // base.VisitInlineFunctionDeclaration(node);

    }

    public override void VisitBaseNode(BaseNode node) {
        w._($"BaseNode");
        base.VisitBaseNode(node);
    }
    public override void VisitNodeList<T>(NodeList<T> node) {
        w._($"NodeList");
        base.VisitNodeList(node);

    }
}