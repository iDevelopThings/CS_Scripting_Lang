package com.voltum.voltumscript.ext

@Suppress("UNCHECKED_CAST")
abstract class BitFlagInstanceBuilder<T> private constructor(
    private val limit: Limit, 
    startFromBit: Int
) {
    protected constructor(limit: Limit) : this(limit, 0)
    protected constructor(prevBuilder: BitFlagInstanceBuilder<T>, limit: Limit) : this(limit, prevBuilder.counter) {
        values.addAll(prevBuilder.values)
        valueTable.putAll(prevBuilder.valueTable)
    }

    data class BitFlag(val value: Int, val name: String) {
        override fun toString(): String {
            return "BitFlag(value=$value, name='$name')"
        }
    }

    var valueTable = mutableMapOf<String, BitFlag>()
    var values = mutableListOf<BitFlag>()
    val all get() : Int = values.fold(0) { acc, bitFlag -> acc or bitFlag.value }

    private var counter: Int = startFromBit

    protected fun next(name: String? = null): T {
        val nextBit = counter++
        if (nextBit == limit.bits) error("Bitmask index out of $limit limit!")
        return makeBitMask(nextBit, name)
    }

    private fun makeBitMask(bit: Int, name: String?): T {
        val nextBit = 1 shl bit
        values.add(BitFlag(nextBit, name ?: "BIT_MASK_$bit"))
        valueTable[name ?: "BIT_MASK_$bit"] = BitFlag(nextBit, name ?: "BIT_MASK_$bit")

        return nextBit as T
    }

    fun hasFlag(flags: Int, mask: Int): Boolean = (flags and mask) == mask
    fun hasFlag(flags: Int, mask: String): Boolean = (flags and valueTable[mask]?.value!!) == valueTable[mask]?.value

    fun addFlag(flags: Int, mask: Int): Int = flags or mask
    fun addFlag(flags: Int, mask: String): Int = flags or valueTable[mask]?.value!!

    fun removeFlag(flags: Int, mask: Int): Int = flags and mask.inv()
    fun removeFlag(flags: Int, mask: String): Int = flags and valueTable[mask]?.value!!.inv()

    fun dump(value: Int): String {
        return values.filter { hasFlag(value, it.value) }.joinToString(" | ") { it.name }
    }


    protected enum class Limit(val bits: Int) {
        BYTE(8), INT(32)
    }
}