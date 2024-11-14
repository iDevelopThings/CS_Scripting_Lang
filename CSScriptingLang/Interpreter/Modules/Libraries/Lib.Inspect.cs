using CSScriptingLang.Common.Extensions;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Interpreter.Libraries;

[LanguageModuleBind(FunctionsAsGlobals = true)]
public static partial class Lib_Inspect
{
    // public sealed partial class Library : ILibrary
    // {
    //     public IEnumerable<KeyValuePair<string, Value>> GetDefinitions(ExecContext ctx) {
    //         yield break;
    //     }
    // }

    [LanguageGlobalFunction("inspect")]
    [LanguageMetaDefinition("def function inspect(object value, string context) void;")]
    public static void Inspect(
        FunctionExecContext ctx,
        Value               inspectValue,
        string              contextString = null
    ) {
        using var wr = new ValueTextWriter();
        wr.Write("Inspect");

        if (inspectValue == null) {
            wr.Write(" -> Replaced with null value");
            inspectValue = Value.Null();
        }
        
        if (inspectValue.Symbol?.Name != null) {
            wr.Write($"({inspectValue.Symbol.Name.Bold()})");
        }

        if (contextString.IsNullOrEmpty()) {
            contextString = ctx.CallerExpr?.SourceLocationString();
        } else {
            contextString += " " + ctx.CallerExpr?.SourceLocationString();
        }

        if (contextString != null) {
            wr.Write($" ({contextString.BrightGray()})");
        }


        ValueTextWriter WriteVal(Value value, ValueTextWriter parentWriter, int depth = 0) {
            using var vw = new ValueTextWriter(parentWriter);

            switch (value) {
                case {Is.Array: true}: {
                    var arr = value.As.List();

                    vw.WriteArray(aw => {
                        foreach (var val in arr) {
                            aw.Add(WriteVal(val, aw, depth + 1));
                        }
                    });

                    break;
                }
                case {Is.Object: true}: {
                    var obj = value.As.Object();

                    return vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow, depth + 1));
                        }
                    }, false, true, true, true);
                }
                case {Is.Struct: true}: {
                    var obj = value.As.Struct();

                    vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow));
                        }
                    });

                    break;
                }
                case {Is.String : true}:
                case {Is.Number : true}:
                case {Is.Boolean: true}:
                case {Is.Null   : true}:
                case {Is.Unit   : true}: {
                    vw.Write(value.Type.FormattedValueString(value.GetUntypedValue()));
                    break;
                }
                case {Is.Function: true}: {
                    vw.Write(value.Type.FormattedValueString(value.As.Fn().Name));
                    break;
                }
                case {Is.Signal: true}: {
                    var sig = value.As.Signal();
                    vw.Write(value.Type.FormattedValueString($"{sig?.Name ?? "undefined"} - {sig?.Listeners.Count ?? 0} listeners"));
                    break;
                }

                case {Is.Enum: true}: {
                    var obj = value.GetUntypedValue() as Dictionary<string, Value>;

                    return vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow, depth + 1));
                        }
                    }, false, true, true, true);
                }
                /*case {Is.Enum: true}: {
                    return vw.WriteObject(w => {
                        var enumProto = (EnumPrototype) value.PrototypeType;

                        enumProto.DeclaredEnumMembers.ForEach((m, i) => {
                            var memberValue = value[m.Name];
                            w.Add(
                                $"{m.Name}(EnumMember)",
                                WriteVal(memberValue, w, depth + 1)
                                );
                        });

                    }, false, true, true, true);
                }*/
                case {Is.EnumMember: true}: {
                    return vw.WriteObject(w => {
                        w.Add("Name", value["name"].As.String());
                        w.Add("Value", WriteVal(value["value"], w, depth + 1));
                    }, false, true, true, true);
                }
                case null: {
                    vw.Write("null");
                    break;
                }
                default: {
                    throw new NotImplementedException($"Value type {value.Type} is not implemented for inspection");
                }
            }

            return vw;
        }

        wr.InstantWriteObject(w => {
            w.Add("Type", $"{inspectValue.PrototypeType?.ValueType?.Name}({$"RTVT={inspectValue.Type}".BrightGray()})");

            w.Add("Value", WriteVal(inspectValue, w, 1));

            /*var fields = inspectValue.Members.ToList();
            if (fields.Count > 0) {
                w.AddComment("Fields");
                foreach (var (name, val) in fields) {
                    w.Add(name, WriteVal(val, w, 1));
                }
            }*/

            var declared = inspectValue.AllDeclaredMembers().ToList();
            if (declared.Count > 0) {
                w.AddComment("Declared Fields");
                foreach (var (key, value) in declared) {
                    w.Add(key, value.MemberType.ToString());
                }
            }
        });

        /*string WriteValue(Value Value, int indent = 0) {
            string GetIndent(int i) => new(' ', i * 2);
            var ident = GetIndent(indent);

            var sb = new StringBuilder();

            switch (Value) {
                case {Is.Array: true}: {
                    var arr = Value.As.List();
                    sb.Append($"\n{ident}[\n");
                    for (var i = 0; i < arr.Count; i++) {
                        var val = arr[i];
                        sb.Append($"{ident}{GetIndent(indent + 1)}{i}: ");
                        var valStr = WriteValue(val);
                        sb.Append(valStr);
                        if (i < arr.Count - 1) {
                            sb.Append(", ");
                        }
                    }

                    sb.Append($"\n{ident}]");
                    break;
                }
                case {Is.Object: true}: {
                    var obj = Value.As.Object();
                    sb.Append($"{ident}{{\n");

                    var allFields = Value.AllUniqueMembers();

                    foreach (var (key, val) in allFields) {
                        var isProtoField = obj.ContainsKey(key);
                        sb.Append($"{ident}{GetIndent(indent + 1)}{key}(Inherited={isProtoField}): ");
                        var valStr = WriteValue(val, 0);
                        sb.Append(valStr);
                        sb.Append(", \n");
                    }

                    sb.Append("\n");
                    sb.Append($"{ident}}}");
                    break;
                }
                case {Is.Struct: true}: {
                    var obj = Value.As.Struct();
                    sb.Append($"{ident}{{");
                    foreach (var (key, val) in obj) {
                        sb.Append($"{ident}{GetIndent(indent + 1)}{key}: ");
                        var valStr = WriteValue(val, 0);
                        sb.Append(valStr);
                        sb.Append(", ");
                    }

                    sb.Append($"{ident}}}");
                    break;
                }
                case {Is.String: true}: {
                    sb.Append($"{ident}\"{Value.As.String()}\"");
                    break;
                }
                case {Is.Number: true}: {
                    sb.Append($"{ident}{Value.Type.FormattedStringNumber(Value.GetUntypedValue())}");
                    break;
                }
                case {Is.Boolean: true}: {
                    sb.Append($"{ident}{Value.As.Bool()}");
                    break;
                }
                case {Is.Null: true}: {
                    sb.Append($"{ident}null");
                    break;
                }
                case {Is.Unit: true}: {
                    sb.Append($"{ident}unit");
                    break;
                }
                case {Is.Function: true}: {
                    sb.Append($"{ident}function({Value.As.Fn().Name})");
                    break;
                }
                case {Is.Signal: true}: {
                    var sig = Value.As.Signal();

                    sb.Append($"{ident}signal({sig?.Name ?? "undefined"} - {sig?.Listeners.Count ?? 0} listeners)");
                    break;
                }
                case {Is.Enum: true}: {
                    using var vw = new ValueTextWriter(sb);
                    vw.SetIndent(indent);

                    vw.WriteObject(w => {
                        var enumProto = (EnumPrototype) Value.PrototypeType;

                        enumProto.DeclaredEnumMembers.ForEach((m, i) => {
                            var memberValue = Value[m.Name];
                            w.Add(m.Name, WriteValue(memberValue));
                        });

                    });

                    break;
                }
                case {Is.EnumMember: true}: {
                    using var vw = new ValueTextWriter(sb);
                    vw.SetIndent(indent);
                    vw.WriteObject(w => {
                        w.Add("Name", Value["name"].As.String());
                        w.Add("Value", WriteValue(Value["value"]));
                    });
                    break;
                }
                default: {
                    throw new NotImplementedException($"Value type {Value.Type} is not implemented for inspection");
                }
            }

            return sb.ToString();
        }*/


        /*if (inspectValue is ValueSignal signal) {
            Console.WriteLine("\tSignal:");
            Console.WriteLine($"\t\tSubscribers: {signal.Listeners.Count}");
        }

        if (inspectValue is ValueFunction func) {
            Console.WriteLine("\tFunction:");
            if (func.Value is { } fn) {
                Console.WriteLine($"\t\tParameters: {(fn.Parameters.Count == 0 ? "None" : string.Join(", ", fn.Parameters.Arguments.Select(p => p.Name)))}");
            }
        }*/

        Console.WriteLine(wr.ToString());
    }

    public static string InspectString(
        Value  inspectValue,
        string contextString = null
    ) {
        using var wr = new ValueTextWriter();
        wr.Write("Inspect");

        if (inspectValue.Symbol?.Name != null) {
            wr.Write($"({inspectValue.Symbol.Name.Bold()})");
        }

        if (contextString != null) {
            wr.Write($" ({contextString.BrightGray()})");
        }

        ValueTextWriter WriteVal(Value value, ValueTextWriter parentWriter, int depth = 0) {
            using var vw = new ValueTextWriter(parentWriter);

            switch (value) {
                case {Is.Array: true}: {
                    var arr = value.As.List();

                    vw.WriteArray(aw => {
                        foreach (var val in arr) {
                            aw.Add(WriteVal(val, aw, depth + 1));
                        }
                    });

                    break;
                }
                case {Is.Object: true}: {
                    var obj = value.As.Object();

                    return vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow, depth + 1));
                        }
                    }, false, true, true, true);
                }
                case {Is.Struct: true}: {
                    var obj = value.As.Struct();

                    vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow));
                        }
                    });

                    break;
                }
                case {Is.String : true}:
                case {Is.Number : true}:
                case {Is.Boolean: true}:
                case {Is.Null   : true}:
                case {Is.Unit   : true}: {
                    vw.Write(value.Type.FormattedValueString(value.GetUntypedValue()));
                    break;
                }
                case {Is.Function: true}: {
                    vw.Write(value.Type.FormattedValueString(value.As.Fn().Name));
                    break;
                }
                case {Is.Signal: true}: {
                    var sig = value.As.Signal();
                    vw.Write(value.Type.FormattedValueString($"{sig?.Name ?? "undefined"} - {sig?.Listeners.Count ?? 0} listeners"));
                    break;
                }

                case {Is.Enum: true}: {
                    var obj = value.GetUntypedValue() as Dictionary<string, Value>;

                    return vw.WriteObject(ow => {
                        var allFields = value.AllUniqueMembers();
                        foreach (var (key, val) in allFields) {
                            var keyStr       = key;
                            var isProtoField = obj.ContainsKey(key);

                            keyStr += isProtoField ? " (Inherited)".BrightGray() : "";

                            ow.Add(keyStr, WriteVal(val, ow, depth + 1));
                        }
                    }, false, true, true, true);
                }
                /*case {Is.Enum: true}: {
                    return vw.WriteObject(w => {
                        var enumProto = (EnumPrototype) value.PrototypeType;

                        enumProto.DeclaredEnumMembers.ForEach((m, i) => {
                            var memberValue = value[m.Name];
                            w.Add(
                                $"{m.Name}(EnumMember)",
                                WriteVal(memberValue, w, depth + 1)
                                );
                        });

                    }, false, true, true, true);
                }*/
                case {Is.EnumMember: true}: {
                    return vw.WriteObject(w => {
                        w.Add("Name", value["name"].As.String());
                        w.Add("Value", WriteVal(value["value"], w, depth + 1));
                    }, false, true, true, true);
                }
                default: {
                    throw new NotImplementedException($"Value type {value.Type} is not implemented for inspection");
                }
            }

            return vw;
        }

        wr.InstantWriteObject(w => {
            w.Add("Type", $"{inspectValue.PrototypeType?.ValueType?.Name}({$"RTVT={inspectValue.Type}".BrightGray()})");

            w.Add("Value", WriteVal(inspectValue, w, 1));

            /*var fields = inspectValue.Members.ToList();
            if (fields.Count > 0) {
                w.AddComment("Fields");
                foreach (var (name, val) in fields) {
                    w.Add(name, WriteVal(val, w, 1));
                }
            }*/

            var declared = inspectValue.AllDeclaredMembers().ToList();
            if (declared.Count > 0) {
                w.AddComment("Declared Fields");
                foreach (var (key, value) in declared) {
                    w.Add(key, value.MemberType.ToString());
                }
            }
        });

        return wr.ToString();
    }
}