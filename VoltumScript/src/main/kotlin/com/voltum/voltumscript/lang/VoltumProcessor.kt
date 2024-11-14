package com.voltum.voltumscript.lang

import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.util.SmartList
import kotlin.contracts.ExperimentalContracts
import kotlin.contracts.InvocationKind
import kotlin.contracts.contract

interface IVoltumProcessor {
    fun execute(element: PsiElement, state: ResolveState): Boolean
    fun <T> getFirstResult(): T?
    fun <T> getResults(): SmartList<T>
}

@Suppress("UNCHECKED_CAST")
abstract class VoltumProcessor<T> : PsiScopeProcessor, IVoltumProcessor {
    var name: String 
    var fromElement: PsiElement 
    
    var result: SmartList<T> = SmartList()
    private var onMatch: ((T) -> Unit)? = null

    constructor(name: String, fromElement: PsiElement) {
        this.name = name
        this.fromElement = fromElement
    }
    
    fun addResult(element: T) {
        result.add(element)
        onMatch?.invoke(element)
    }

    override fun <T> getFirstResult(): T? {
        return result.firstOrNull() as? T
    }

    override fun <T> getResults(): SmartList<T> {
        return result as SmartList<T>
    }

    @OptIn(ExperimentalContracts::class)
    fun onMatch(action: ((T) -> Unit)): VoltumProcessor<T> {
        contract {
            callsInPlace(action, InvocationKind.EXACTLY_ONCE)
        }
        onMatch = action
        return this
    }
}
