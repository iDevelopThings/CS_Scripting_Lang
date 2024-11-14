namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class TypeDeclaration_Interface_Test : IncrementalParserTest
{
    [Test]
    public void InterfaceDeclaration() {
        Execute(
            """

            type ITest interface {
                GetName() string
                SetName(string name)
            }
            type Struct struct {
                name string
                
                GetName() string {
                    print('Get Name is: {0}', this.name);
                    return this.name;
                }
                
                SetName(string name) {
                    this.name = name;
                    print('Name set to: {0}', this.name);
                }
            }

            var inst = new<Struct>();

            inst.GetName();
                    
            inspect(inst);

            inst.SetName("John");
            var n = inst.GetName();

            """
        );

        var structInst = Vars["inst"];
        Assert.That(structInst.GetMember("name").As.String(), Is.EqualTo("John"));
        Assert.That(Vars["n"].As.String(), Is.EqualTo("John"));

    }
}