package com.voltum.voltumscript.lang.inference

import com.intellij.util.BitUtil
import com.voltum.voltumscript.ext.BitFlagsBuilder

object InferenceKindFlags : BitFlagsBuilder(Limit.INT) {
    val VARIABLE = nextBitMask()
    val FUNCTION = nextBitMask()
    val TYPE_DECLARATION = nextBitMask()

    val ALL = VARIABLE or FUNCTION or TYPE_DECLARATION
}

class InferenceFlags(flags: Int?) {
    val flags = flags ?: 0

    companion object {
        fun empty() = InferenceFlags(0)
        fun all() = InferenceFlags(InferenceKindFlags.ALL)

        fun variable() = InferenceFlags(InferenceKindFlags.VARIABLE)
        fun function() = InferenceFlags(InferenceKindFlags.FUNCTION)
        fun typeDeclaration() = InferenceFlags(InferenceKindFlags.TYPE_DECLARATION)
    }

    var variable
        get() = BitUtil.isSet(flags, InferenceKindFlags.VARIABLE)
        set(value) {
            BitUtil.set(flags, InferenceKindFlags.VARIABLE, value)
        }

    var function
        get() = BitUtil.isSet(flags, InferenceKindFlags.FUNCTION)
        set(value) {
            BitUtil.set(flags, InferenceKindFlags.FUNCTION, value)
        }

    var typeDeclaration
        get() = BitUtil.isSet(flags, InferenceKindFlags.TYPE_DECLARATION)
        set(value) {
            BitUtil.set(flags, InferenceKindFlags.TYPE_DECLARATION, value)
        }

    override fun toString(): String {
        return "InferenceFlags(flags=$flags, variable=$variable, function=$function, typeDeclaration=$typeDeclaration)"
    }
}