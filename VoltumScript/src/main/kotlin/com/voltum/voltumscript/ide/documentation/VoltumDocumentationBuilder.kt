package com.voltum.voltumscript.ide.documentation

import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.text.HtmlChunk
import com.intellij.psi.PsiElement
import com.voltum.voltumscript.VoltumBundle
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.ext.addElementLink
import com.voltum.voltumscript.ext.buildPrinter
import com.voltum.voltumscript.ide.presentation.VoltumPsiRenderer
import com.voltum.voltumscript.psi.*

class VoltumDocumentationBuilder(private val element: PsiElement) {
    private val psiRenderer = VoltumPsiRenderer()

    private val resolveElement: VoltumReferenceElement?
        get() {
            var tempEl = element as VoltumElement?
            if (tempEl is VoltumArgumentId) {
                tempEl = element.parent as VoltumIdentifierWithType?
                tempEl = tempEl?.type
            }
            if (tempEl?.parent is VoltumPath) {
                tempEl = tempEl.parent as VoltumPath
            }
            if (tempEl?.parent is VoltumTypeDeclarationFieldMember) {
                tempEl = tempEl.parent as VoltumTypeDeclarationFieldMember
                tempEl = tempEl.typeRef
            }
            if (tempEl?.parent is VoltumVariableDeclaration) {
                tempEl = tempEl.parent as VoltumVariableDeclaration
            }

            return tempEl as VoltumReferenceElement?
        }
    private val resultReferenceElement: VoltumElement?
        get() {
            val resolveElement = resolveElement ?: return null
            return resolveElement.reference?.resolve()
        }

    @NlsSafe
    fun buildDocumentation(): String? {
        /* val proto = element.prototype
         val doc = buildDocumentation(proto)
         if (doc != null)
             return doc*/

        val definitionBuilder = VoltumDocumentationDefinitionBuilder(
            element,
            resolveElement,
            resultReferenceElement
        )
        val def = definitionBuilder.buildDefinition() ?: return null

        return buildPrinter {
            definition { a(def) }
            documentationComment(element)
            additionalSections()
        }
    }

    /*private fun buildDocumentation(type: Ty): String? {
        buildPrinter {

        }
    }*/

    private fun Printer.paramsSection(params: List<VoltumIdentifierWithType>) {
        if (params.isEmpty())
            return

        sections {
            for ((i, param) in params.withIndex()) {
                val header = if (i == 0) VoltumBundle.message("voltum.doc.section.params") else ""

                section(header) {
                    cell { pre(param.nameIdentifier.text!!) }
                    cellDivider()
                    cell(noWrap = false) { a(documentationMarkdownToHtml(param.getDocumentation()) ?: "") }
                }
            }
        }
    }


    private fun Printer.additionalSections() {
        /*var tempEl = element as VoltumElement?
        if (tempEl is VoltumArgumentId) {
            tempEl = element.parent as VoltumIdentifierWithType?
            tempEl = tempEl?.type
        }
        if (tempEl?.parent is VoltumPath) {
            tempEl = tempEl.parent as VoltumPath
        }
        if (tempEl?.parent is VoltumTypeDeclarationFieldMember) {
            tempEl = tempEl.parent as VoltumTypeDeclarationFieldMember
            tempEl = tempEl.typeRef
        }
        if(tempEl?.parent is VoltumVariableDeclaration) {
            tempEl = tempEl.parent as VoltumVariableDeclaration
        }
        if (tempEl is VoltumReferenceElement) {
            tempEl = tempEl.reference?.resolve()
            when (tempEl?.parent) {
                is VoltumTypeDeclaration -> typeDeclMember(tempEl.parent as VoltumTypeDeclaration)
                else                     -> when (tempEl) {
                    is VoltumTypeDeclaration -> typeDeclMember(tempEl)
                }
            }
        }
        */

        when (resultReferenceElement?.parent) {
            is VoltumTypeDeclaration -> typeDeclMember(resultReferenceElement?.parent as VoltumTypeDeclaration)

            else                                                                    -> when (element) {
                is VoltumTypeDeclaration -> typeDeclMember(element)
            }
        }

    }

    private fun Printer.typeDeclMember(element: VoltumTypeDeclaration) {
        sections {
            val fields = element.fields
            if (fields.isNotEmpty()) {
                section(VoltumBundle.message("voltum.doc.section.fields")) {
                    cell {
                        fields.forEach {

                            var fieldContent = buildPrinter {
                                a(psiRenderer.span(it.varId).style("padding-right: 5px"))

                                it.typeRef.reference?.resolve()?.let {
                                    addElementLink(it, {
//                                    val f = preformatted(it,true, "span")
                                        val f = HtmlChunk.raw(it).wrapWith("code")// .wrapWith(HtmlChunk.p())
                                        a(f)
                                    })
                                } ?: a(psiRenderer.span(it.typeRef)/*.wrapWith(HtmlChunk.p())*/)

                                documentationComment(it, {
                                    ln(HtmlChunk.raw(it).wrapWith(HtmlChunk.p()).wrapWith("small").toString())
                                })
                            }
                            
                            ln(HtmlChunk.raw(fieldContent)
                                   .wrapWith("p")
                                   .style("padding-bottom: 5px")
                                   .toString())

                        }
                    }
                    /*
                    cell {
                        fields.forEach {
                            a(psiRenderer.pre(it.varId).wrapWith(HtmlChunk.p()))
                            documentationComment(it, {
                                a(HtmlChunk.raw(it).wrapWith(HtmlChunk.p()))
                            })
                        }
                    }
//                    cellDivider()
                    cell {
                        fields.forEach {
                            it.typeRef.reference?.resolve()?.let {
                                addElementLink(it, {
//                                    val f = preformatted(it,true, "span")
                                    val f = HtmlChunk.raw(it).wrapWith(HtmlChunk.tag("code")).wrapWith(HtmlChunk.p())
                                    a(f)
                                })
                            } ?: a(psiRenderer.span(it.typeRef).wrapWith(HtmlChunk.p()))

                        }

                    }*/
                }
            }

            val methods = element.methods
            if (methods.isNotEmpty()) {
                section(VoltumBundle.message("voltum.doc.section.methods")) {
                    cell {
                        methods.forEach {
                            var methodString = it.nameIdentifier.text
                            methodString += it.arguments.asPresentationString()

                            a(preformatted(methodString).wrapWith(HtmlChunk.p()))
                        }
                    }
                }
            }
        }
    }

    private fun VoltumPsiRenderer.span(element: PsiElement?, noWrap: Boolean = true): HtmlChunk.Element =
        preformatted(psiRenderer.build(element), noWrap, "span")

    private fun VoltumPsiRenderer.pre(element: PsiElement?, noWrap: Boolean = true): HtmlChunk.Element =
        preformatted(psiRenderer.build(element), noWrap)

    private fun Printer.pre(@NlsSafe source: String, noWrap: Boolean = true): Printer =
        a(preformatted(source, noWrap))

    private fun preformatted(@NlsSafe source: String, noWrap: Boolean = true, tagName: String = "code"): HtmlChunk.Element {
        val code = HtmlChunk.tag(tagName).let { if (noWrap) it.style(NO_WRAP) else it }
        return HtmlChunk.text(source).wrapWith(code)
    }
}