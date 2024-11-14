package com.voltum.voltumscript.ide.documentation

import com.intellij.codeInsight.documentation.DocumentationManagerUtil
import com.intellij.openapi.editor.DefaultLanguageHighlighterColors
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.project.DumbService
import com.intellij.psi.PsiElement
import com.intellij.psi.util.elementType
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.ext.addElementLink
import com.voltum.voltumscript.ext.buildPrinter
import com.voltum.voltumscript.ide.highlighting.VoltumColors
import com.voltum.voltumscript.ide.presentation.VoltumPsiRenderer
import com.voltum.voltumscript.parser.isKeyword
import com.voltum.voltumscript.psi.*

private const val UNKNOWN = "<unknown>"

class VoltumDocumentationDefinitionBuilder(
    private val element: PsiElement,
    private val resolveElement: VoltumReferenceElement?,
    private val resultReferenceElement: VoltumElement?,
) {
    fun buildDefinition(): String? {

        val text = buildPrinter {
            if (element is VoltumPath)
                namedElement(element)
            else if (element is VoltumNamedElement)
                namedElement(element)

            when (element.parent) {
                is VoltumTypeDeclarationFieldMember -> buildFieldDeclaration(element.parent as VoltumTypeDeclarationFieldMember)
                is VoltumDictionaryField            -> buildKeyValue(element.parent as VoltumDictionaryField)
            }

            buildKeyword()
        }

        if (text.isEmpty()) {
            return null
        }

        return text
        
//        return toHtml(element.project, text)
    }

    private fun Printer.buildKeyword() {
        if (element.isKeyword) {
            a(element.text)
        }
    }

    private fun Printer.buildFieldDeclaration(element: VoltumTypeDeclarationFieldMember) {
        val fieldType = element.typeRef

        a(": ")
        source(fieldType)
    }

    private fun Printer.buildKeyValue(element: VoltumDictionaryField) {
        a(" = ")
        source(element.value)
    }

    private fun Printer.source(element: PsiElement?) = a(VoltumPsiRenderer().build(element))

    private fun Printer.namedElement(element: VoltumNamedElement) {
        val name = element.name ?: UNKNOWN


        a("element type: ${element.elementType}\n")
        a("class: ${element.javaClass.simpleName}\n")

        if (resultReferenceElement != null) {
            addElementLink(resultReferenceElement)            
            return
        }

        keyword(element)
        qualifier(element)
        a(name)
    }

    private fun Printer.qualifier(element: VoltumNamedElement) {
        if (element.parent is VoltumTypeDeclarationFieldMember) {
            val parent = element.parent as VoltumTypeDeclarationFieldMember

            parent.owner()?.name?.let {
                a(it)
                a(".")
            }
        }
    }

    private fun Printer.keyword(element: PsiElement) {
        element.keyword?.let {
            a(it)
            a(" ")
        }
    }

    private val PsiElement.keyword: String?
        get() {
            return when (this) {
                is VoltumTypeDeclaration -> "type"
                else                                                                    -> null
            }
        }
}