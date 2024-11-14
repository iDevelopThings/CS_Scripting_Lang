package com.voltum.voltumscript.ide.presentation

import com.intellij.ide.projectView.PresentationData
import com.intellij.navigation.ItemPresentation
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.ext.buildPrinter
import com.voltum.voltumscript.psi.*

object Presentation {

    fun forElement(psi: PsiElement): ItemPresentation {
        val presentation = buildPrinter {

            a(getPresentableName(psi))

            when (psi) {
                is VoltumFunction -> {
                    a(psi.getArguments().asPresentationString())
                    psi.getReturnType()?.let { a(" -> ${it.text}") }
                }
            }

        }

        return PresentationData(
            presentation,
            psi.containingFile.name,
            psi.getIcon(0),
            null
        )
    }

    fun getPresentableName(psi: PsiElement): String {
        return when (psi) {
            is VoltumPath                        -> psi.text
            is VoltumNamedElement                                                               -> psi.name!!
            is VoltumTypeDeclarationFieldMember  -> psi.varId.name!!
            is VoltumTypeDeclarationMethodMember -> psi.nameIdentifier.name!!
            is VoltumDictionaryField             -> psi.getKey()
            is VoltumIdentifierWithType          -> "${psi.nameIdentifier.text}: ${psi.type.text}"

            else                                 -> throw Exception("[getPresentableName] Unhandled element type: ${psi.javaClass}")
        }
    }
}