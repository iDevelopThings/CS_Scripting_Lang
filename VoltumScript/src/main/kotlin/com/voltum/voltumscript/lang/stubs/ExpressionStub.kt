package com.voltum.voltumscript.lang.stubs

import com.intellij.lang.ASTNode
import com.intellij.lang.FileASTNode
import com.intellij.psi.stubs.IStubElementType
import com.voltum.voltumscript.ext.ancestors
import com.voltum.voltumscript.parser.VALUE_ITEMS
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumTypes.BLOCK_BODY
import com.voltum.voltumscript.psi.VoltumTypes.FUNC_DECLARATION


class VoltumExprStubType<PsiT : VoltumElement>(
    debugName: String,
    psiCtor: (VoltumPlaceholderStub<*>, IStubElementType<*, *>) -> PsiT
) : VoltumPlaceholderStub.Type<PsiT>(debugName, psiCtor) {
    override fun shouldCreateStub(node: ASTNode): Boolean {
        val shouldCreate = shouldCreateExprStub(node)
        return shouldCreate
    }
}

fun shouldCreateExprStub(node: ASTNode): Boolean {
    val element = node.ancestors.firstOrNull {
        val parent = it.treeParent
        parent?.elementType in VALUE_ITEMS || parent is FileASTNode
    }
    return element != null && !element.isFunctionBody() && createStubIfParentIsStub(node)
}

private fun ASTNode.isFunctionBody() = this.elementType == BLOCK_BODY && treeParent?.elementType == FUNC_DECLARATION