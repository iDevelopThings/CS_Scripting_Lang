namespace CSScriptingLang.Parsing.AST;

public partial class ASTParentLinker : BaseAstVisitor
{
    public ASTParentLinker() { }

    private void SetParent(BaseNode parent, BaseNode child) {
        if (child == null)
            return;

        if (child.Parent != null && child.Parent != parent) {
            throw new Exception($"Parent already set for {child.GetType().Name}");
        }

        child.Parent = parent;
    }

    private void SetParents(BaseNode parent) {
        foreach (var child in parent.AllNodes()) {
            SetParent(parent, child);
        }
    }

    public override void VisitNodeList<T>(NodeList<T> node) {
        base.VisitNodeList(node);
    }
    public override void VisitVariableDeclarationNode(VariableDeclarationNode node) {
        SetParents(node);
        base.VisitVariableDeclarationNode(node);
    }
    public override void VisitArgumentDeclarationNode(ArgumentDeclarationNode node) {
        SetParents(node);
        base.VisitArgumentDeclarationNode(node);
    }
    public override void VisitArgumentListDeclarationNode(ArgumentListDeclarationNode node) {
        SetParents(node);
        base.VisitArgumentListDeclarationNode(node);
    }
    public override void VisitInlineFunctionDeclarationNode(InlineFunctionDeclarationNode node) {
        SetParents(node);
        base.VisitInlineFunctionDeclarationNode(node);
    }
    public override void VisitFunctionDeclarationNode(FunctionDeclarationNode node) {
        SetParents(node);
        base.VisitFunctionDeclarationNode(node);
    }
    public override void VisitNumberNode(NumberNode node) {
        SetParents(node);
        base.VisitNumberNode(node);
    }
    public override void VisitStringNode(StringNode node) {
        SetParents(node);
        base.VisitStringNode(node);
    }
    public override void VisitExpressionListNode(ExpressionListNode node) {
        SetParents(node);
        base.VisitExpressionListNode(node);
    }
    public override void VisitVariableNode(VariableNode node) {
        SetParents(node);
        base.VisitVariableNode(node);
    }
    public override void VisitBinaryOperationNode(BinaryOperationNode node) {
        SetParents(node);
        base.VisitBinaryOperationNode(node);
    }
    public override void VisitUnaryOperationNode(UnaryOperationNode node) {
        SetParents(node);
        base.VisitUnaryOperationNode(node);
    }
    public override void VisitObjectProperty(ObjectProperty node) {
        SetParents(node);
        base.VisitObjectProperty(node);
    }
    public override void VisitObjectLiteralNode(ObjectLiteralNode node) {
        SetParents(node);
        base.VisitObjectLiteralNode(node);
    }
    public override void VisitPropertyAccessNode(PropertyAccessNode node) {
        SetParents(node);
        base.VisitPropertyAccessNode(node);
    }
    public override void VisitIndexAccessNode(IndexAccessNode node) {
        SetParents(node);
        base.VisitIndexAccessNode(node);
    }
    public override void VisitProgramNode(ProgramNode node) {
        SetParents(node);
        base.VisitProgramNode(node);
    }
    public override void VisitBlockNode(BlockNode node) {
        SetParents(node);
        base.VisitBlockNode(node);
    }
    public override void VisitAssignmentNode(AssignmentNode node) {
        SetParents(node);
        base.VisitAssignmentNode(node);
    }
    public override void VisitFunctionCallNode(FunctionCallNode node) {
        SetParents(node);
        base.VisitFunctionCallNode(node);
    }
    public override void VisitIfStatementNode(IfStatementNode node) {
        SetParents(node);
        base.VisitIfStatementNode(node);
    }
    public override void VisitForLoopNode(ForLoopNode node) {
        SetParents(node);
        base.VisitForLoopNode(node);
    }
    public override void VisitReturnStatementNode(ReturnStatementNode node) {
        SetParents(node);
        base.VisitReturnStatementNode(node);
    }
}