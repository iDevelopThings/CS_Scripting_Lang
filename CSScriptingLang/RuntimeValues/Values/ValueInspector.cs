using System.Globalization;
using CSScriptingLang.Common.CodeWriter;
using CSScriptingLang.Interpreter;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.Utils;

namespace CSScriptingLang.RuntimeValues.Values;

public static class ValueInspectorExt
{
    public static string Inspect(this Value value, bool allowColors = true) {
        return ValueInspector.Inspect(value, null, allowColors);
    }
    public static string Inspect(this Value value, bool allowColors, Action<Writer, Func<string>> writerAction) {
        var w = new Writer();

        var writeAction = new Func<string>(() => {
            return ValueInspector.Inspect(value, w, allowColors);
        });

        writerAction(w, writeAction);

        return w.ToString();
    }

    public static string Inspect(this IEnumerable<VariableSymbol> values, bool allowColors, Action<Writer, Func<string>> writerAction) {
        return values.Select(x => x.Val).Inspect(allowColors, writerAction);
    }
    public static string Inspect<T>(this IEnumerable<T> values, bool allowColors, Action<Writer, Func<string>> writerAction) where T : Value {
        var w = new Writer();

        var writeAction = new Func<string>(() => {
            return ValueInspector.InspectValues(values, w, allowColors);
        });

        writerAction(w, writeAction);

        return w.ToString();
    }
}

public class ValueInspector
{
    public static string Inspect(Value value, Writer parentWriter, bool allowColors = true) {
        var w = new Writer(parentWriter);

        switch (value) {
            case {Is.Null: true}:
                return InspectNull(value, w, allowColors);
            case {Is.Boolean: true}:
                return InspectBoolean(value, w, allowColors);
            case {Is.String: true}:
                return InspectString(value, w, allowColors);
            case {Is.Number: true}:
                return InspectNumber(value, w, allowColors);
            case {Is.Object: true}:
                return InspectObject(value, w, allowColors);
            case {Is.Array: true}:
                return InspectValueArray(value, w, allowColors);
            case {Is.Signal: true}:
                return InspectSignal(value, w, allowColors);
            case {Is.Function: true}:
                return InspectFunction(value, w, allowColors);
            default:
                return "UNKNOWN VALUE";
        }
    }
    public static string InspectNull(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);
        w.WriteInline("null".ColorIf(allowColors, AnsiColorCodes.Gray));
        return w.ToString();
    }
    public static string InspectBoolean(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);
        w.WriteInline(v.As.Bool() ? "true".ColorIf(allowColors, AnsiColorCodes.Green) : "false".ColorIf(allowColors, AnsiColorCodes.Red));
        return w.ToString();
    }
    public static string InspectString(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);
        w.WriteInline(v.As.String().ApplyColorTags(allowColors));
        return w.ToString();
    }
    public static string InspectNumber(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);
        switch (v.Type) {
            case RTVT.Int32:
                w.WriteInline("i32(".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                w.WriteInline(v.As.Int().ToString(CultureInfo.InvariantCulture).ColorIf(allowColors, AnsiColorCodes.BrightWhite));
                w.WriteInline(")".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                break;
            case RTVT.Int64:
                w.WriteInline("i64(".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                w.WriteInline(v.As.Int64().ToString(CultureInfo.InvariantCulture).ColorIf(allowColors, AnsiColorCodes.BrightWhite));
                w.WriteInline(")".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                break;
            case RTVT.Float:
                w.WriteInline("f32(".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                w.WriteInline(v.As.Float().ToString(CultureInfo.InvariantCulture).ColorIf(allowColors, AnsiColorCodes.BrightWhite));
                w.WriteInline(")".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                break;
            case RTVT.Double:
                w.WriteInline("f64(".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                w.WriteInline(v.As.Double().ToString(CultureInfo.InvariantCulture).ColorIf(allowColors, AnsiColorCodes.BrightWhite));
                w.WriteInline(")".ColorIf(allowColors, AnsiColorCodes.BrightGray));
                break;
            default:
                w.WriteInline($"UNKNOWN VALUE OF TYPE {v.Type} -> {v.GetUntypedValue()}".ColorIf(allowColors, AnsiColorCodes.BrightWhite));
                break;
        }

        return w.ToString();
    }
    public static string InspectObject(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);
        using (w.bNoIndent("Object")) {
            foreach (var pair in v.Members) {
                w.WriteInlineIndented($"{pair.Key} : ");
                var val = Inspect(pair.Value, w, allowColors);
                w.WriteInline(val);
                w.WriteInline(",\n");
            }
        }

        return w.ToString();
    }
    public static string InspectValues(IEnumerable<Value> values, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);

        w.Array(values, (element, writer) => {
            var val = Inspect(element, writer, allowColors);
            writer.WriteInline(val);
        });

        return w.ToString();
    }

    public static string InspectValueArray(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);

        w.Array(v.As.Array(), (element, writer) => {
            var val = Inspect(element, writer, allowColors);
            writer.WriteInline(val);
        });
        return w.ToString();
    }
    public static string InspectSignal(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);

        using (w.bNoIndent("Signal")) {
            foreach (var pair in v.Members) {
                w.WriteInlineIndented($"{pair.Key} : ");
                var val = Inspect(pair.Value, w, allowColors);
                w.WriteInline(val);
                w.WriteInline(",\n");
            }
        }

        return w.ToString();
    }
    public static string InspectFunction(Value v, Writer parentWriter = null, bool allowColors = true) {
        var w = new Writer(parentWriter);


        using (w.bNoIndent($"Function({v.As.Fn().Name})")) {
            /*foreach (var param in v.Parameters) {
                w.WriteInlineIndented($"{param.Name} : {param.Type?.Name ?? "UNKNOWN"} = ");
                var val = Inspect(param.DefaultValue, w, allowColors);
                w.WriteInline(val);
                w.WriteInline(",\n");
            }*/
        }

        return w.ToString();
    }
}