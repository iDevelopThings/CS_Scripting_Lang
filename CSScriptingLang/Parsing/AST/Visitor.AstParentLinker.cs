using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Interpreter.Modules;

namespace CSScriptingLang.Parsing.AST;

public class ASTParentLinker : BaseAstVisitor
{
    public Script Script { get; set; }

    protected HashSet<BaseNode> linkedNodes = new();

    public enum LinkPhase
    {
        PrevNextParentChild,
        Instructions
    }

    public LinkPhase Phase { get; set; } = LinkPhase.PrevNextParentChild;

    public Stack<BlockExpression>                  BlockStack           { get; } = new();
    public HashSet<ITopLevelDeclarationNode> TopLevelDeclarations { get; } = new();
    public List<BaseNode>                    InstructionList      { get; } = new();

    public ASTParentLinker(Script script) {
        Script = script;
    }

    private void SetParents(IEnumerable<BaseNode> nodes, BaseNode parent) {
        if (nodes == null || parent == null)
            return;

        foreach (var node in nodes) {
            SetParent(parent, node);
        }
    }
    private void SetParents(BaseNode parent) {
        if (parent is BlockExpression b) {
            LinkPrevNext(b.Nodes);
        } else if (parent is INodeList list) {
            LinkPrevNext(list.NodesAsBaseNode);
        } else if (parent is VariableDeclarationNode vdn && vdn.Initializers != null) {
            LinkPrevNext(vdn.Initializers);
        }

        foreach (var child in parent.AllNodes()) {
            SetParent(parent, child);

            if (child is BlockExpression block) {
                LinkPrevNext(block.Nodes);
            } else if (child is INodeList list) {
                LinkPrevNext(list.NodesAsBaseNode);
            }
        }
    }
    private void SetParent(BaseNode parent, BaseNode child) {
        if (child == null)
            return;

        if (child.Parent != null && child.Parent != parent) {
            // throw new Exception($"Parent already set for {child.GetType().Name}");
            // child.Parent = parent;
        }

        child.Parent = parent;
    }

    private void LinkPrevNext(IEnumerable<BaseNode> nodes) {
        BaseNode prev = null;
        foreach (var node in nodes) {
            if (prev != null) {
                prev.Next     = node;
                node.Previous = prev;
            }

            prev = node;
        }
    }

    public override void OnVisitAny(BaseNode node) {
        base.OnVisitAny(node);
        
        node.ScriptId = Script?.Id ?? 0;

        if (Phase == LinkPhase.PrevNextParentChild) {
            if (linkedNodes.Add(node)) {
                if (node is VariableDeclarationNode vdn) {
                    SetParents(vdn.Initializers, vdn);
                }

                SetParents(node);
            }
        }

        if (Phase == LinkPhase.Instructions) {
            if (linkedNodes.Add(node)) {
                void TryAddTopLevelDecl(BaseNode n) {
                    if (n is ITopLevelDeclarationNode tld) {
                        if (BlockStack.Count == 0)
                            TopLevelDeclarations.Add(tld);
                    }
                }

                TryAddTopLevelDecl(node);

                if (node is Statement && BlockStack.Count == 0) {
                    InstructionList.Add(node);
                }

            }
        }

    }

    public void ProcessNodes(ProgramExpression node) {
        Phase = LinkPhase.PrevNextParentChild;
        _visitedNodes.Clear();
        linkedNodes.Clear();
        BlockStack.Clear();
        TopLevelDeclarations.Clear();
        InstructionList.Clear();

        VisitProgramExpression(node);
    }
    
    public void ProcessModuleNodes(ProgramExpression node) {
        Phase = LinkPhase.PrevNextParentChild;
        _visitedNodes.Clear();
        linkedNodes.Clear();
        BlockStack.Clear();
        TopLevelDeclarations.Clear();
        InstructionList.Clear();

        if (node.Nodes.FirstOrDefault(n => n is InlineFunctionDeclaration) is not InlineFunctionDeclaration moduleDecl) {
            throw new Exception("Module declaration not found");
        }
        
        moduleDecl.Accept(this);
        
        // foreach (var stmt in moduleDecl.Statements) {
        //     stmt.Accept(this);
        // }
    }

    public override void VisitProgramExpression(ProgramExpression node) {

        base.VisitProgramExpression(node);

        if (Phase == LinkPhase.PrevNextParentChild) {
            Phase = LinkPhase.Instructions;
            _visitedNodes.Clear();
            linkedNodes.Clear();
            VisitProgramExpression(node);
        } else if (Phase == LinkPhase.Instructions) {

            /*var declsFlattened = TopLevelDeclarations
               .SelectMany(d => {
                    var statements = (d as BaseNode)?.Cursor.All.Children<IStatementNode>();

                    return statements;
                })
               .ToList();
            var instrs = InstructionList;
            */


        }
    }

    public override void VisitBlockExpression(BlockExpression node) {
        BlockStack.Push(node);

        base.VisitBlockExpression(node);

        BlockStack.Pop();
    }
    /*
    public override void VisitVariableDeclarationNode(VariableDeclarationNode node) {
        ProcessNode(node);
        base.VisitVariableDeclarationNode(node);
    }
    public override void VisitArgumentDeclarationNode(ArgumentDeclarationNode node) {
        ProcessNode(node);
        base.VisitArgumentDeclarationNode(node);
    }
    public override void VisitTupleListDeclarationNode(TupleListDeclarationNode node) {
        ProcessNode(node);
        base.VisitTupleListDeclarationNode(node);
    }
    public override void VisitArgumentListDeclarationNode(ArgumentListDeclarationNode node) {
        ProcessNode(node);
        base.VisitArgumentListDeclarationNode(node);
    }
    public override void VisitInlineFunctionDeclarationNode(InlineFunctionDeclaration node) {
        ProcessNode(node);
        base.VisitInlineFunctionDeclarationNode(node);
    }
    public override void VisitFunctionDeclarationNode(FunctionDeclaration node) {
        ProcessNode(node);
        base.VisitFunctionDeclarationNode(node);
    }
    public override void VisitNumberNode(NumberNode node) {
        ProcessNode(node);
        base.VisitNumberNode(node);
    }
    public override void VisitStringNode(StringNode node) {
        ProcessNode(node);
        base.VisitStringNode(node);
    }
    public override void VisitExpressionListNode(ExpressionListNode node) {
        ProcessNode(node);
        LinkPrevNext(node.ExpressionNodes);
        base.VisitExpressionListNode(node);
    }
    public override void VisitVariableNode(VariableNode node) {
        ProcessNode(node);
        base.VisitVariableNode(node);
    }
    public override void VisitBinaryOperationNode(BinaryOpExpression node) {
        ProcessNode(node);
        base.VisitBinaryOperationNode(node);
    }
    public override void VisitUnaryOperationNode(UnaryOpExpression node) {
        ProcessNode(node);
        base.VisitUnaryOperationNode(node);
    }
    public override void VisitObjectProperty(ObjectProperty node) {
        ProcessNode(node);
        base.VisitObjectProperty(node);
    }
    public override void VisitObjectLiteralNode(ObjectLiteralNode node) {
        ProcessNode(node);
        base.VisitObjectLiteralNode(node);
    }
    public override void VisitPropertyAccessNode(MemberAccessExpression node) {
        ProcessNode(node);
        base.VisitPropertyAccessNode(node);
    }
    public override void VisitIndexAccessNode(IndexAccessNode node) {
        ProcessNode(node);
        base.VisitIndexAccessNode(node);
    }
    public override void VisitAssignmentNode(AssignmentNode node) {
        ProcessNode(node);
        base.VisitAssignmentNode(node);
    }
    public override void VisitFunctionCallNode(CallExpression node) {
        ProcessNode(node);
        base.VisitFunctionCallNode(node);
    }
    public override void VisitIfStatementNode(IfStatementNode node) {
        ProcessNode(node);
        base.VisitIfStatementNode(node);
    }
    public override void VisitForRangeNode(ForRangeStatement node) {
        ProcessNode(node);
        base.VisitForRangeNode(node);
    }

    public override void VisitForLoopNode(ForLoopStatement node) {
        ProcessNode(node);
        base.VisitForLoopNode(node);
    }
    public override void VisitReturnStatementNode(ReturnStatementNode node) {
        ProcessNode(node);
        base.VisitReturnStatementNode(node);
    }
    public override void VisitNodeList<T>(NodeList<T> node) {
        base.VisitNodeList(node);
    }*/
}