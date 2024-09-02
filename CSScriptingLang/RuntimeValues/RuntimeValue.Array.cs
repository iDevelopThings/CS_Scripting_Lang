using CSScriptingLang.Utils.CodeWriter;

namespace CSScriptingLang.RuntimeValues;

public class RuntimeValue_Array : RuntimeValue
{
    public List<RuntimeValue> Elements {
        get => (List<RuntimeValue>) Value;
        set => Value = value;
    }

    public bool CanAutoExpand { get; set; } = true;

    public int Length => Elements.Count;

    public Action<RuntimeValue> OnElementAdded   { get; set; }
    public Action<RuntimeValue> OnElementRemoved { get; set; }


    public RuntimeValue_Array() {
        _value = new List<RuntimeValue>();
    }

    public override RuntimeValue GetField(RuntimeValue index) {
        if (index.Is<string>()) {
            var strField = index.As<string>();
            if(strField == "length")
                return Rent(Length);
        } 
        
        var idx = index.As<int>();
        return Get(idx);
    }
    public override RuntimeValue GetField(string name) {
        if (name == "length")
            return Rent(Length);

        // Assume we're accessing an index
        if (int.TryParse(name, out var index))
            return Get(index);

        return base.GetField(name);
    }
    public override void SetField(RuntimeValue index, RuntimeValue value) {
        var idx = index.As<int>();
        Set(idx, value);
    }

    /*public new static RuntimeValue_Array Rent(params object[] args) {

    }*/

    public RuntimeValue this[int index] {
        get => Get(index);
        set => Set(index, value);
    }

    public RuntimeValue Get(int index) {
        if (index < 0 || index >= Elements.Count) {
            Logger.Warning($"Index out of range: {index}");
            return null;
        }

        return Elements[index];
    }

    public void Remove(int index) {
        if (index < 0 || index >= Elements.Count) {
            Logger.Warning($"Index out of range: {index}");
            return;
        }

        var value = Elements[index];
        Elements.RemoveAt(index);

        OnElementRemoved?.Invoke(value);
    }

    public void Add(RuntimeValue value) {
        Elements.Add(value);
        OnElementAdded?.Invoke(value);
    }

    public void Set(int index, RuntimeValue value) {
        if (index < 0)
            throw new IndexOutOfRangeException("Index cannot be negative");

        if (index >= Elements.Count) {
            if (!CanAutoExpand) {
                Logger.Warning($"Index out of range: {index}");
                return;
            }

            Elements.AddRange(Enumerable.Repeat<RuntimeValue>(null, index - Elements.Count + 1));
        }

        Elements[index] = value;

        OnElementAdded?.Invoke(value);
    }

    public override string Inspect(Writer parentWriter = null) {
        var w = new Writer(parentWriter);

        w.Array(Elements, (element, writer) => {
            writer.WriteInline(element.Inspect(w));
        });

        return w.ToString();
    }
}