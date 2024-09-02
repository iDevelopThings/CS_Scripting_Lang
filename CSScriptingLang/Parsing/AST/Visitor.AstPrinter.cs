using CSScriptingLang.Lexing;
using CSScriptingLang.Utils.CodeWriter;

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

    public override void VisitProgramNode(ProgramNode node) {
        w._($"ProgramNode");
        base.VisitProgramNode(node);

        Console.WriteLine(w.ToString());
    }
    public override void VisitFunctionDeclarationNode(FunctionDeclarationNode node) {
        base.VisitFunctionDeclarationNode(node);
    }
    public override void VisitNumberNode(NumberNode node) {
        w.WriteInline($"Number({node.Value})");

        base.VisitNumberNode(node);
    }
    public override void VisitStringNode(StringNode node) {
        w.WriteInline($"String({node.Value})");
        base.VisitStringNode(node);
    }
    public override void VisitExpressionListNode(ExpressionListNode node) {
        w.WriteInline($"(");
        base.VisitExpressionListNode(node);
        w.WriteInline($")");
    }
    public override void VisitVariableNode(VariableNode node) {
        w.WriteInline($"Variable({node.Name})");
        base.VisitVariableNode(node);
    }
    public override void VisitBinaryOperationNode(BinaryOperationNode node) {
        if (!_visitedNodes.Add(node))
            return;

        node.Left?.Accept(this);
        w.WriteInline($" {node.Operator.ToSymbol()} ");
        node.Right?.Accept(this);
    }
    public override void VisitUnaryOperationNode(UnaryOperationNode node) {
        w._($"UnaryOperationNode");
        base.VisitUnaryOperationNode(node);

    }
    public override void VisitObjectLiteralNode(ObjectLiteralNode node) {
        using var _ = w.b($"ObjectLiteralNode");
        base.VisitObjectLiteralNode(node);
    }
    public override void VisitObjectProperty(ObjectProperty node) {
        w.WriteInlineIndented($"{node.Name}: ");
        base.VisitObjectProperty(node);

    }
    public override void VisitPropertyAccessNode(PropertyAccessNode node) {
        base.VisitPropertyAccessNode(node);

        w.WriteInline($".{node.Name}");
    }
    public override void VisitIndexAccessNode(IndexAccessNode node) {
        w._($"IndexAccessNode");
        base.VisitIndexAccessNode(node);

    }
    public override void VisitBlockNode(BlockNode node) {
        using var _ = w.b($"");
        base.VisitBlockNode(node);
    }
    public override void VisitAssignmentNode(AssignmentNode node) {
        w.WriteInline($" = ");
        base.VisitAssignmentNode(node);
    }
    public override void VisitFunctionCallNode(FunctionCallNode node) {
        if (node.Variable != null) {
            node.Variable.Accept(this);
            if(node.Name != null) {
                w.WriteInline($".{node.Name}");
            }
        } else {
            w.WriteInline($"{node.Name ?? node.VariableName ?? "UNKNOWN FUNCTION NAME??"}");
        }
        
        node.Arguments.Accept(this);

        // base.VisitFunctionCallNode(node);
    }
    public override void VisitIfStatementNode(IfStatementNode node) {
        w._($"IfStatementNode");
        base.VisitIfStatementNode(node);

    }
    public override void VisitForLoopNode(ForLoopNode node) {
        w._($"ForLoopNode");
        base.VisitForLoopNode(node);

    }
    public override void VisitReturnStatementNode(ReturnStatementNode node) {
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
        w.WriteInline($"var {node.VariableName}");
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
    public override void VisitInlineFunctionDeclarationNode(InlineFunctionDeclarationNode node) {
        if (!_visitedNodes.Add(node))
            return;

        w.WriteInline($"function");
        node.Parameters?.Accept(this);

        node.Body?.Accept(this);
        // base.VisitInlineFunctionDeclarationNode(node);

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