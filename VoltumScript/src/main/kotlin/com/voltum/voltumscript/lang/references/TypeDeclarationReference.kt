package com.voltum.voltumscript.lang.references

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.lang.index.VoltumNamedElementIndex
import com.voltum.voltumscript.lang.types.PrototypeContainer
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumReferenceElement

//class TypeDeclarationReference : VoltumReferenceCached<VoltumReferenceElement> {
class TypeDeclarationReference : VoltumReferenceBase<VoltumReferenceElement> {
    constructor(el: VoltumReferenceElement, idEl: PsiElement?) : super(el, idEl)

    override fun resolveInner(): List<VoltumElement> {
        var elements = VoltumNamedElementIndex.findElementsByName(element.project, idElement?.text!!)
        if (elements.isEmpty()) {
            PrototypeContainer.typeAliases[idElement.text!!]?.let {
                elements = VoltumNamedElementIndex.findElementsByName(element.project, it)
            }
        }
        val results = elements.map { it.getNameId() ?: it }
        return results
    }

    override fun calculateDefaultRangeInElement(): TextRange = element.referenceTextRange
}