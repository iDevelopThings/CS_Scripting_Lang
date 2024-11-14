namespace CSScriptingLang.Tests.IncrementalInterpreterTests;

[TestFixture]
[UsingIncrementalSyntaxTreeEvaluation]
public class TypeDeclaration_Struct_Test : IncrementalParserTest
{

    [Test]
    public void Construction() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst);

            """
        );

        var structInst = Vars["inst"];
        structInst.Should().NotBeNull();
    }

    [Test]
    public void ZeroValOnInit() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst);

            """
        );

        var structInst = Vars["inst"];
        structInst.Should().NotBeNull();

        structInst.GetMember("name").Should().MatchRawValue("");
        structInst.GetMember("age").Should().MatchRawValue(0);
    }
    [Test]
    public void Constructors() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
                
                Struct() {
                    print('constructor(Struct()) called');
                }
                
                Struct(int age) {
                    print('constructor(Struct(int)) called');
                    this.age = age;
                }
                
                Struct(int age, string name) {
                    print('constructor(Struct(int, string)) called');
                    this.age = age;
                    this.name = name;
                }
            }

            var instA = new<Struct>(2);
            inspect(instA);

            var instB = new<Struct>(3, 'John');
            inspect(instB);

            var instC = new<Struct>();
            inspect(instC);

            """
        );

        var structInstA = Vars["instA"];
        structInstA.Should().NotBeNull();
        structInstA.GetMember("name").Should().MatchRawValue(string.Empty);
        structInstA.GetMember("age").Should().MatchRawValue(2);

        var structInstB = Vars["instB"];
        structInstB.Should().NotBeNull();
        structInstB.GetMember("name").Should().MatchRawValue("John");
        structInstB.GetMember("age").Should().MatchRawValue(3);

        var structInstC = Vars["instC"];
        structInstC.Should().NotBeNull();
        structInstC.GetMember("name").Should().MatchRawValue(string.Empty);
        structInstC.GetMember("age").Should().MatchRawValue(0);
    }

    [Test]
    public void SettingData() {
        Execute(
            """
            type Struct struct {
                name string
                age  int
            }

            var inst = new<Struct>();
                    
            inspect(inst, 'var inst = new<Struct>()');

            inst.name = "John";
            inst.age = 25;
            inst.someFunc = () => {
                print("Hello from someFunc, {0} {1}", inst.name, inst.age);
                print("Hello from someFunc, {0} {1}", this.name, this.age);
                inspect(this, 'inside someFunc closure');
            };

            inspect(inst, 'pre inst.someFunc()');

            inst.someFunc();

            """
        );

        var structInst = Vars["inst"];
        structInst.GetMember("name").Should().MatchRawValue("John");
        structInst.GetMember("age").Should().MatchRawValue(25);
    }

    [Test]
    public void Methods() {
        Execute(
            """

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
        structInst.GetMember("name").Should().MatchRawValue("John");
        Vars["n"].Should().MatchRawValue("John");

    }

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
        structInst.GetMember("name").Should().MatchRawValue("John");
        Vars["n"].Should().MatchRawValue("John");

    }
    [Test]
    public void StructWithDefs() {
        Execute(
            """

            type Array struct
            {
              def push(int pls, int plspsl) int32;
              def removeAt(object index) int32;
              def removeRange(object start, object end) int32;
              def getEnumerator() object;
            }


            """
        );

    }
}