using System.Reflection;

namespace CSScriptingLang.Utils;

public static class EnumExtensions
{
    public static bool HasAny<EnumType>(this EnumType a, EnumType b) where EnumType : Enum {
        return a.GetTypeCode() switch {
            // byte
            TypeCode.Byte => ((byte) (object) a & (byte) (object) b) != 0,
            // sbyte
            TypeCode.SByte => ((sbyte) (object) a & (sbyte) (object) b) != 0,
            // short
            TypeCode.Int16 => ((short) (object) a & (short) (object) b) != 0,
            // ushort
            TypeCode.UInt16 => ((ushort) (object) a & (ushort) (object) b) != 0,
            // int
            TypeCode.Int32 => ((int) (object) a & (int) (object) b) != 0,
            // uint
            TypeCode.UInt32 => ((uint) (object) a & (uint) (object) b) != 0,
            // long
            TypeCode.Int64 => ((long) (object) a & (long) (object) b) != 0,
            // ulong
            TypeCode.UInt64 => ((ulong) (object) a & (ulong) (object) b) != 0,

            _ => throw new NotSupportedException("Unknown Error. This shouldn't be happened!"),
        };
    }

    public static bool HasAll<EnumType>(this EnumType a, EnumType b) where EnumType : Enum {
        return a.GetTypeCode() switch {
            // byte
            TypeCode.Byte => ((byte) (object) a & (byte) (object) b) == (byte) (object) b,
            // sbyte
            TypeCode.SByte => ((sbyte) (object) a & (sbyte) (object) b) == (sbyte) (object) b,
            // short
            TypeCode.Int16 => ((short) (object) a & (short) (object) b) == (short) (object) b,
            // ushort
            TypeCode.UInt16 => ((ushort) (object) a & (ushort) (object) b) == (ushort) (object) b,
            // int
            TypeCode.Int32 => ((int) (object) a & (int) (object) b) == (int) (object) b,
            // uint
            TypeCode.UInt32 => ((uint) (object) a & (uint) (object) b) == (uint) (object) b,
            // long
            TypeCode.Int64 => ((long) (object) a & (long) (object) b) == (long) (object) b,
            // ulong
            TypeCode.UInt64 => ((ulong) (object) a & (ulong) (object) b) == (ulong) (object) b,

            _ => throw new NotSupportedException("Unknown Error. This shouldn't be happened!"),
        };
    }

    public static IEnumerable<T> GetFlags<T>(this T value) where T : struct, Enum {
        foreach (var flag in Enum.GetValues<T>()) {
            if (value.HasAll(flag))
                yield return flag;
        }
    }

    public static TAttributeType GetEnumAttribute<TAttributeType>(this Enum value) where TAttributeType : Attribute {
        var fieldInfo = value.GetType().GetField(value.ToString());
        if (fieldInfo != null)
            return fieldInfo.GetCustomAttribute<TAttributeType>();
        return null;
    }

    public static T SetFlags<T>(this T value, T flags, bool on) where T : struct, Enum {
        return value.GetTypeCode() switch {
            // byte
            TypeCode.Byte => on
                ? (T) (object) ((byte) (object) value | (byte) (object) flags)
                : (T) (object) ((byte) (object) value & ~(byte) (object) flags),
            // sbyte
            TypeCode.SByte => on
                ? (T) (object) ((sbyte) (object) value | (sbyte) (object) flags)
                : (T) (object) ((sbyte) (object) value & ~(sbyte) (object) flags),
            // short
            TypeCode.Int16 => on
                ? (T) (object) ((short) (object) value | (short) (object) flags)
                : (T) (object) ((short) (object) value & ~(short) (object) flags),
            // ushort
            TypeCode.UInt16 => on
                ? (T) (object) ((ushort) (object) value | (ushort) (object) flags)
                : (T) (object) ((ushort) (object) value & ~(ushort) (object) flags),
            // int
            TypeCode.Int32 => on
                ? (T) (object) ((int) (object) value | (int) (object) flags)
                : (T) (object) ((int) (object) value & ~(int) (object) flags),
            // uint
            TypeCode.UInt32 => on
                ? (T) (object) ((uint) (object) value | (uint) (object) flags)
                : (T) (object) ((uint) (object) value & ~(uint) (object) flags),
            // long
            TypeCode.Int64 => on
                ? (T) (object) ((long) (object) value | (long) (object) flags)
                : (T) (object) ((long) (object) value & ~(long) (object) flags),
            // ulong
            TypeCode.UInt64 => on
                ? (T) (object) ((ulong) (object) value | (ulong) (object) flags)
                : (T) (object) ((ulong) (object) value & ~(ulong) (object) flags),

            _ => throw new NotSupportedException("Unknown Error. This shouldn't be happened!"),
        };


        /*var lValue = Convert.ToInt64(value);
        var lFlag  = Convert.ToInt64(flags);
        if (on) {
            lValue |= lFlag;
        } else {
            lValue &= (~lFlag);
        }

        return (T) Enum.ToObject(typeof(T), lValue);*/
    }

    public static T SetFlags<T>(this T value, T flags) where T : struct, Enum {
        return value.SetFlags(flags, true);
    }

    public static T ClearFlags<T>(this T value, T flags) where T : struct, Enum {
        return value.SetFlags(flags, false);
    }

    public static string GetFlagString<T>(this T value) where T : struct, Enum {
        var flags = value.GetFlags().Select(f => f.ToString());
        return string.Join(" | ", flags);
    }
}