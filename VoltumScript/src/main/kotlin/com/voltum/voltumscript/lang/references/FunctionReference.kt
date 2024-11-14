package com.voltum.voltumscript.lang.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.lang.inference.Inference
import com.voltum.voltumscript.lang.inference.InferenceFlags
import com.voltum.voltumscript.lang.inference.withKindFlags
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumReferenceElement

class FunctionReference : VoltumReferenceCached<VoltumReferenceElement> {
    constructor(el: VoltumReferenceElement, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        return Inference.infer(element)
            .withKindFlags(InferenceFlags.function())
            .resolve()
            .allIds
            .toList()
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}