package com.voltum.voltumscript.ext

import com.intellij.util.BitUtil


fun makeBitMask(bitToSet: Int): Int = 1 shl bitToSet



fun Int.setFlag(flag: Int, mode: Boolean): Int = BitUtil.set(this, flag, mode)
fun Int.isSet(flag: Int): Boolean = this.hasFlag(flag)
fun Int.isSet(flag: Int, mode: Boolean): Boolean = this.hasFlag(flag, mode)
fun Int.hasFlag(flag: Int): Boolean = BitUtil.isSet(this, flag)
fun Int.hasFlag(flag: Int, mode: Boolean): Boolean = if (mode) BitUtil.isSet(this, flag) else !BitUtil.isSet(this, flag)
fun Int.removeFlag(flag: Int): Int = BitUtil.set(this, flag, false)
fun Int.toggleFlag(flag: Int): Int = BitUtil.set(this, flag, !BitUtil.isSet(this, flag))
fun Int.combineFlags(vararg flags: Int): Int = flags.fold(this) { acc, flag -> acc or flag }

@Suppress("FINAL_UPPER_BOUND", "UNCHECKED_CAST")
fun <T : Int> mergeFlags(kinds: Collection<T>): T =
    kinds.fold(0) { a, b -> a or b } as T

/**
 * A simple utility for bitmask creation. Use it like this:
 *
 * ```
 * object Foo: BitFlagsBuilder(Limit.BYTE) {
 *     val BIT_MASK_0 = nextBitMask() // Equivalent to `1 shl 0`
 *     val BIT_MASK_1 = nextBitMask() // Equivalent to `1 shl 1`
 *     val BIT_MASK_2 = nextBitMask() // Equivalent to `1 shl 2`
 *     // ...etc
 * }
 * ```
 */
abstract class BitFlagsBuilder private constructor(private val limit: Limit, startFromBit: Int) {
    protected constructor(limit: Limit) : this(limit, 0)
    protected constructor(prevBuilder: BitFlagsBuilder, limit: Limit) : this(limit, prevBuilder.counter)

    private var counter: Int = startFromBit

    protected fun nextBitMask(): Int {
        val nextBit = counter++
        if (nextBit == limit.bits) error("Bitmask index out of $limit limit!")
        return makeBitMask(nextBit)
    }

    private fun makeBitMask(bit: Int): Int = 1 shl bit

    fun hasFlag(flags: Int, mask: Int): Boolean = (flags and mask) == mask
    fun addFlag(flags: Int, mask: Int): Int = flags or mask
    fun removeFlag(flags: Int, mask: Int): Int = flags and mask.inv()

    protected enum class Limit(val bits: Int) {
        BYTE(8), INT(32)
    }
}


open class BitFlagSet<T : BitFlagsBuilder>(var flags: Int, private val flagBuilder: T) {
    fun hasFlag(mask: Int): Boolean = flagBuilder.hasFlag(flags, mask)
    fun setFlag(mask: Int, value: Boolean) {
        flags = if (value) {
            flagBuilder.addFlag(flags, mask)
        } else {
            flagBuilder.removeFlag(flags, mask)
        }
    }

    fun addFlag(mask: Int) {
        flags = flagBuilder.addFlag(flags, mask)
    }

    fun removeFlag(mask: Int) {
        flags = flagBuilder.removeFlag(flags, mask)
    }

    override fun toString(): String {
        return "BitFlagSet(flags=$flags)"
    }
}
//inline fun <reified T : BitFlagsBuilder> T.empty(flagBuilder: T) = BitFlagSet(0, flagBuilder)
//inline fun <reified T : BitFlagsBuilder> T.all(flagBuilder: T, allFlags: Int) = BitFlagSet(allFlags, flagBuilder)

class FlagProxy<T : BitFlagsBuilder>(private val flags: BitFlagSet<T>) {
    operator fun getValue(thisRef: Any?, property: Any?): Boolean = flags.hasFlag(property as Int)
    operator fun setValue(thisRef: Any?, property: Any?, value: Boolean) = flags.setFlag(property as Int, value)
}
/*

object InferenceKindFlags : BitFlagsBuilder(Limit.INT) {
    val VARIABLE = nextBitMask()
    val FUNCTION = nextBitMask()
    val TYPE_DECLARATION = nextBitMask()

    val ALL = VARIABLE or FUNCTION or TYPE_DECLARATION
}

class InferenceFlags(flags: Int?) : BitFlagSet<InferenceKindFlags>(flags ?: 0, InferenceKindFlags) {
    var variable by FlagProxy(this)
    var function by FlagProxy(this)
    var typeDeclaration by FlagProxy(this)
}

fun main() {
    val flags = InferenceFlags(0)
    flags.variable = true
    flags.function = true
    flags.typeDeclaration = true
    println(flags)
}
*/
