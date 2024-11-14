package com.voltum.voltumscript.lang.inference

import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.ext.ValueHolder
import com.voltum.voltumscript.lang.IVoltumProcessor
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.psi.ext.prototypeNullable

class ValueResult<TKey, TValue>(
    private val owner: CachedInferenceResult,
    private val addToParent: Boolean = true,
    private val debugName: String? = null
) : ValueHolder<TKey, TValue>() {
    override fun add(key: TKey, value: TValue) {
        values[key] = value
        if (addToParent) {
            owner.all.add(value as PsiElement)
            if (value is VoltumNamedElement) {
                owner.named.add(value)
                if (value.getNameId() is VoltumElement && value.getNameId() != null)
                    owner.allIds.add(value.getNameId()!!)
            }
        }
    }

    override fun toString(): String {
        return "${debugName ?: "ValueResult"}: values(${values.size}) = ${values.map { it.key to it.value }.joinToString { it.toString() }}"
    }
}

open class CachedInferenceResult {
    val log = thisLogger()

    open var from: VoltumElement? = null

    val all = HashSet<PsiElement>()
    val allIds = HashSet<VoltumElement>()

    val allIdValues : Map<String, VoltumExpr?> get() = allIds.map {
        return@map Pair(it.text!!, variableValues[it.text!!])
    }.toMap()
    
    val named = ValueResult<String, VoltumNamedElement>(this, false, "Named")

    val functions = ValueResult<String, VoltumFunction>(this, true, "Functions")
    val variables = ValueResult<String, VoltumVariableDeclaration>(this, true, "Variables")
    val typedVariables = ValueResult<String, VoltumTypeRef>(this, true, "TypedVariables")

    /**
     * since variables can be `var a = b` or `var (a, b) = (1, 2)` we'll store results for both, for ex
     * `var a = b` will insert {a -> b} into the map
     * `var (a, b) = (1, 2)` will insert {a -> 1, b -> 2} into the map
     */
    val variableValues = ValueResult<String, VoltumExpr>(this, false, "VariableValues")
    val types = ValueResult<String, VoltumTypeDeclaration>(this, true, "Types")

    val prototypes = ValueResult<String, Ty>(this, false, "Prototypes")

    fun addTypeDeclaration(type: VoltumTypeDeclaration) {
        types.add(type.typeId.text, type)
        prototypes.add(type.typeId.text, type.prototype)
    }

    fun addVariable(id: VoltumVarId, value: VoltumExpr) {
        variableValues.add(id.text, value)

        var checkValue = value
//        if (checkValue is VoltumLiteralExpr) {
//            checkValue = checkValue.firstChild as VoltumExpr
//        }
        
        if(checkValue is VoltumCallExpr) {
            prototypes.add(id.text, checkValue.prototype)
        } else if (checkValue is VoltumValueTypeElement) {
            prototypes.add(id.text, checkValue.prototype)
        }

    }

    fun addVariable(varDecl: VoltumVariableDeclaration) {
        variables.add(varDecl)
        varDecl.varIdAndValueList.forEach {
            addVariable(it.first, it.second)
        }
    }

    fun addTypedVariable(varDecl: VoltumIdentifierWithType) {
        typedVariables.add(varDecl.nameIdentifier.text, varDecl.type)
        prototypes.add(varDecl.nameIdentifier.text, varDecl.type.prototype)
    }

    fun addFunction(result: VoltumFunction) {
        functions.add(result)
        result.getArguments().forEach {
            addTypedVariable(it)
        }
        result.prototypeNullable?.let {
            prototypes.add(result.name, it)
        }
    }

    fun elementsNamed(name: String): Sequence<VoltumNamedElement> = sequence {
        yieldAll(
            typedVariables.values
                .filter { it.key == name }.values
                .map { named.values[it.nameIdentifier?.text] }
                .filterNotNull()
        )
        yieldAll(variables.values.filter { it.key == name }.values)
        yieldAll(functions.values.filter { it.key == name }.values)
    }

    companion object {
        @JvmStatic
        val EMPTY = CachedInferenceResult()
    }

    override fun toString(): String {
        return "CachedInferenceResult: " +
                "all(${all.size}), " +
                "named(${named.values.size}), " +
                "functions(${functions.values.size}), " +
                "variables(${variables.values.size}), " +
                "types(${types.values.size}), " +
                "prototypes(${prototypes.values.size}), " +
                "variableValues(${variableValues.values.size})"
    }

    fun merge(other: CachedInferenceResult?) {
        if (other == null) return

        all.addAll(other.all)
        allIds.addAll(other.allIds)

        named.values.putAll(other.named.values)
        functions.values.putAll(other.functions.values)
        variables.values.putAll(other.variables.values)
        typedVariables.values.putAll(other.typedVariables.values)
        variableValues.values.putAll(other.variableValues.values)
        types.values.putAll(other.types.values)
        prototypes.values.putAll(other.prototypes.values)
    }


}