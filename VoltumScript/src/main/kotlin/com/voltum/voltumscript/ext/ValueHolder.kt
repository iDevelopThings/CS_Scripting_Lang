package com.voltum.voltumscript.ext

import com.voltum.voltumscript.psi.VoltumNamedElement
import kotlin.contracts.ExperimentalContracts
import kotlin.contracts.InvocationKind
import kotlin.contracts.contract

@Suppress("UNCHECKED_CAST")
open class ValueHolder<TKey, TValue> {
    val values = mutableMapOf<TKey, TValue>()
    val all get() = values.values
    val first get() = values.values.firstOrNull()

    operator fun get(key: TKey): TValue? = values[key]
    
    open fun add(key: TKey, value: TValue) {
        values[key] = value

        onAdded?.invoke(value)
    }

    fun add(value: VoltumNamedElement) {
        if (value.name != null)
            add(value.name!! as TKey, value as TValue)
    }

    inline fun <reified T : VoltumNamedElement> addIfType(value: VoltumNamedElement): Boolean {
        if (value is T) {
            add(value)
            return true
        }
        return false
    }


    operator fun plus(other: ValueHolder<TKey, TValue>): ValueHolder<TKey, TValue> {
        val result = ValueHolder<TKey, TValue>()
        result.values.putAll(values)
        result.values.putAll(other.values)
        return result
    }

    fun clear() {
        values.clear()
    }


    var onAdded: ((TValue) -> Unit)? = null
}

@OptIn(ExperimentalContracts::class)
fun <T : ValueHolder<TKey, TValue>, TKey, TValue> T.onAdded(action: ((TValue) -> Unit)): T {
    contract {
        callsInPlace(action, InvocationKind.EXACTLY_ONCE)
    }
    onAdded = action
    return this
}

open class ValueHolderString<TValue> : ValueHolder<String, TValue>() {

}

