package com.voltum.voltumscript.lang.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.intellij.psi.util.PsiTreeUtil
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumIdentifier
import com.voltum.voltumscript.psi.VoltumPath
import com.voltum.voltumscript.psi.VoltumVarReference
import com.voltum.voltumscript.psi.ext.descendantOfTypeOrSelf
import com.voltum.voltumscript.psi.ext.prototype

class VoltumPathExprReference : VoltumReferenceBase<VoltumPath> {
    constructor(el: VoltumPath, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        val nameId = element.getNameId()
        val qualifierId = element.qualifier?.getNameId()

//        val qualifierReference = qualifierId?.reference?.resolve()
//        val qualifierRefInference = qualifierReference?.inference

        var proto: Ty? = null
        if (qualifierId?.prototype != null) {
            proto = qualifierId.prototype
            val field = proto.getField(nameId?.text!!)
            proto = field?.ty
        }

        if (proto?.linkedToField != null && proto.linkedToField?.name == idElement?.text) {
            val id = proto.linkedToField?.linkedElement?.descendantOfTypeOrSelf<VoltumIdentifier>()!!
//            nameIdInference?.prototypes?.add(id.text, proto.linkedToField?.ty!!)
            return listOf(id)
        } else if (proto?.linkedElement != null) {
            val id = proto.linkedElement as VoltumElement
//            nameIdInference?.addVariable(id , proto.linkedElement as VoltumElement)
            return listOf(id)
        }

        return emptyList()/*
                val ref = element.getNameId()?.reference;
                val inf = element.getNameId()?.inference;
        
                val parts = element.pathParts
                val quals = element.qualifiers
        
                val pathQueue = ArrayDeque<VoltumReferenceElement>()
                for (part in parts) {
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
        
                return out*/
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}