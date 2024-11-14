package com.voltum.voltumscript.lang.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.selfInferenceResult

//class VariableReference : VoltumReferenceCached<VoltumVarReference> {
class VariableReference : VoltumReferenceBase<VoltumVarReference> {
    constructor(el: VoltumVarReference, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        val inference = element.selfInferenceResult
        if (inference == null) {
            return emptyList()
        }

        val named = inference.elementsNamed(idElement?.text!!).map {
            if (it is VoltumTypeRef) {
                it.parentOfType<VoltumIdentifierWithType>()?.nameIdentifier ?: it
            } else {
                it
            }
        }.toList()

        return named
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}

class VariableDeclarationValueReference : VoltumReferenceBase<VoltumVariableDeclaration> {
    constructor(el: VoltumVariableDeclaration, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        if (element.initializer == null)
            throw IllegalStateException("VariableDeclarationValueReference: Initializer is null")

        val initializer = element.initializer
        val result = initializer?.reference?.resolve() as? VoltumElement ?: return emptyList()

        if(result is VoltumNamedElement) {
            return listOf(result.getNameId() ?: result)
        }
        
        return listOf(result)
    }

    override fun calculateDefaultRangeInElement(): TextRange {
        return element.referenceTextRange
    }
}


/*class MemberAccessExprReference : VoltumReferenceBase<VoltumAccessExpr> {
    constructor(el: VoltumAccessExpr, idEl: PsiElement?) : super(el, idEl)

    override fun multiResolve(): List<VoltumElement> {
        return resolveInner()
    }

    fun resolveInner(): List<VoltumElement> {
        val pathQueue = ArrayDeque<VoltumReferenceElement>()
        for (part in element.pathParts) {
            pathQueue.add(part as VoltumReferenceElement)
            if (part == idElement)
                break
        }

        val results = mutableListOf<Pair<VoltumReferenceElement, CachedInferenceResult?>>()

        var prevType: Ty?
        var currentType: Ty? = null
        fun resolveCurrent(): Pair<VoltumReferenceElement, CachedInferenceResult?> {
            val current = pathQueue.first()
            pathQueue.removeFirst()

            val inferenceResult = current.inference

            prevType = currentType
            currentType = current.prototype

            if (prevType != null) {
                val field = prevType?.getField(current.name!!)
                if (field != null) {
                    val result = CachedInferenceResult().apply { from = current }
                    result.prototypes.add(current.name!!, field.ty!!)
                    currentType = field.ty
                    return Pair(current, result)
                }
            }
            return Pair(current, inferenceResult)
        }

        while (pathQueue.isNotEmpty()) {
            val (el, result) = resolveCurrent()
            results.add(Pair(el, result))
        }

        val out = results.map { pair ->
            pair.second?.prototypes?.all?.mapNotNull {
                it.linkedElement as? VoltumElement
            } ?: emptyList()
        }.flatten()

        return out
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}*/
/*
class PartialMemberAccessExprReference : VoltumReferenceBase<VoltumVarReference> {
    constructor(
        el: VoltumVarReference,
        idEl: PsiElement?
    ) : super(el, idEl)

    val accessExpr = element.parent as VoltumAccessExpr

    override fun multiResolve(): List<VoltumElement> {
        return resolveInner()
    }
    
    fun resolveInner(): List<VoltumElement> {
        val pathQueue = ArrayDeque<VoltumReferenceElement>()
        for (part in element.pathParts) {
            pathQueue.add(part as VoltumReferenceElement)
            if (part == idElement)
                break
        }

        val results = mutableListOf<Pair<VoltumReferenceElement, CachedInferenceResult?>>()

        var prevType: Ty?
        var currentType: Ty? = null
        fun resolveCurrent(): Pair<VoltumReferenceElement, CachedInferenceResult?> {
            val current = pathQueue.first()
            pathQueue.removeFirst()

            val inferenceResult = current.inference

            prevType = currentType
            currentType = current.prototype

            if (prevType != null) {
                val field = prevType?.getField(current.name!!)
                if (field != null) {
                    val result = CachedInferenceResult().apply { from = current }
                    result.prototypes.add(current.name!!, field.ty!!)
                    currentType = field.ty
                    return Pair(current, result)
                }
            }
            return Pair(current, inferenceResult)
        }

        while (pathQueue.isNotEmpty()) {
            val (el, result) = resolveCurrent()
            results.add(Pair(el, result))
        }

        val out = results.map { pair ->
            pair.second?.prototypes?.all?.mapNotNull {
                if (it.linkedToField != null && it.linkedToField?.name == idElement?.text) {
                    it.linkedToField?.linkedElement?.descendantOfTypeOrSelf<VoltumIdentifier>()
                } else {
                    */
/* val idEl = it.linkedElement?.descendantOfTypeOrSelf<VoltumIdentifier>()
                     if(idEl?.text == idElement?.text) {
                         idEl
                     } else {
                         it.linkedElement as? VoltumElement
                     }*//*

                    it.linkedElement as? VoltumElement
                }
            } ?: emptyList()
        }.flatten()

        if (out.isEmpty()) {
            log.warn("PartialMemberAccessExprReference: No results found for '${element.pathParts.joinToString(".") { it.text }}'")
            return emptyList()
        }

        return mutableListOf(out.reversed().first())
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}*/
