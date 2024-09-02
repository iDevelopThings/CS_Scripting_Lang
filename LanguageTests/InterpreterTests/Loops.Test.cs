namespace LanguageTests.InterpreterTests;

[TestFixture]
public class LoopsTest : BaseCompilerTest
{
    [Test(Description = "For i; i < 10; i = i + 1")]
    public void For_i_plus_1() {
        var interp = Execute(@"
            for (var i = 0; i < 10; i = i + 1) {
                print(i);
            }
        ");

        Assert.That(interp.Symbols["i"].RawValue, Is.EqualTo((double) 10));
    }
    
    [Test(Description = "For i; i < 10; i++")]
    public void For_i_plus_plus() {
        var interp = Execute(@"
            for (var i = 0; i < 10; i++) {
                print(i);
            }
        ");

        Assert.That(interp.Symbols["i"].RawValue, Is.EqualTo((double) 10));
    }
    
    [Test(Description = "Reverse For i; i > 0; i--")]
    public void For_i_minus_minus() {
        var interp = Execute(@"
            for (var i = 10; i > 0; i--) {
                print(i);
            }
        ");

        Assert.That(interp.Symbols["i"].RawValue, Is.EqualTo((double) 0));
    }
    
    [Test(Description = "For using a variable from the outer scope")]
    public void For_outer_scope_variable() {
        var interp = Execute(@"
            var i = 0;
            for (; i < 10; i++) {
                print(i);
            }
        ");

        Assert.That(interp.Symbols["i"].RawValue, Is.EqualTo((double) 10));
    }
    
    [Test]
    public void ForRange() {
        var interp = Execute(@"
            var counter = 0;
            for (var i = range 10) {
                print(i);
                counter = i;
            }
        ");

        Assert.That(interp.Symbols["counter"].RawValue, Is.EqualTo(10));
    }
    
    [Test]
    public void ForRange_Array() {
        var interp = Execute(@"
            var arr = [1, 2, 3, 4, 5];
            for (var (i, el) = range arr) {
                print(el);
            }
        ");

        Assert.That(interp.Symbols["el"].RawValue, Is.EqualTo((double) 5));
    }
}