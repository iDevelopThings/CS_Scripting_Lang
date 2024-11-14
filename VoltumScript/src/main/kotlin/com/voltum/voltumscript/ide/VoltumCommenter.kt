package com.voltum.voltumscript.ide

import com.intellij.lang.CodeDocumentationAwareCommenter
import com.intellij.lang.Commenter
import com.intellij.psi.PsiComment
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.parser.*

class VoltumCommenter : Commenter, CodeDocumentationAwareCommenter {
    override fun getLineCommentPrefix(): String = "//"
    override fun getBlockCommentPrefix(): String = "/*"
    override fun getBlockCommentSuffix(): String = "*/"
    override fun getCommentedBlockCommentPrefix(): String = "/**"
    override fun getCommentedBlockCommentSuffix(): String = "*/"
    
    override fun getLineCommentTokenType(): IElementType = VoltumTokenTypes.EOL_COMMENT
    override fun getBlockCommentTokenType(): IElementType = VoltumTokenTypes.BLOCK_COMMENT
    override fun getDocumentationCommentTokenType(): IElementType = VoltumTokenTypes.BLOCK_COMMENT
    override fun getDocumentationCommentPrefix(): String = commentedBlockCommentPrefix
    override fun getDocumentationCommentLinePrefix(): String = lineCommentPrefix
    override fun getDocumentationCommentSuffix(): String = commentedBlockCommentSuffix

    override fun isDocumentationComment(element: PsiComment?): Boolean = element is PsiComment
}
