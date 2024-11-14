package com.voltum.voltumscript.ide.folding

import com.intellij.application.options.CodeStyle
import com.intellij.codeInsight.folding.CodeFoldingSettings
import com.intellij.lang.ASTNode
import com.intellij.lang.folding.CustomFoldingBuilder
import com.intellij.lang.folding.FoldingDescriptor
import com.intellij.openapi.editor.Document
import com.intellij.openapi.editor.FoldingGroup
import com.intellij.openapi.project.DumbAware
import com.intellij.openapi.util.TextRange
import com.intellij.psi.PsiComment
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiWhiteSpace
import com.intellij.psi.tree.TokenSet
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.nextLeaf
import com.voltum.voltumscript.ext.document
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.parser.*
import com.voltum.voltumscript.psi.VoltumBlockBody
import com.voltum.voltumscript.psi.VoltumFile
import com.voltum.voltumscript.psi.VoltumFunction
import com.voltum.voltumscript.psi.VoltumTypes.*
import com.voltum.voltumscript.psi.VoltumVisitor
import com.voltum.voltumscript.psi.ext.*
import java.lang.Integer.max

class VoltumFoldingBuilder : CustomFoldingBuilder(), DumbAware {

    override fun getLanguagePlaceholderText(node: ASTNode, range: TextRange): String {
        when (node.elementType) {
            LCURLY -> return " { "
            RCURLY -> return " }"
        }
        return when (node.psi) {
            is PsiComment -> "/* ... */"
            else -> "{...}"
        }
    }

    override fun isRegionCollapsedByDefault(node: ASTNode): Boolean = CodeFoldingSettings.getInstance().isDefaultCollapsedNode(node)
    override fun isCustomFoldingRoot(node: ASTNode) = node.elementType == BLOCK_BODY

    override fun buildLanguageFoldRegions(descriptors: MutableList<FoldingDescriptor>, root: PsiElement, document: Document, quick: Boolean) {
        if (root !is VoltumFile)
            return

        var rightMargin = CodeStyle.getSettings(root).getRightMargin(VoltumLanguage)

        val visitor = object : VoltumVisitor() {
            /*override fun visitFile(file: PsiFile) {
                super.visitFile(file)
                if (file is VoltumFile) {
                    val topLevelDeclarations = file.children
                    for (topLevelDeclaration in topLevelDeclarations) {
                        if (topLevelDeclaration is VoltumTopLevelDeclaration)
                            topLevelDeclaration.accept(this)
                    }
                }
            }*/

            private fun fold(element: PsiElement) {
                descriptors += FoldingDescriptor(element.node, element.textRange)
            }

            private fun foldBetween(element: PsiElement, left: PsiElement?, right: PsiElement?) {
                if (left != null && right != null && right.textLength > 0) {
                    val range = TextRange(left.textOffset, right.textOffset + 1)
                    descriptors += FoldingDescriptor(element.node, range)
                }
            }

            private fun foldBlock(block: VoltumBlockBody) {
                if(tryFoldBlockWhitespaces(block))
                    return
                
                val lbrace = block.lcurly
                val rbrace = block.rcurly
                
                foldBetween(block, lbrace, rbrace)
            }
            private fun tryFoldBlockWhitespaces(block: VoltumBlockBody): Boolean {
                if (block.parent !is VoltumFunction) return false

                val doc = block.containingFile.document ?: return false
                val maxLength = rightMargin - block.getOffsetInLine(doc) - ONE_LINER_PLACEHOLDERS_EXTRA_LENGTH
                if (!block.isSingleLine(doc, maxLength)) return false

                val lbrace = block.lcurly
                val rbrace = block.rcurly ?: return false

                val blockElement = lbrace.getNextNonCommentSibling()
                if (blockElement == null || blockElement != rbrace.getPrevNonCommentSibling()) return false
                if (blockElement.textContains('\n')) return false
                if (!(doc.areOnAdjacentLines(lbrace, blockElement) && doc.areOnAdjacentLines(blockElement, rbrace))) return false

                val leadingSpace = lbrace.nextSibling as? PsiWhiteSpace ?: return false
                val trailingSpace = rbrace.prevSibling as? PsiWhiteSpace ?: return false

                val leftEl = block.prevSibling as? PsiWhiteSpace ?: lbrace
                val range1 = TextRange(leftEl.textOffset, leadingSpace.endOffset)
                val range2 = TextRange(trailingSpace.textOffset, rbrace.endOffset)
                val group = FoldingGroup.newGroup("one-liner")
                descriptors += FoldingDescriptor(lbrace.node, range1, group)
                descriptors += FoldingDescriptor(rbrace.node, range2, group)

                return true
            }

            override fun visitComment(comment: PsiComment) {
                when(comment.tokenType) {
                    VoltumTokenTypes.BLOCK_COMMENT -> fold(comment)
                    VoltumTokenTypes.EOL_COMMENT   -> fold(comment)
                    
                }
            }
            override fun visitBlockBody(o: VoltumBlockBody) = foldBlock(o)

        }
        PsiTreeUtil.processElements(root) { it.accept(visitor); true }
    }

    private companion object {
        val COLLAPSED_BY_DEFAULT = TokenSet.create(LCURLY, RCURLY)
        const val ONE_LINER_PLACEHOLDERS_EXTRA_LENGTH = 4
    }
}


private fun CodeFoldingSettings.isDefaultCollapsedNode(node: ASTNode) =
    (this.COLLAPSE_DOC_COMMENTS && node.elementType in COMMENTS)
            || (this.COLLAPSE_METHODS && node.elementType == BLOCK_BODY && node.psi.parent is VoltumFunction)


private fun Document.areOnAdjacentLines(first: PsiElement, second: PsiElement): Boolean =
    getLineNumber(first.endOffset) + 1 == getLineNumber(second.startOffset)

private fun VoltumBlockBody.isSingleLine(doc: Document, maxLength: Int): Boolean {
    val startContents = lcurly.rightSiblings.dropWhile { it is PsiWhiteSpace }.firstOrNull() ?: return false
    if (startContents.node.elementType == RCURLY) return false
    val endContents = rcurly?.leftSiblings?.dropWhile { it is PsiWhiteSpace }?.firstOrNull() ?: return false
    if (endContents.endOffset - startContents.textOffset > maxLength) return false
    return doc.getLineNumber(startContents.textOffset) == doc.getLineNumber(endContents.endOffset)
}

private fun PsiElement.getOffsetInLine(doc: Document): Int {
    val blockLine = doc.getLineNumber(startOffset)
    return leftLeaves
        .takeWhile { doc.getLineNumber(it.endOffset) == blockLine }
        .sumOf { el -> el.text.lastIndexOf('\n').let { el.text.length - max(it + 1, 0) } }
}

private fun PsiElement.foldRegionStart(): Int {
    val nextLeaf = nextLeaf(skipEmptyElements = true) ?: return endOffset
    return if (nextLeaf.text.startsWith(' ')) {
        endOffset + 1
    } else {
        endOffset
    }
}