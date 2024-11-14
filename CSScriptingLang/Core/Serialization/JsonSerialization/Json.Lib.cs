using System.Globalization;
using System.Text;
using CSScriptingLang.Interpreter.Bindings;
using CSScriptingLang.Interpreter.Context;
using CSScriptingLang.Interpreter.Execution.Declaration;
using CSScriptingLang.Interpreter.Libraries;
using CSScriptingLang.Lexing;
using CSScriptingLang.RuntimeValues.Prototypes;
using CSScriptingLang.RuntimeValues.Types;
using CSScriptingLang.RuntimeValues.Values;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Core.Serialization.JsonSerialization;

[LanguageModuleBind("Json")]
public static partial class Lib_Json
{
    [LanguageFunction]
    public static Value Deserialize(FunctionExecContext ctx, string text) {
        text = text.Trim();

        if (string.IsNullOrEmpty(text))
            return Value.Null();

        var parser = new JsonParser(text);

        var value = parser.ParseValue();
        parser.Require(JsonTokenType.End);

        return value;
    }
    [LanguageFunction]
    public static Value DeserializeTo(string text, Value value) {
        text = text.Trim();

        if (string.IsNullOrEmpty(text))
            return Value.Null();

        var parser = new JsonParser(text);

        var outValue = parser.ParseValue(value);
        parser.Require(JsonTokenType.End);

        return outValue;
    }
    
    public static Value DeserializeTo(string text, Value value, out JsonValue jsonValue) {
        text = text.Trim();

        if (string.IsNullOrEmpty(text)) {
            jsonValue = JsonValue.Null();
            return Value.Null();
        }

        var parser = new JsonParser(text);

        jsonValue = parser.ParseJsonValue();
        parser.Require(JsonTokenType.End);

        if (jsonValue.Type != JsonValueType.Object)
            throw new InterpreterRuntimeException("JsonDeserializeToStruct: value must be an object");
        
        return jsonValue.ToValue();
    }

    [LanguageFunction]
    public static Value DeserializeToStruct(FunctionExecContext ctx, string text, Value value) {
        text = text.Trim();

        if (value is not {Type: RTVT.Struct})
            throw new InterpreterRuntimeException("JsonDeserializeToStruct: value must be a struct");

        if (string.IsNullOrEmpty(text))
            return Value.Null();

        var parser = new JsonParser(text);

        var jsonValue = parser.ParseJsonValue();
        parser.Require(JsonTokenType.End);

        if (jsonValue.Type != JsonValueType.Object)
            throw new InterpreterRuntimeException("JsonDeserializeToStruct: value must be an object");

        return DeserializeToStruct(ctx, value, jsonValue.AsObject);
    }

    public static Value DeserializeToStruct(FunctionExecContext ctx, Value value, JsonObject jsonObj) {
        var structValue = value.As.Struct();
        var proto       = (value.PrototypeType as StructPrototype)!;

        foreach (var member in proto.DeclaredMembers) {
            switch (member.MemberType) {
                case TypeDeclMemberType.Field: {
                    var jsonInfo = member.JsonAttributes.FirstOrDefault();
                    if (jsonInfo == null)
                        continue;

                    var jsonName = jsonInfo.Name;

                    if (jsonObj.TryGetValue(jsonName, out var jsonMemberValue)) {
                        var memberType = member.Declaration.TypeIdentifier.ResolveType();

                        switch (memberType.ValueType.ForType) {
                            case RTVT.Boolean: {
                                if (jsonMemberValue.Value is not JsonBool jsonBoolean) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.Boolean))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a boolean");

                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.Boolean);
                                    break;
                                }

                                structValue[member.Name] = jsonBoolean.ToValue();
                                break;
                            }
                            
                            case RTVT.Int32: {
                                if (jsonMemberValue.Value is not JsonNumber_Int32 jsonInt32) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.Int32))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be an int32");
                                    
                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.Int32);
                                    break;
                                }

                                structValue[member.Name] = jsonInt32.ToValue();
                                break;
                            }

                            case RTVT.Int64: {
                                if (jsonMemberValue.Value is not JsonNumber_Int64 jsonInt64) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.Int64))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be an int64");
                                    
                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.Int64);
                                    break;
                                }
                                
                                structValue[member.Name] = jsonInt64.ToValue();
                                break;
                            }

                            case RTVT.Float: {
                                if (jsonMemberValue.Value is not JsonNumber_Float jsonFloat) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.Float))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a float");
                                    
                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.Float);
                                    break;
                                }

                                structValue[member.Name] = jsonFloat.ToValue();
                                break;
                            }

                            case RTVT.Double: {
                                if (jsonMemberValue.Value is not JsonNumber_Double jsonDouble) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.Double))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a double");
                                    
                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.Double);
                                    break;
                                }

                                structValue[member.Name] = jsonDouble.ToValue();
                                break;
                            }

                            case RTVT.String: {
                                if (jsonMemberValue.Value is not JsonString jsonString) {
                                    if (!memberType.Prototype.CanCastTo(RTVT.String))
                                        throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a string");
                                    
                                    structValue[member.Name] = jsonMemberValue.ToValue().CastTo(RTVT.String);
                                    break;
                                }

                                structValue[member.Name] = jsonString.ToValue();
                                break;
                            }

                            case RTVT.Struct: {

                                if (jsonMemberValue.Value is not JsonObject jsonObject)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a struct");

                                if (memberType.Prototype is not StructPrototype structType)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a struct");

                                var ctor = structType.ValueType.GetConstructorFn();
                                var val  = ctx.Call(ctor, structType.ValueType);

                                var newStruct = DeserializeToStruct(ctx, val, jsonObject);
                                structValue[member.Name] = newStruct;
                                break;
                            }

                            case RTVT.Object: {
                                if (jsonMemberValue.Value is not JsonObject jsonObject)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be an object");

                                structValue[member.Name] = jsonObject.ToValue();
                                break;
                            }

                            case RTVT.Array: {
                                if (jsonMemberValue.Value is not JsonArray jsonArray)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be an array");

                                structValue[member.Name] = jsonArray.ToValue();
                                break;
                            }

                            default:
                                throw new InterpreterRuntimeException("JsonDeserializeToStruct: unsupported member type");
                        }

                        /*switch (jsonMemberValue.Value) {
                            case JsonNull _:
                                structValue[member.Name] = Value.Null();
                                break;
                            case JsonBool jsonBoolean:
                                if (memberType.Prototype is not BooleanPrototype)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a boolean");

                                structValue[member.Name] = jsonBoolean.ToValue();
                                break;
                            case JsonNumber_Int32:
                            case JsonNumber_Int64:
                            case JsonNumber_Float:
                            case JsonNumber_Double:
                                structValue[member.Name] = jsonMemberValue.ToValue();
                                break;
                            case JsonString jsonString:
                                structValue[member.Name] = jsonString.ToValue();
                                break;
                            case JsonObject jsonObject: {
                                if (memberType.Prototype is not StructPrototype structType)
                                    throw new InterpreterRuntimeException("JsonDeserializeToStruct: member type must be a struct");

                                var ctor = structType.ValueType.GetConstructorFn();
                                var val  = ctx.Call(ctor, structType.ValueType);

                                var newStruct = DeserializeToStruct(ctx, val, jsonObject);

                                structValue[member.Name] = newStruct;

                                break;
                            }

                            case JsonArray arr: {
                                structValue[member.Name] = arr.ToValue();
                                break;
                            }

                            default:
                                throw new InterpreterRuntimeException("JsonDeserializeToStruct: unsupported json value type");
                        }*/


                        //structValue[member.Name] = jsonMemberValue.ToValue();
                    }

                    break;
                }

                case TypeDeclMemberType.Method: {
                    break;
                }
            }
        }

        return value;
    }

    [LanguageFunction]
    public static string Serialize(Value value) {
        // using var _ = TimedScope.Scoped_PrintWithCaller();

        var sb = new StringBuilder();

        SerializeImpl(value, sb, 0);

        return sb.ToString();
    }

    private static void SerializeImpl(Value value, StringBuilder sb, int depth) {
        if (depth >= 32)
            throw new InterpreterRuntimeException("JsonSerialize: maximum depth exceeded");

        var first = true;

        switch (value.Type) {
            case RTVT.Boolean:
                sb.Append((bool) value ? "true" : "false");
                break;

            case RTVT.Null:
            case RTVT.Unit:
                sb.Append("null");
                break;

            case RTVT.Int32: {
                var int32 = (int) value;
                sb.Append(int32.ToString(CultureInfo.InvariantCulture));
                break;
            }

            case RTVT.Int64: {
                var int64 = (long) value;
                sb.Append(int64.ToString(CultureInfo.InvariantCulture));
                break;
            }

            case RTVT.Float: {
                var floatVal = (float) value;
                sb.Append(floatVal.ToString(CultureInfo.InvariantCulture));
                break;
            }

            case RTVT.Double: {
                var doubleVal = (double) value;

                if (double.IsNaN(doubleVal))
                    throw new InterpreterRuntimeException("JsonSerialize: Failed to serialize 'NaN'");

                if (double.IsInfinity(doubleVal))
                    throw new InterpreterRuntimeException("JsonSerialize: Failed to serialize 'Infinity'");

                sb.Append(doubleVal.ToString(CultureInfo.InvariantCulture));
                break;
            }

            case RTVT.String:
                SerializeString(value, sb);
                break;

            case RTVT.Struct:
                var structValue = value.As.Struct();
                var proto       = (value.PrototypeType as StructPrototype)!;

                sb.Append('{');

                foreach (var member in proto.DeclaredFields) {
                    var jsonInfo = member.JsonAttributes.FirstOrDefault();
                    if (jsonInfo == null)
                        continue;
                    var jsonName = jsonInfo.Name;

                    var memberValue = structValue[member.Name];

                    if (first)
                        first = false;
                    else
                        sb.Append(',');

                    SerializeImpl(jsonName, sb, depth + 1);

                    sb.Append(':');

                    SerializeImpl(memberValue, sb, depth + 1);
                }

                sb.Append('}');
                break;

            case RTVT.Object:
                sb.Append('{');

                foreach (var kvp in value.As.Object()) {
                    if (kvp.Value == null)
                        continue;

                    if (first)
                        first = false;
                    else
                        sb.Append(',');

                    SerializeImpl(kvp.Key, sb, depth + 1);

                    sb.Append(':');

                    SerializeImpl(kvp.Value, sb, depth + 1);
                }

                sb.Append('}');
                break;

            case RTVT.Array:
                sb.Append('[');

                foreach (var v in value.As.List()) {
                    if (first)
                        first = false;
                    else
                        sb.Append(',');

                    SerializeImpl(v, sb, depth + 1);
                }

                sb.Append(']');
                break;

            default:
                throw new InterpreterRuntimeException("JsonSerialize: Failed to serialize {0}s", value.Type.Name());
        }
    }

    private static void SerializeString(string value, StringBuilder sb) {
        sb.Append('"');

        foreach (var c in value) {
            switch (c) {
                case '\\':
                    sb.Append(@"\\");
                    break;

                case '\"':
                    sb.Append("\\\"");
                    break;

                case '\b':
                    sb.Append("\\b");
                    break;

                case '\f':
                    sb.Append("\\f");
                    break;

                case '\n':
                    sb.Append("\\n");
                    break;

                case '\r':
                    sb.Append("\\r");
                    break;

                case '\t':
                    sb.Append("\\t");
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        sb.Append('"');
    }

}