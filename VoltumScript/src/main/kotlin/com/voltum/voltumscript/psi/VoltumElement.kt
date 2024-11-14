package com.voltum.voltumscript.psi

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.openapi.util.UserDataHolderEx
import com.intellij.psi.PsiComment
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiReference
import com.intellij.psi.PsiWhiteSpace
import com.intellij.psi.impl.source.tree.CompositePsiElement
import com.intellij.psi.search.SearchScope
import com.intellij.psi.search.searches.ReferencesSearch
import com.intellij.psi.tree.IElementType
import com.intellij.util.Query
import com.voltum.voltumscript.ide.documentation.VoltumVirtualDocumentationComment
import com.voltum.voltumscript.ide.documentation.collectPrecedingDocComments
import com.voltum.voltumscript.ide.documentation.trailingDocComment

interface VoltumDocumentationOwner : PsiElement {
    val docComment: VoltumVirtualDocumentationComment?
        get() {
            val comments = collectPrecedingDocComments()
            if (comments.isNotEmpty()) {
                return VoltumVirtualDocumentationComment(comments)
            }

            val trailing = trailingDocComment
            if (trailing != null) {
                return VoltumVirtualDocumentationComment(listOf(trailing))
            }

            return null
        }
}


interface VoltumElement : PsiElement, UserDataHolderEx, VoltumDocumentationOwner

abstract class VoltumElementImpl(type: IElementType) : CompositePsiElement(type), VoltumElement {

    override fun getNavigationElement(): PsiElement {
        return super.getNavigationElement()
    }

    override fun toString(): String = "${javaClass.simpleName}($elementType)"
}

interface PsiElementWithLookup {
    fun getLookupElement(): LookupElement?
}


private val PsiElement.isWhitespaceOrComment
    get(): Boolean = this is PsiWhiteSpace || this is PsiComment

fun VoltumElement.searchReferences(scope: SearchScope? = null): Query<PsiReference> {
    return if (scope == null) {
        ReferencesSearch.search(this)
    } else {
        ReferencesSearch.search(this, scope)
    }
}


fun VoltumElement.getDocumentation(): String? {
    return null
}