package com.voltum.voltumscript.psi

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.navigation.ItemPresentation
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.diagnostic.thisLogger
import com.intellij.psi.PsiElement
import com.intellij.psi.ResolveState
import com.intellij.psi.impl.source.resolve.reference.ReferenceProvidersRegistry
import com.intellij.psi.scope.PsiScopeProcessor
import com.intellij.psi.util.elementType
import com.voltum.voltumscript.ide.presentation.Presentation
import com.voltum.voltumscript.lang.references.VoltumReference

@Suppress("UNUSED_PARAMETER")
object VoltumPsiUtilImpl {

    private val LOG = logger<VoltumPsiUtilImpl>()

    @JvmStatic
    fun getLookupElement(el: PsiElement): LookupElement? {
        thisLogger().warn("getLookupElement: VoltumVarId called, but not implemented")
        return null
    }

    @JvmStatic
    fun getNameId(el: PsiElement): VoltumIdent? {
        return when (el) {
            is VoltumFuncDeclaration     -> el.nameIdentifier
            is VoltumVariableDeclaration -> el.varName
            is VoltumTypeDeclaration     -> el.typeId
            is VoltumIdentifier          -> el
            is VoltumPath                -> el.getNameId()
            is VoltumTypeRef             -> el
            is VoltumCallExpr            -> el.qualifier?.getNameId() ?: el.path?.getNameId()

            else                         -> {
                throw UnknownNameElementException(el)
            }
        }
    }

    @JvmStatic
    fun getNameIdentifier(el: PsiElement): PsiElement? {
        val id = getNameId(el)
        return when (id?.elementType) {
            VoltumTypes.ID -> id
            else           -> id?.node?.findChildByType(VoltumTypes.ID)?.psi
        }

        /*return when (el) {

            is VoltumFuncDeclaration -> el.funcId
            is VoltumVariableDeclaration -> el.varName?.nameIdentifier
            is VoltumTypeDeclaration -> el.typeId
            is VoltumAccessExpr -> el.varReference
            is VoltumIdent -> el.nameIdentifier
            is VoltumNamedElement -> {
                // run `el.nameIdentifier` and catch stack overflow
                try {
                    el.nameIdentifier
                } catch (e: StackOverflowError) {
                    LOG.error("StackOverflowError in getNameIdentifier for ${el.javaClass}")
                    null
                }
            }

            is VoltumAccessExprImpl -> {
                return el.node.findChildByType(VoltumTypes.ID)?.psi
            }
            
            else -> {
                val idNode = el.node.findChildByType(VoltumTypes.ID)
                val varRefNode = el.descendantsOfType<VoltumIdentifier>()
                val lastVarRefNode = el.lastDescendantOfType<VoltumIdentifier>()

                LOG.warn("getNameIdentifier not implemented for ${el.node.elementType}, falling back; idNode: $idNode, varRefNode: $varRefNode, lastVarRefNode: $lastVarRefNode")

                val id: PsiElement = el.node.findChildByType(VoltumTypes.ID)?.psi
                    ?: throw UnknownNameElementException(el)

                id
            }
        }*/
    }

    @JvmStatic
    fun setName(el: PsiElement, name: String): PsiElement {
        return when (el) {

            is VoltumNamedElement -> {
                val idNode = el.node.findChildByType(VoltumTypes.ID)
                if (idNode != null) {
                    val newIdNode = VoltumElementFactory.createId(el.project, name)
                    el.node.replaceChild(idNode, newIdNode)
                }
                el
            }

            else                  -> {
                LOG.warn("setName called, but not implemented")
                el
            }
        }
    }

    @JvmStatic
    fun getPresentation(el: PsiElement): ItemPresentation? {
        if (el is VoltumIdentifier) {
            return Presentation.forElement(el.parent)
        }
        return Presentation.forElement(el)
    }

    @JvmStatic
    fun getLHS(element: PsiElement): PsiElement? {
        return null
    }

    @JvmStatic
    fun getReference(el: PsiElement): VoltumReference? {
        val references = getReferences(el)
        return if (references.isEmpty()) null else references[0]
    }

    @JvmStatic
    fun getReferences(inEl: PsiElement): Array<VoltumReference> {
        return ReferenceProvidersRegistry.getReferencesFromProviders(inEl).mapNotNull { it as? VoltumReference }.toTypedArray()
    }
    /*@JvmStatic
    fun getReferences(inEl: PsiElement): Array<PsiReference> {
        var el = inEl
        if (el is VoltumTopLevelDeclaration)
            el = el.getDeclaration()!!

        return when (el) {
            is VoltumTypeDeclaration -> {
                val id = el.typeId.node
                val startOffset: Int = id.startOffset - el.node.textRange.startOffset
                val range = TextRange(startOffset, startOffset + id.textLength)

                arrayOf(DeclarationReference(el, range))
            }
            is VoltumFunction -> {
                val id = el.nameIdentifier?.node!!
                val startOffset: Int = id.startOffset - el.node.textRange.startOffset
                val range = TextRange(startOffset, startOffset + id.textLength)

                arrayOf(DeclarationReference(el, range))
            }

            is VoltumFuncId -> {
                val id = el.node
                val startOffset: Int = id.startOffset - el.node.textRange.startOffset
                val range = TextRange(startOffset, startOffset + id.textLength)

                arrayOf(DeclarationReference(el, range))
            }
            
            is VoltumVarId -> {
                val id = el.node
                val startOffset: Int = id.startOffset - el.node.textRange.startOffset
                val range = TextRange(startOffset, startOffset + id.textLength)

                arrayOf(DeclarationReference(el, range))
            }
            is VoltumVariableDeclaration -> {
                val id = el.varName!!.node
                val startOffset: Int = id.startOffset - el.node.textRange.startOffset
                val range = TextRange(startOffset, startOffset + id.textLength)

                arrayOf(DeclarationReference(el, range))
            }

            else -> {
                LOG.error("getReferences() not implemented for ${el.javaClass}")

                emptyArray()
            }
        }
    }*/


    @JvmStatic
    fun processDeclarations(element: PsiElement, processor: PsiScopeProcessor, state: ResolveState, place: PsiElement): Boolean {
        when (element) {
            /*is VoltumArgumentDeclarationList -> {
                element.argumentDeclarationList.forEach {
                    if (!processor.execute(it, state)) {
                        return false
                    }
                }
            }*/

            is VoltumBlockBody           -> {
                element.statementList.forEach {
                    if (!it.processDeclarations(processor, state, element.parent, element)) {
                        return false
                    }
                    // if (!processor.execute(it, state)) {
                    // 		return false
                    // }
                }
            }

            is VoltumFunction            -> {
                if (!element.processDeclarations(processor, state, element.parent, place))
                    return false
            }

            is VoltumVariableDeclaration -> {
                if (!element.processDeclarations(processor, state, element.parent, place))
                    return false
            }

            is VoltumStatement           -> {
                if (element.variableDeclaration != null) {
                    if (!element.variableDeclaration!!.processDeclarations(processor, state, element.parent, place))
                        return false
                }
//                if (element.deferStatement != null) {
//                    if (!element.deferStatement!!.processDeclarations(processor, state, element.parent, place))
//                        return false
//                }
            }

            else                         -> {
                LOG.error("processDeclarations not implemented for ${element.javaClass}")
            }
        }

        return true
    }

}