package com.voltum.voltumscript.ide.documentation

import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiComment
import com.intellij.psi.PsiDocCommentBase
import com.intellij.psi.PsiElement
import com.intellij.psi.impl.FakePsiElement
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.endOffset
import com.intellij.psi.util.startOffset
import com.voltum.voltumscript.parser.VoltumTokenTypes

class VoltumVirtualDocumentationComment(val comments: List<PsiComment>) : FakePsiElement(), PsiDocCommentBase {
    init {
        check(comments.isNotEmpty()) { "Comments shouldn't be an empty list" }
    }

    override fun getParent(): PsiElement = comments.first().parent
    override fun getTokenType(): IElementType = VoltumTokenTypes.BLOCK_COMMENT
    override fun getOwner(): PsiElement? = PsiTreeUtil.skipWhitespacesAndCommentsForward(comments.last())
    override fun getTextRange(): TextRange = TextRange.create(comments.first().startOffset, comments.last().endOffset)
    override fun getText(): String = comments.joinToString(" ") { stripAllCommentPrefixes(it.text) }

    val content: String
        get() = text
}