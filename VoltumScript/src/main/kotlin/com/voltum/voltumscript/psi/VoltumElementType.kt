package com.voltum.voltumscript.psi

import com.intellij.lang.ASTNode
import com.intellij.psi.impl.source.tree.LeafPsiElement
import com.intellij.psi.tree.ICompositeElementType
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.parser.compositeNodeFactory

class VoltumElementType(debugName: String) : IElementType(debugName, VoltumLanguage), ICompositeElementType {
    override fun createCompositeNode(): ASTNode {
        return compositeNodeFactory(this)
    }
}