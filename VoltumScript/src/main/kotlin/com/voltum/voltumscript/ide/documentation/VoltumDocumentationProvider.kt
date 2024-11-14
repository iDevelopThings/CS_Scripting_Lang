package com.voltum.voltumscript.ide.documentation

import com.intellij.lang.documentation.AbstractDocumentationProvider
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.util.TextRange
import com.intellij.openapi.util.io.toNioPathOrNull
import com.intellij.openapi.vfs.LocalFileSystem
import com.intellij.psi.*
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.startOffset
import com.voltum.voltumscript.VoltumUtil
import com.voltum.voltumscript.ext.toPsiFile
import com.voltum.voltumscript.psi.VoltumCallExpr
import com.voltum.voltumscript.psi.VoltumReferenceElement
import java.util.function.Consumer

class VoltumDocumentationProvider : AbstractDocumentationProvider() {
    override fun generateDoc(element: PsiElement, originalElement: PsiElement?): String? {
        return VoltumDocumentationBuilder(element).buildDocumentation()
    }

    override fun generateRenderedDoc(comment: PsiDocCommentBase): String? {
        val docComment = comment as? VoltumVirtualDocumentationComment ?: return null
        return VoltumDocumentationRenderer(docComment).render()
    }

    override fun getDocumentationElementForLink(psiManager: PsiManager?, link: String?, context: PsiElement?): PsiElement? {
        if (link == null)
            return null
        val project = context?.project!!

        var linkStr = link
        // our link looks like `file/file_path#offset`
        if (!linkStr.startsWith("file/"))
            return null

        linkStr = linkStr.substring("file/".length)
        // we need to split by `#` but only the last one
        val parts = linkStr.lastIndexOf('#').let {
            if (it == -1) {
                listOf(linkStr)
            } else {
                listOf(linkStr.substring(0, it), linkStr.substring(it + 1))
            }
        }
        if (parts.size != 2)
            return null
        val path = parts[0].toNioPathOrNull() ?: return null
        val virtualFile = LocalFileSystem.getInstance().findFileByNioFile(path) ?: return null
        val file = virtualFile.toPsiFile(context.project) ?: return null
        
        
//        val file = VoltumUtil.findFileByPath(project, parts[0]) ?: return null
        val offset = parts[1].toIntOrNull() ?: return null
        val element = file.findElementAt(offset) ?: return null
        if (element is PsiWhiteSpace) {
            return element.nextSibling
        }

        return element.parent
    }

    override fun getCustomDocumentationElement(
        editor: Editor,
        file: PsiFile,
        contextElement: PsiElement?,
        targetOffset: Int
    ): PsiElement? {
        if (contextElement as? VoltumReferenceElement != null) {
            return contextElement
        }
        return super.getCustomDocumentationElement(editor, file, contextElement, targetOffset)
    }

    private fun acceptCustomElement(context: PsiElement?): Boolean {
        if (context == null) {
            return false
        }
        if ((context as? VoltumReferenceElement)?.reference?.resolve() != null) {
            return false
        }

//        if (context is VoltumCallExpr) {
//            return !isFieldExpression(context)
//        }
        return true
    }

    override fun findDocComment(file: PsiFile, range: TextRange): PsiDocCommentBase? {
        val element = file.findElementAt(range.startOffset)
        val comments = generateSequence(element) { el -> el.nextSibling }
            .takeWhile { it.startOffset < range.endOffset }
            .filter { it.isComment && !it.isTrailingComment }
            .mapNotNull { it as? PsiComment }
            .toList()

        return groupComments(comments).firstOrNull()
    }

    override fun collectDocComments(file: PsiFile, sink: Consumer<in PsiDocCommentBase>) {
        val comments = PsiTreeUtil
            .findChildrenOfType(file, PsiComment::class.java)
            .filter { it.isComment && !it.isTrailingComment }

        groupComments(comments).forEach {
            sink.accept(it)
        }
    }

    private fun groupComments(comments: List<PsiComment>): List<VoltumVirtualDocumentationComment> {
        if (comments.isEmpty()) {
            return emptyList()
        }

        val docComments = mutableListOf<VoltumVirtualDocumentationComment>()
        var i = comments.lastIndex
        while (i >= 0) {
            val comment = comments[i]
            val block = comment.collectPrecedingDocComments(false)

            if (block.isNotEmpty()) {
                docComments.add(VoltumVirtualDocumentationComment(block))
                i = comments.indexOf(block.first()) - 1
            } else {
                i--
            }
        }
        return docComments
    }
}