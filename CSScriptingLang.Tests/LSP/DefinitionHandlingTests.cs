using CSScriptingLang.Core.Diagnostics;
using CSScriptingLang.IncrementalParsing.Syntax.SyntaxNodes;

namespace CSScriptingLang.Tests.LSP;

[TestFixture]
[LSPTest]
[UsingIncrementalSyntaxTreeEvaluation]
public class DefinitionHandlingTests : IncrementalParserTest
{
    [Test]
    public void SimpleObjectMemberReference() {
        var script = InitScript(
            """
            var obj = {a : 'true'};
            var val = obj.a;
            """
        );


        var objDeclNode = script.SyntaxTree.GetElement<VariableDecl>(0);
        var valDeclNode = script.SyntaxTree.GetElement<VariableDecl>(1);

        var obj = objDeclNode.ResolvedType;
        obj.Should().NotBeNull();

        var val = valDeclNode.ResolvedType;
        val.Should().NotBeNull();
        val.Name.Should().Be("String");

    }
    [Test]
    public void MoreComplexObjectMemberReference() {
        var script = InitScript(
            """
            var otherObj = {a : 'true'};
            var obj = {other : otherObj};
            var val = obj.other;
            """
        );

        var otherObjDeclNode = script.SyntaxTree.GetElement<VariableDecl>(0);
        var objDeclNode      = script.SyntaxTree.GetElement<VariableDecl>(1);
        var valDeclNode      = script.SyntaxTree.GetElement<VariableDecl>(2);

        var otherObj = otherObjDeclNode.ResolvedType;
        otherObj.Should().NotBeNull();

        var obj = objDeclNode.ResolvedType;
        obj.Should().NotBeNull();

        var val = valDeclNode.ResolvedType;
        val.Should().NotBeNull()
           .And
           .BeEquivalentTo(otherObj);

    }
    [Test]
    public void FindReferencesCalls() {
        var script = InitScript(
            """
            var otherObj = {a : 'true'};
            var obj = {other : otherObj};
            var val = obj.other;
            """
        );

        var otherObjDeclNode = script.SyntaxTree.GetElement<VariableDecl>(0);
        var objDeclNode      = script.SyntaxTree.GetElement<VariableDecl>(1);
        var valDeclNode      = script.SyntaxTree.GetElement<VariableDecl>(2);

        var otherObj = otherObjDeclNode.ResolvedType;
        otherObj.Should().NotBeNull();

        var obj = objDeclNode.ResolvedType;
        obj.Should().NotBeNull();

        var val                = valDeclNode.ResolvedType;
        var valObjRefs         = valDeclNode.VarValuePairs.ToArray()[0].value.ChildNodes<IdentifierExpr>().ElementAtOrDefault(0)!;
        var valObjOtherRefs    = valDeclNode.VarValuePairs.ToArray()[0].value.ChildNodes<IdentifierExpr>().ElementAtOrDefault(1)!;
        var valObjMemberAccess = valDeclNode.VarValuePairs.ToArray()[0].value;

        var objRefs = valObjRefs.FindReferences().ToList();
        objRefs.Should().HaveCount(1);
        objRefs[0].SyntaxNode.Parent.Should().BeEquivalentTo(objDeclNode);

        var objOtherRefs = valObjOtherRefs.FindReferences().ToList();

        var memberAccessRefs = valObjMemberAccess.FindReferences().ToList();
        memberAccessRefs.Should().HaveCount(1);
        memberAccessRefs[0].SyntaxNode.Parent.Should().BeEquivalentTo(objDeclNode);
    }
    [Test]
    public void FindReferencesCalls_MemberAccess_FnArg() {
        var script = InitScript(
            """

            type OtherObject struct
            {
              pls string
            }
            type MyObject struct
            {
              Value OtherObject
              a int
            }
            function run(MyObject o) {
                var x = o.a;
                var b = o.Value;
            }

            """
        );

        var varDecl = script.SyntaxTree.GetElement<VariableDecl>(0);
        var valueDecl = script.SyntaxTree.GetElement<VariableDecl>(1);

        var varDeclType = varDecl.ResolvedType;
        varDeclType.Should().NotBeNull();

        var objRefs = varDecl.FindReferences().ToList();
        var valueRefs = valueDecl.Descendants.LastOrDefault()!.FindReferences().ToList();
        
        objRefs.Should().HaveCount(1);

    }
    [Test]
    public void FindReferencesCallss() {
        var script = InitScript(
            """

            var objj = {
             a : 1,
             e : { bye : { message : 'bye' }}
            };

            var msg = objj.e.bye.message;
            var msg = objj.e.bye.message;

            function run() {
                var pls = objj;
                // var pls = objj.a;
            }

            """
        );


        DiagnosticManager.TryConsumeScriptDiagnostics(script.Id);

        var plsVar = script.SyntaxTree.GetElement<VariableDecl>(v => v.Vars.All(x => x == "pls"));
        plsVar.Should().NotBeNull();

        var refs = plsVar.FindReferences().ToList();
        refs.Should().HaveCount(1);
    }
}