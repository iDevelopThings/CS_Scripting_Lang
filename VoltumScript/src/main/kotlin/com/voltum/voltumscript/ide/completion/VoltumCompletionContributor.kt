@file:Suppress("SameParameterValue")

package com.voltum.voltumscript.ide.completion

import com.intellij.codeInsight.completion.*
import com.intellij.codeInsight.lookup.LookupElementBuilder
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.openapi.editor.EditorModificationUtil
import com.intellij.patterns.ElementPattern
import com.intellij.psi.PsiElement
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.elementType
import com.intellij.util.ProcessingContext
import com.voltum.voltumscript.lang.VoltumPsiPatterns
import com.voltum.voltumscript.lang.types.TyFieldKind
import com.voltum.voltumscript.parser.KeywordCompletionFlag
import com.voltum.voltumscript.parser.VoltumKeywords
import com.voltum.voltumscript.psi.VoltumPath
import com.voltum.voltumscript.psi.VoltumTypes
import com.voltum.voltumscript.psi.ext.inference
import com.voltum.voltumscript.psi.ext.prototype
import com.voltum.voltumscript.psi.ext.prototypeNullable

abstract class VoltumCompletionProvider : CompletionProvider<CompletionParameters>() {
    abstract val pattern: ElementPattern<out PsiElement>

    protected fun createLookupElement(
        label: String,
        element: PsiElement,
    ): LookupElementBuilder {
        var builder = LookupElementBuilder.create(label)
            .withPsiElement(element)
//            .withIcon(element.getIcon())
//            .withTypeText(element.type)

        return builder
    }
}

object VoltumKeywordProvider : VoltumCompletionProvider() {
    override val pattern = VoltumPsiPatterns.topKeyword

    override fun addCompletions(parameters: CompletionParameters, context: ProcessingContext, result: CompletionResultSet) {
        VoltumKeywords.forFlags(KeywordCompletionFlag.TOP_LEVEL).forEach { keyword ->
            createLookupElement(keyword.name.lowercase(), parameters.position).let { builder ->
                result.addElement(builder)
            }
        }
    }

}

object VoltumBlockKeywordProvider : VoltumCompletionProvider() {
    override val pattern = VoltumPsiPatterns.insideBlock

    override fun addCompletions(parameters: CompletionParameters, context: ProcessingContext, result: CompletionResultSet) {
        thisLogger().warn("insideBlock")
        VoltumKeywords.forFlags(KeywordCompletionFlag.BLOCK_BODY).forEach { keyword ->
            createLookupElement(keyword.name.lowercase(), parameters.position).let { builder ->
                result.addElement(builder)
            }
        }
    }

}

object VoltumPathCompletionProvider : VoltumCompletionProvider() {
    override val pattern = VoltumPsiPatterns.pathExpr

    override fun addCompletions(parameters: CompletionParameters, context: ProcessingContext, result: CompletionResultSet) {
        val position = parameters.position
        // avoid 'IntellijIdeaRulezzz' placeholder
        val element = CompletionUtil.getOriginalOrSelf(position)

        thisLogger().warn("pathCompletion, original element = '${position.text}', current = '${element.text}'")

        val path = PsiTreeUtil.findFirstParent(element) { it.elementType == VoltumTypes.PATH } as VoltumPath?

        if (path == null) {
            thisLogger().warn("pathCompletion, path not found")
            return
        }

        val qualifier = path.qualifier
        if (qualifier == null) {
            thisLogger().warn("pathCompletion, qualifier not found")
            return
        }
        thisLogger().warn("pathCompletion, qualifier = '${qualifier?.text}'")

        val qualifierVar = qualifier.lastVarReference!!

        val inf = qualifierVar.inference
        val type = qualifierVar.reference?.resolve()
        val value = inf?.allIdValues?.get(qualifierVar.name!!)
        val proto = value?.prototypeNullable

        if (type == null) {
            thisLogger().warn("pathCompletion, type not found")
            return
        }
        
        if(proto == null) {
            thisLogger().warn("pathCompletion, prototype not found")
            return
        }
        
        proto.members.values.forEach{
            createLookupElement(it.name, it.linkedElement!!).let { b ->
                var builder = b
                
                if(it.kind == TyFieldKind.METHOD) {
                    builder = builder.withInsertHandler{
                        context, _ ->
                        if(!context.alreadyHasCallParens) {
                            context.document.insertString(context.selectionEndOffset, "()")
                            context.doNotAddOpenParenCompletionChar()
                            EditorModificationUtil.moveCaretRelatively(context.editor, 1)
                        }
                    }
                }
                result.addElement(builder)
            }
        }

        thisLogger().warn("pathCompletion, type = '${type.text}'")


    }
}

class VoltumCompletionContributor : CompletionContributor() {
    init {
        extend(CompletionType.BASIC, VoltumKeywordProvider)
        extend(CompletionType.BASIC, VoltumBlockKeywordProvider)
        extend(CompletionType.BASIC, VoltumPathCompletionProvider)
    }

    private fun extend(type: CompletionType, provider: VoltumCompletionProvider) {
        extend(type, provider.pattern, provider)
    }

    override fun beforeCompletion(context: CompletionInitializationContext) {
        val element = context.file.findElementAt(context.startOffset) ?: return
        if (element.elementType == VoltumTypes.ID) {
            context.dummyIdentifier = CompletionInitializationContext.DUMMY_IDENTIFIER_TRIMMED
        }
    }
}