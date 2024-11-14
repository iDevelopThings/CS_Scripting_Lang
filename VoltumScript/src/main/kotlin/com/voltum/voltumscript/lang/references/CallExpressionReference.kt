package com.voltum.voltumscript.lang.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.lang.inference.Inference
import com.voltum.voltumscript.lang.inference.InferenceFlags
import com.voltum.voltumscript.lang.inference.withKindFlags
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.psi.VoltumCallExpr
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumReferenceElement
import com.voltum.voltumscript.psi.ext.inference
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.psi.ext.prototypeNullable
import com.voltum.voltumscript.psi.ext.tryFoldType
import kotlinx.serialization.protobuf.ProtoType

class CallExpressionReference : VoltumReferenceCached<VoltumCallExpr> {
    constructor(el: VoltumCallExpr, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        var proto: Ty? = null

        // in this case, it's simple func call, for ex `foo()` rather than `foo.bar()`
        if (element.path?.qualifier == null) {
            proto = element.getNameId()?.prototype
            val folded = element.tryFoldType(proto)
            if (folded != null && folded.type.linkedElement != null) {
                return listOf(folded.type.linkedElement as VoltumElement)
            }
        } else {

            val qualifierVar = element.path?.qualifier?.lastVarReference
            val inf = qualifierVar?.inference

            val vv = element.path?.reference?.resolve()
            
            val type = qualifierVar?.reference?.resolve()
            val value = inf?.allIdValues?.get(qualifierVar.name!!)

            proto = value?.prototypeNullable
            
//            val member = proto?.getField(element.getNameId()?.text ?: "")
            
            
            return listOfNotNull(proto?.linkedElement as VoltumElement)
        }
        
        return emptyList()
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}