package com.voltum.voltumscript.lang.references

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.util.TextRange
import com.intellij.psi.*
import com.voltum.voltumscript.ext.measureLogTime
import com.voltum.voltumscript.lang.resolver.ResolveCacheDependency
import com.voltum.voltumscript.lang.resolver.VoltumResolveCache
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumReferenceElement

interface VoltumReference : PsiPolyVariantReference {
    override fun getElement(): VoltumElement
    override fun resolve(): VoltumElement?
    fun resolveInner(): List<VoltumElement>
}


abstract class VoltumReferenceBase<T : VoltumReferenceElement> : PsiPolyVariantReferenceBase<T>, VoltumReference {

    protected val idElement: PsiElement?

    companion object {
        val log = logger<VoltumReferenceBase<*>>()
    }

    constructor(element: T, idEl: PsiElement?) : super(element) {
        idElement = (idEl ?: element.nameIdentifier) ?: throw IllegalArgumentException("no id element provided, or element.nameIdentifier is null, for element: $element")
    }

    override fun resolve(): VoltumElement? = super.resolve() as? VoltumElement

    override fun multiResolve(incompleteCode: Boolean): Array<out ResolveResult> =
        resolveInner().map { PsiElementResolveResult(it) }.toTypedArray()

    final override fun getRangeInElement(): TextRange = super.getRangeInElement()

    override fun calculateDefaultRangeInElement(): TextRange {
        val anchor = idElement ?: return TextRange.EMPTY_RANGE
        check(anchor.parent === element, { "Anchor should be a child of the element, anchor is $anchor, element is $element - parent is ${anchor.parent}" })
        return TextRange.from(anchor.startOffsetInParent, anchor.textLength)
    }

    override fun handleElementRename(newName: String): PsiElement {
        val referenceNameElement = idElement
        if (referenceNameElement != null) {
//            doRename(referenceNameElement, newName)
            log.warn("handleElementRename: $referenceNameElement, $newName")
        }
        return element
    }

    override fun getVariants(): Array<out LookupElement> = LookupElement.EMPTY_ARRAY
    override fun equals(other: Any?): Boolean = other is VoltumReferenceBase<*> && element === other.element
    override fun hashCode(): Int = element.hashCode()

    abstract override fun resolveInner(): List<VoltumElement>

}


abstract class VoltumReferenceCached<T : VoltumReferenceElement> : VoltumReferenceBase<T> {

    constructor(element: T, idEl: PsiElement?) : super(element, idEl)

    final override fun multiResolve(incompleteCode: Boolean): Array<out ResolveResult> =
        cachedMultiResolve().toTypedArray()

    override fun resolveInner(): List<VoltumElement> =
        cachedMultiResolve().mapNotNull { it.element as? VoltumElement }

    private fun cachedMultiResolve(): List<PsiElementResolveResult> {
        measureLogTime {
            return VoltumResolveCache.getInstance(element.project)
                .resolveWithCaching(element, cacheDependency, Resolver).orEmpty()
        }
    }

    override fun toString(): String {
        val resultsStr = resolveInner().joinToString { it.text }
        return "${javaClass.simpleName}(${myElement.javaClass.simpleName}:$rangeInElement, str = ${rangeInElement.substring(myElement.text)}, result = ${resultsStr})"
    }

    protected open val cacheDependency: ResolveCacheDependency get() = ResolveCacheDependency.ANY_PSI_CHANGE

    private object Resolver : (VoltumReferenceElement) -> List<PsiElementResolveResult> {
        override fun invoke(ref: VoltumReferenceElement): List<PsiElementResolveResult> {
            return (ref.reference as VoltumReferenceCached<*>).resolveInner().map { PsiElementResolveResult(it) }
        }
    }
}

