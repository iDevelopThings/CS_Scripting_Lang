using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Types;

namespace CSScriptingLang.RuntimeValues.Values;

public partial class Value
{

    public bool IsEnumerable {
        get {
            var hasGetEnumerator  = this["getEnumerator"].Type == RTVT.Function;
            var hasEnumeratorFunc = this["moveNext"].Type == RTVT.Function;
            return hasGetEnumerator || hasEnumeratorFunc;
        }
    }



    public IEnumerable<Value> Enumerate(ExecContext ctx) {
        var enumerator = this;
        var moveNext   = enumerator["moveNext"];

        if (moveNext.Type != RTVT.Function) {
            var getEnumerator = this["getEnumerator"];
            if (getEnumerator.Type != RTVT.Function)
                throw new InterpreterRuntimeException("Value is not enumerable");

            enumerator = ctx.Call(getEnumerator, this);

            moveNext = enumerator["moveNext"];
            if (moveNext.Type != RTVT.Function)
                throw new InterpreterRuntimeException("Value is not enumerable");
        }

        while (ctx.Call(moveNext, this)) {
            yield return enumerator["current"];
        }
    }
}

public class EnumerableIterator : IIterator
{
    protected Value       _enumerator;
    protected ExecContext _ctx;

    public EnumerableIterator(Value enumerator, ExecContext ctx) {
        _ctx        = ctx;
        _enumerator = enumerator;

        if (_enumerator["moveNext"].Type != RTVT.Function) {
            var getEnumerator = _enumerator["getEnumerator"];
            if (getEnumerator.Type != RTVT.Function)
                throw new InterpreterRuntimeException("Value is not enumerable");
            _enumerator = _ctx.Call(getEnumerator, _enumerator);
        }
    }

    public bool MoveNext() {
        return _ctx.Call(_enumerator["moveNext"], _enumerator);
    }

    public virtual Value Current      => _enumerator["current"];
    // public virtual Value CurrentIndex => _enumerator["current"];
    public virtual Value CurrentIndex => _enumerator.HasMember("currentIndex") ? _enumerator["currentIndex"] : _enumerator["current"];

    public void Reset() { }
}

public class ObjectEnumerableIterator : EnumerableIterator
{

    public ObjectEnumerableIterator(Value obj, ExecContext ctx) : base(obj, ctx) {
    }

    public override Value Current      => _enumerator["current"]["value"];
    public override Value CurrentIndex => _enumerator["current"]["key"];

}