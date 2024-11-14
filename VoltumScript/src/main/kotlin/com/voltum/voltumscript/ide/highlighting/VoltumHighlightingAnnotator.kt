package com.voltum.voltumscript.ide.highlighting

import com.intellij.codeInspection.util.InspectionMessage
import com.intellij.lang.annotation.AnnotationHolder
import com.intellij.lang.annotation.Annotator
import com.intellij.lang.annotation.HighlightSeverity
import com.intellij.openapi.application.ApplicationManager
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.psi.PsiElement
import com.intellij.psi.impl.source.tree.LeafPsiElement
import com.intellij.psi.util.elementType
import com.voltum.voltumscript.psi.*

class VoltumHighlightingAnnotator : Annotator {
    override fun annotate(element: PsiElement, holder: AnnotationHolder) {
        if (element.elementType == VoltumTypes.ID) {
            val parent = element.parent ?: return
            when (parent) {
                is VoltumVarId      -> {
                    if (parent.parent is VoltumVariableDeclaration) {
                        if (parent.parent?.parent is VoltumFile)
                            newAnnotation(holder, element, VoltumColors.GLOBAL_VARIABLE)
                        else
                            newAnnotation(holder, element, VoltumColors.LOCAL_VARIABLE)
                    } else
                        newAnnotation(holder, element, VoltumColors.FIELD_NAME)
                }

                is VoltumTypeRef    -> newAnnotation(holder, element, VoltumColors.TYPE_REFERENCE)
                is VoltumArgumentId -> newAnnotation(holder, element, VoltumColors.PARAMETER)
                is VoltumFuncId     -> newAnnotation(holder, element, VoltumColors.FUNCTION)
                is VoltumTypeId     -> newAnnotation(holder, element, VoltumColors.TYPE_NAME)
            }
        }

        /*    if (element is VoltumIdent) {
                highlightIdentifier(holder, element)
            }
            if (element is VoltumIdentifierWithType) {
                thisLogger().info("VoltumHighlightingAnnotator: VoltumIdentifierWithType")
                newAnnotation(holder, element.type, VoltumColors.TYPE_REFERENCE)
                newAnnotation(holder, element.nameIdentifier, VoltumColors.PARAMETER)
            }
    */
    }

    private fun highlightIdentifier(holder: AnnotationHolder, element: PsiElement) {
        val parent = element.parent ?: return

        when (parent) {
            is VoltumTypeDeclaration            -> newAnnotation(holder, element, VoltumColors.TYPE_NAME)
            is VoltumTypeDeclarationFieldMember -> {
                if (element is VoltumVarId)
                    newAnnotation(holder, element, VoltumColors.FIELD_NAME)
                if (element is VoltumTypeRef)
                    newAnnotation(holder, element, VoltumColors.TYPE_REFERENCE)
            }

            is VoltumTypeRef                    -> newAnnotation(holder, element, VoltumColors.TYPE_REFERENCE)
            // is VoltumIdentifierWithType         -> {
            //     if (element is VoltumTypeRef)
            //         newAnnotation(holder, element, VoltumColors.TYPE_REFERENCE)
            //     if (element is VoltumArgumentId)
            //         newAnnotation(holder, element, VoltumColors.PARAMETER)
            // }

            is VoltumPath                       -> highlightPathExpression(parent, holder, element)
        }
    }

    private fun highlightPathExpression(
        expr: VoltumPath,
        holder: AnnotationHolder,
        element: PsiElement
    ) {
        when (val topmostParent = expr.getTopMostPathParent()) {

            is VoltumCallExpr -> if (topmostParent.reference?.resolve() is VoltumTypeDeclarationFieldMember) {
                newAnnotation(holder, element, VoltumColors.FIELD_REFERENCE)
            } else {
                newAnnotation(holder, element, VoltumColors.FUNCTION)
            }

//            is PrismaBlockAttribute, is PrismaFieldAttribute ->
//                newAnnotation(holder, element, VoltumColors.ATTRIBUTE)

            else              -> newAnnotation(holder, element, VoltumColors.FIELD_REFERENCE)
        }
    }

    private fun newAnnotation(
        holder: AnnotationHolder, element: PsiElement, textAttributesKey: TextAttributesKey
    ) {
        newAnnotationBuilder(holder, textAttributesKey.externalName)
            .textAttributes(textAttributesKey)
            .range(element)
            .create()
    }

    private fun newAnnotationBuilder(holder: AnnotationHolder, @InspectionMessage tag: String) =
        if (ApplicationManager.getApplication().isUnitTestMode)
            holder.newAnnotation(HighlightSeverity.INFORMATION, tag)
        else
            holder.newSilentAnnotation(HighlightSeverity.INFORMATION)
}