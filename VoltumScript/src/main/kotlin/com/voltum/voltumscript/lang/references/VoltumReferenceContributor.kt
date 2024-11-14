package com.voltum.voltumscript.lang.references

import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.util.TextRange
import com.intellij.psi.*
import com.intellij.util.ProcessingContext
import com.voltum.voltumscript.lang.VoltumPsiPatterns
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.startOffset

private val LOG = logger<VoltumReferenceContributor>()


class VoltumReferenceContributor : PsiReferenceContributor() {
    override fun registerReferenceProviders(registrar: PsiReferenceRegistrar) {

        registrar.registerReferenceProvider(
            VoltumPsiPatterns.qualifiedPathExpr,
            object : PsiReferenceProvider() {
                override fun getReferencesByElement(element: PsiElement, context: ProcessingContext): Array<PsiReference> {
                    return arrayOf(VoltumPathExprReference(element as VoltumPath, element.nameIdentifier))
                }
            }
        )
        registrar.registerReferenceProvider(
            VoltumPsiPatterns.unqualifiedPathExpr,
            object : PsiReferenceProvider() {
                override fun getReferencesByElement(element: PsiElement, context: ProcessingContext): Array<PsiReference> {
                    val varRefEl = element as VoltumVarReference; //  (element as VoltumPath).getNameId() as VoltumVarReference
                    return arrayOf(VariableReference(varRefEl, varRefEl.getId()))
                }
            }
        )
        registrar.registerReferenceProvider(
            VoltumPsiPatterns.typeReference,
            object : PsiReferenceProvider() {
                override fun getReferencesByElement(element: PsiElement, context: ProcessingContext): Array<PsiReference> {
                    return arrayOf(TypeDeclarationReference(element as VoltumReferenceElement, element))
//                    return arrayOf(FunctionReference(element as VoltumReferenceElement, element))
                }
            }
        )
        registrar.registerReferenceProvider(
            VoltumPsiPatterns.callExpr,
            object : PsiReferenceProvider() {
                override fun getReferencesByElement(element: PsiElement, context: ProcessingContext): Array<PsiReference> {
                    val callExpr = element as VoltumCallExpr
                    return arrayOf(CallExpressionReference(callExpr, callExpr.nameIdentifier))
                }
            }
        )
    }
}
/*

fun getNameIdentifierRange(): TextRange? {
    if (nameIdentifier == null) return null
    val startOffset: Int = nameIdentifier!!.startOffset - textRange.startOffset
    return TextRange(startOffset, startOffset + nameIdentifier!!.textLength)
}
*/

val PsiElement.referenceTextRange: TextRange
    get() = when (this) {
        is VoltumNamedElement -> {
            nameIdentifier?.let {
                val startOffset: Int = it.startOffset - textRange.startOffset
                TextRange(startOffset, startOffset + it.textLength)
            } ?: textRange
        }

        else                  -> textRange
    }
