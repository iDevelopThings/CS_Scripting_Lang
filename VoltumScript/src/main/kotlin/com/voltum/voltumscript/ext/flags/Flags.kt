package com.voltum.voltumscript.ext.flags

import java.util.*
import kotlin.experimental.and
import kotlin.reflect.KClass
import kotlin.reflect.KProperty

/**
 * From: https://github.com/justjanne/kotlin-bitflags/tree/main
 * @see com.voltum.voltumscript.ext.flags.BitFlagsTest
 *
 * Example enum:
 * enum class MessageFlagInt(
 *   override val value: Int,
 * ) : Flag<Int> {
 *   Unknown(0x00),
 *   Self(0x01),
 *   Highlight(0x02),
 *   Redirected(0x04),
 *   ServerMsg(0x08),
 *   Backlog(0x10);
 *
 *   companion object : Flags<Int, MessageFlagInt> {
 *     override val all: Set<MessageFlagInt> = values().toEnumSet()
 *   }
 * }
 */
@Suppress("UNCHECKED_CAST", "FINAL_UPPER_BOUND")
class FlagValue<T : Int>(value: Int = 0) : Flag<T> {
    private var _value: T = value as T
    private var onChange: ((T) -> Unit)? = null

    override var value: T
        get() = _value
        set(value) {
            _value = value
            onChange?.invoke(value)
        }

    fun onChange(onChange: (T) -> Unit) {
        this.onChange = onChange
    }

    operator fun getValue(thisRef: Any?, property: KProperty<*>): T {
        return value
    }

    operator fun setValue(thisRef: Any?, property: KProperty<*>, value: T) {
        this.value = value
    }

    fun set(value: T): FlagValue<T> = this.apply { this.value = value }
    fun set(value: FlagValue<T>): FlagValue<T> = this.apply { this.value = value.value }
    fun <U> set(value: U): FlagValue<T> where U : Flag<T>, U : Enum<U> = this.apply { this.value = value.value }
    fun <U> set(value: EnumSet<U>): FlagValue<T> where U : Flag<T>, U : Enum<U> =
        this.apply {
            this.value = value.fold(0) { acc, el -> acc or el.value } as T
        }

    fun get(): T = value
    fun <U> asFlag(): U where U : Flag<T>, U : Enum<U> = value as U

    // operator fun getValue(thisRef: Any?, property: Any?): T = value
    // operator fun setValue(thisRef: Any?, property: Any?, value: T) { this.value = value }
    // operator fun <U> setValue(thisRef: Any?, property: Any?, value: U) { this.value = value as T }

    operator fun plus(other: Int): Int = value + other
    operator fun plusAssign(other: T) {
        value = (value as Int + other as Int) as T
    }

    operator fun <U> plus(other: U): Int where U : Flag<T>, U : Enum<U> = value + other.value
    operator fun <U> plusAssign(other: U) where U : Flag<T>, U : Enum<U> {
        value = (value as Int + other.value) as T
    }

    operator fun minus(other: Int): Int = value - other
    operator fun minusAssign(other: T) {
        value = (value as Int - other as Int) as T
    }

    operator fun <U> minus(other: U): Int where U : Flag<T>, U : Enum<U> = value - other.value
    operator fun <U> minusAssign(other: U) where U : Flag<T>, U : Enum<U> {
        value = (value as Int - other.value) as T
    }

    operator fun times(other: Int): Int = value * other
    operator fun timesAssign(other: T) {
        value = (value as Int * other as Int) as T
    }

    operator fun <U> times(other: U): Int where U : Flag<T>, U : Enum<U> = value * other.value
    operator fun <U> timesAssign(other: U) where U : Flag<T>, U : Enum<U> {
        value = (value as Int * other.value) as T
    }

    operator fun div(other: Int): Int = value / other
    operator fun divAssign(other: T) {
        value = (value as Int / other as Int) as T
    }

    operator fun <U> div(other: U): Int where U : Flag<T>, U : Enum<U> = value / other.value
    operator fun <U> divAssign(other: U) where U : Flag<T>, U : Enum<U> {
        value = (value as Int / other.value) as T
    }

    infix fun shl(bitCount: Int): Int = value shl bitCount
    infix fun shl(bitCount: Flag<T>): Int = value shl bitCount.value
    infix fun <U> shl(bitCount: U): Int where U : Flag<T>, U : Enum<U> = value shl bitCount.value

    infix fun shr(bitCount: Int): Int = value shr bitCount
    infix fun shr(bitCount: Flag<T>): Int = value shr bitCount.value
    infix fun <U> shr(bitCount: U): Int where U : Flag<T>, U : Enum<U> = value shr bitCount.value

    infix fun ushr(bitCount: Int): Int = value ushr bitCount
    infix fun ushr(bitCount: Flag<T>): Int = value ushr bitCount.value
    infix fun <U> ushr(bitCount: U): Int where U : Flag<T>, U : Enum<U> = value ushr bitCount.value

    infix fun and(other: Int): Int = value and other
    infix fun and(other: Flag<T>): Int = value and other.value
    infix fun <U> and(other: U): Int where U : Flag<T>, U : Enum<U> = value and other.value

    infix fun or(other: Int): Int = value or other
    infix fun or(other: Flag<T>): Int = value or other.value
    infix fun <U> or(other: U): Int where U : Flag<T>, U : Enum<U> = value or other.value

    infix fun xor(other: Int): Int = value xor other
    infix fun xor(other: Flag<T>): Int = value xor other.value
    infix fun <U> xor(other: U): Int where U : Flag<T>, U : Enum<U> = value xor other.value

    operator fun contains(other: Int): Boolean = value == other
    operator fun compareTo(other: Int): Int = value.compareTo(other)

    operator fun unaryPlus(): Int = +value
    operator fun unaryMinus(): Int = -value


    override fun toString(): String = value.toString()

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is FlagValue<*>) return false
        if (_value != other._value) return false
        return true
    }

    override fun hashCode(): Int = _value.hashCode()

    fun copy(): FlagValue<T> = FlagValue(value as Int)

    // `FlagA |= FlagB` to add a flag
    fun add(other: T): T {
        value = (value as Int or other as Int) as T
        return value
    }

    fun <U> add(other: U): T where U : Flag<T>, U : Enum<U> {
        value = (value as Int or other.value) as T
        return value
    }

    // `FlagA &= ~FlagB` to remove a flag
    fun remove(other: T): T {
        value = (value as Int and (other as Int).inv()) as T
        return value
    }

    fun <U> remove(other: U): T where U : Flag<T>, U : Enum<U> {
        value = (value as Int and other.value.inv()) as T
        return value
    }

    // `FlagA & FlagB` to check if a flag is set
    fun has(other: T): Boolean = (value as Int and other as Int) != 0
    fun has(other: Flag<T>): Boolean = (value as Int and other.value) != 0
    fun <U> has(other: U): Boolean where U : Flag<T>, U : Enum<U> = (value as Int and other.value) != 0

    // `FlagA & ~FlagB` to check if a flag is not set
    fun hasNot(other: T): Boolean = (value as Int and other as Int) == 0
    fun hasNot(other: Flag<T>): Boolean = (value as Int and other.value) == 0
    fun <U> hasNot(other: U): Boolean where U : Flag<T>, U : Enum<U> = (value as Int and other.value) == 0

    // `FlagA & FlagB == FlagB` to check if a flag is set
    fun isSet(other: T): Boolean = (value as Int and other as Int) == other as Int
    fun isSet(other: Flag<T>): Boolean = (value as Int and other.value) == other.value
    fun isSet(other: FlagValue<T>): Boolean = (value as Int and other.value as Int) == other.value as Int
    fun <U> isSet(other: U): Boolean where U : Flag<T>, U : Enum<U> = (value as Int and other.value) == other.value

    // `FlagA & FlagB == 0` to check if a flag is not set
    fun isNotSet(other: T): Boolean = (value as Int and other as Int) == 0
    fun isNotSet(other: Flag<T>): Boolean = (value as Int and other.value) == 0
    fun isNotSet(other: FlagValue<T>): Boolean = (value as Int and other.value as Int) == 0
    fun <U> isNotSet(other: U): Boolean where U : Flag<T>, U : Enum<U> = (value as Int and other.value) == 0


}

class EnumFlagValue<TEnum>(value: Int = 0, val enumInst: KClass<TEnum>)
        where TEnum : Enum<TEnum>,
              TEnum : Flag<Int> {

    private var _value: Int = value
    private var onChange: ((Int) -> Unit)? = null

    var value: Int
        get() = _value
        set(value) {
            _value = value
            onChange?.invoke(value)
        }

    override fun toString(): String {
        // using `TEnum` create a flag string `FlagA | FlagB | FlagC`
        return enumInst.java.enumConstants
            .filter { (value and it.value) != 0 }
            .joinToString(" | ") { it.name }
    }

    fun onChange(onChange: (Int) -> Unit) {
        this.onChange = onChange
    }

    operator fun getValue(thisRef: Any?, property: KProperty<*>): Int {
        return value
    }

    operator fun setValue(thisRef: Any?, property: KProperty<*>, value: Int) {
        this.value = value
    }

    fun set(value: Int): EnumFlagValue<TEnum> = this.apply { this.value = value }
    fun set(value: EnumFlagValue<TEnum>): EnumFlagValue<TEnum> = this.apply { this.value = value.value }
    fun set(value: Flag<Int>): EnumFlagValue<TEnum> = this.apply { this.value = value.value }

    fun <U> set(value: U): EnumFlagValue<TEnum> where U : Flag<Int>, U : Enum<U> = this.apply { this.value = value.value }
    fun <U> set(value: EnumSet<U>): EnumFlagValue<TEnum> where U : Flag<Int>, U : Enum<U> =
        this.apply {
            this.value = value.fold(0) { acc, el -> acc or el.value }
        }

    fun asInt(): Int = value

    operator fun plus(other: Int): Int = value + other
    operator fun plus(other: Flag<Int>): Int = value + other.asInt()
    operator fun plusAssign(other: Int) {
        value = (value + other)
    }

    operator fun plusAssign(other: Flag<Int>) {
        value = (value + other.asInt())
    }

    operator fun minus(other: Int): Int = value - other
    operator fun minus(other: Flag<Int>): Int = value - other.asInt()
    operator fun minusAssign(other: Int) {
        value = (value - other)
    }

    operator fun minusAssign(other: Flag<Int>) {
        value = (value - other.asInt())
    }

    operator fun times(other: Int): Int = value * other
    operator fun times(other: Flag<Int>): Int = value * other.asInt()
    operator fun timesAssign(other: Int) {
        value = (value * other)
    }

    operator fun timesAssign(other: Flag<Int>) {
        value = (value * other.asInt())
    }

    operator fun div(other: Int): Int = value / other
    operator fun div(other: Flag<Int>): Int = value / other.asInt()
    operator fun divAssign(other: Int) {
        value = (value / other)
    }

    operator fun divAssign(other: Flag<Int>) {
        value = (value / other.asInt())
    }

    infix fun shl(bitCount: Int): Int = value shl bitCount
    infix fun shl(bitCount: Flag<Int>): Int = value shl bitCount.value

    infix fun shr(bitCount: Int): Int = value shr bitCount
    infix fun shr(bitCount: Flag<Int>): Int = value shr bitCount.value

    infix fun ushr(bitCount: Int): Int = value ushr bitCount
    infix fun ushr(bitCount: Flag<Int>): Int = value ushr bitCount.value

    infix fun and(other: Int): Int = value and other
    infix fun and(other: Flag<Int>): Int = value and other.value

    infix fun or(other: Int): Int = value or other
    infix fun or(other: Flag<Int>): Int = value or other.value

    infix fun xor(other: Int): Int = value xor other
    infix fun xor(other: Flag<Int>): Int = value xor other.value

    operator fun contains(other: Int): Boolean = value == other
    operator fun contains(other: Flag<Int>): Boolean = value == other.value

    operator fun compareTo(other: Int): Int = value.compareTo(other)
    operator fun compareTo(other: Flag<Int>): Int = value.compareTo(other.value)

    operator fun unaryPlus(): Int = +value
    operator fun unaryMinus(): Int = -value


    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other is EnumFlagValue<*>)
            return _value == other._value
        if (other is Int)
            return _value == other
        return false
    }

    override fun hashCode(): Int = _value.hashCode()

    // `FlagA |= FlagB` to add a flag
    fun add(other: Int): Int {
        value = (value or other)
        return value
    }

    fun <U> add(other: U): Int where U : Flag<Int>, U : Enum<U> {
        value = (value or other.value)
        return value
    }

    // `FlagA &= ~FlagB` to remove a flag
    fun remove(other: Int): Int {
        value = (value and (other).inv())
        return value
    }

    fun <U> remove(other: U): Int where U : Flag<Int>, U : Enum<U> {
        value = (value and other.value.inv())
        return value
    }

    // `FlagA & FlagB` to check if a flag is set
    fun has(other: Int): Boolean = (value and other) != 0
    fun has(other: Flag<Int>): Boolean = (value and other.value) != 0
    fun <U> has(other: U): Boolean where U : Flag<Int>, U : Enum<U> = (value and other.value) != 0

    // `FlagA & ~FlagB` to check if a flag is not set
    fun hasNot(other: Int): Boolean = (value and other) == 0
    fun hasNot(other: Flag<Int>): Boolean = (value and other.value) == 0
    fun <U> hasNot(other: U): Boolean where U : Flag<Int>, U : Enum<U> = (value and other.value) == 0

    // `FlagA & FlagB == FlagB` to check if a flag is set
    fun isSet(other: Int): Boolean = (value and other) == other
    fun isSet(other: Flag<Int>): Boolean = (value and other.value) == other.value
    fun isSet(other: EnumFlagValue<TEnum>): Boolean = (value and other.value) == other.value
    fun <U> isSet(other: U): Boolean where U : Flag<Int>, U : Enum<U> = (value and other.value) == other.value

    // `FlagA & FlagB == 0` to check if a flag is not set
    fun isNotSet(other: Int): Boolean = (value and other) == 0
    fun isNotSet(other: Flag<Int>): Boolean = (value and other.value) == 0
    fun isNotSet(other: EnumFlagValue<TEnum>): Boolean = (value and other.value) == 0
    fun <U> isNotSet(other: U): Boolean where U : Flag<Int>, U : Enum<U> = (value and other.value) == 0


}

class EnumFlagValueProxy<TEnum>(
    val value: EnumFlagValue<TEnum>,
    val flag: TEnum
)
        where TEnum : Enum<TEnum>,
              TEnum : Flag<Int> {
    operator fun getValue(thisRef: Any?, property: KProperty<*>): Boolean {
        return value.has(flag)
    }

    operator fun setValue(thisRef: Any?, property: KProperty<*>, value: Boolean) {
        if (value) this.value.add(flag)
        else this.value.remove(flag)
    }
}

/**
 * Interface for a single flag which can be part of a bitfield
 */
interface Flag<T> {
    /**
     * Binary value of the flag
     */
    val value: T

    fun asInt(): Int = value as Int
}

/**
 * Interface for a helper object for a type of flag
 */
interface Flags<T, U> where U : Flag<T>, U : Enum<U> {
    /**
     * Predefined set with all possible flag values
     */
    val all: Set<U>
}

// convert the `flags` to `EnumFlagValue`
inline fun <reified T> Flags<*, T>.toEnumFlagValue(flags: Int = 0): EnumFlagValue<T>
        where T : Flag<Int>, T : Enum<T> = EnumFlagValue(flags, T::class)

// convert the `EnumSet` to `EnumFlagValue`
inline fun <reified T> Set<T>.toEnumFlagValue(): EnumFlagValue<T>
        where T : Flag<Int>, T : Enum<T> =
    EnumFlagValue(this.fold(0) { acc, el -> acc or el.value }, T::class)


/**
 * Function to obtain an empty bitfield for a certain flag type
 * @return empty bitfield
 */
@Suppress("unused")
inline fun <reified T> Flags<*, T>.none(): EnumSet<T>
        where T : Flag<*>, T : Enum<T> = EnumSet.noneOf(T::class.java)


/**
 * Construct a bitfield out of discrete flags
 * @return bitfield
 */
@Suppress("unused")
inline fun <reified T> Flags<*, T>.of(vararg values: T): EnumSet<T>
        where T : Flag<*>, T : Enum<T> = values.toEnumSet()

/**
 * Construct a bitfield out of a collection of flags
 * @return bitfield
 */
@Suppress("unused")
inline fun <reified T> Flags<*, T>.of(values: Collection<T>): EnumSet<T>
        where T : Flag<*>, T : Enum<T> = values.toEnumSet()

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
inline fun <reified T> Flags<Byte, T>.of(value: Byte?): EnumSet<T> where T : Flag<Byte>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0.toByte() }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
@ExperimentalUnsignedTypes
inline fun <reified T> Flags<UByte, T>.of(value: UByte?): EnumSet<T> where T : Flag<UByte>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0.toUByte() }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
inline fun <reified T> Flags<Short, T>.of(value: Short?): EnumSet<T> where T : Flag<Short>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0.toShort() }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
@ExperimentalUnsignedTypes
inline fun <reified T> Flags<UShort, T>.of(value: UShort?): EnumSet<T> where T : Flag<UShort>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0.toUShort() }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
inline fun <reified T> Flags<Int, T>.of(value: Int?): EnumSet<T> where T : Flag<Int>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0 }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
@ExperimentalUnsignedTypes
inline fun <reified T> Flags<UInt, T>.of(value: UInt?): EnumSet<T> where T : Flag<UInt>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0u }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
inline fun <reified T> Flags<Long, T>.of(value: Long?): EnumSet<T> where T : Flag<Long>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0L }.toEnumSet()
}

/**
 * Construct a bitfield out of a binary value
 * @return bitfield
 */
@ExperimentalUnsignedTypes
inline fun <reified T> Flags<ULong, T>.of(value: ULong?): EnumSet<T> where T : Flag<ULong>, T : Enum<T> {
    if (value == null) return emptyList<T>().toEnumSet()
    return all.filter { (value and it.value) != 0uL }.toEnumSet()
}
