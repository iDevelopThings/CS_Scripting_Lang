package com.voltum.voltumscript.ext

import com.intellij.lang.ASTNode
import com.intellij.lang.FileASTNode

/** Iterates all children of [this] node and invokes [action] for each one */
inline fun ASTNode.forEachChild(action: (ASTNode) -> Unit) {
    var treeChild: ASTNode? = firstChildNode

    while (treeChild != null) {
        action(treeChild)
        treeChild = treeChild.treeNext
    }
}

val ASTNode.ancestors: Sequence<ASTNode>
    get() = generateSequence(this) {
        if (it is FileASTNode) null else it.treeParent
    }
