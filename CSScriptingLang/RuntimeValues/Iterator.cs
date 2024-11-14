using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Values;

namespace CSScriptingLang.RuntimeValues;

public interface IIterator
{
    bool  MoveNext();
    Value Current      { get; }
    Value CurrentIndex { get; }
    // Value MaxIndex     { get; }
    void  Reset();
}

public interface IIterable
{
    IIterator GetIterator(ExecContext ctx);
}

public class NumberRangeIterator<TNative> : IIterator where TNative : struct
{
    private readonly Value _start;
    private readonly Value _end;
    private readonly Value _step;

    private Value _current;

    public NumberRangeIterator(Value end) {
        _start   = Value.Number(-1);
        _end     = end;
        _current = Value.Number(-1);
        _step    = Value.Number(1);
    }

    public bool MoveNext() {
        var val = _current.As.Int();
        val += _step.As.Int();
        _current.SetValue(val);

        if ((_current < _end).IsTruthy()) {
            return true;
        }
        // if (_current.Operator_LessThan(_end).Value<bool>()) {
        // return true;
        // }

        _current = _end;

        return false;
    }

    public Value Current      => _current;
    public Value CurrentIndex => _current;
    public Value MaxIndex     => _end;

    public void Reset() {
        _current = _start;
    }
}

public class ArrayIterator : IIterator
{
    private readonly Value _array;

    private int   _position = -1;
    private Value _currentIndex;

    public ArrayIterator(Value array) {
        _array        = array;
        _currentIndex = 0;
    }

    public bool MoveNext() {
        _position++;
        var inRange = _position < _array.As.Array().Count;
        if (inRange) {
            _currentIndex.SetValue(_position);
        }

        return inRange;
    }

    public Value Current => _array[_position];

    // public int       CurrentIndex => _position;
    public Value CurrentIndex => _currentIndex;
    public Value MaxIndex     => _array.As.Array().Count;

    public void Reset() {
        _position = -1;
    }
}

public class ObjectIterator : IIterator
{
    private readonly Value _object;
    private          int   _position = -1;

    private Value _currentKey;

    private List<KeyValuePair<string, Value>> _fields;

    public ObjectIterator(Value obj) {
        _object     = obj;
        _fields     = _object.Members.ToList();
        _currentKey = "";
    }

    public bool MoveNext() {
        _position++;
        var inRange = _position < _fields.Count;
        if (inRange) {
            _currentKey.SetValue(_fields[_position].Key);
        }

        return inRange;
    }

    public Value Current      => _fields[_position].Value;
    public Value CurrentIndex => _currentKey;
    public Value MaxIndex     => _fields.Count;

    public void Reset() {
        _position = -1;
    }
}

public class StringIterator : IIterator
{
    private readonly string _str;

    private int   _position = -1;
    private Value _currentIndex;

    public StringIterator(string str) {
        _str          = str;
        _currentIndex = 0;
    }

    public bool MoveNext() {
        _position++;
        var inRange = _position < _str.Length;
        if (inRange) {
            _currentIndex.SetValue(_position);
        }

        return inRange;
    }

    public Value Current      => _str[_position].ToString();
    public Value CurrentIndex => _currentIndex;
    public Value MaxIndex     => _str.Length;

    public void Reset() {
        _position = -1;
    }
}