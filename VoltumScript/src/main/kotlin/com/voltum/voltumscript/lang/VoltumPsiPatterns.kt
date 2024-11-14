package com.voltum.voltumscript.lang


import com.intellij.patterns.*
import com.intellij.patterns.PlatformPatterns.psiElement
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiErrorElement
import com.intellij.psi.TokenType
import com.intellij.psi.tree.IElementType
import com.intellij.psi.util.elementType
import com.intellij.psi.util.prevLeaf
import com.intellij.util.ProcessingContext
import com.voltum.voltumscript.psi.*
import com.voltum.voltumscript.psi.ext.leftLeaves
import com.voltum.voltumscript.psi.ext.leftSiblings
import com.voltum.voltumscript.psi.ext.skipWhitespacesBackwardWithoutNewLines

object VoltumPsiPatterns {
    val error = psiElement<PsiErrorElement>()
    val whitespace = psiElement().whitespace()

    val newLine: PsiElementPattern.Capture<PsiElement> = psiElement().with("newLine") { element ->
        element.elementType == TokenType.WHITE_SPACE && element.textContains('\n')
    }

    val topKeyword = tokenElement(VoltumTypes.ID, "topKeyword")
        .withLanguage(VoltumLanguage)
        // .andNot(psiElement().withPrevSiblingSkipping(whitespace, error))
        .withParent(VoltumFile::class.java)

    /**
     * check if we're inside some kind of block, for ex;
     * function x() { THIS }
     * or
     * if (x) { THIS }
     */
    val insideBlock = tokenElement(VoltumTypes.ID, "topKeyword")
        .withLanguage(VoltumLanguage)
        .withParent(VoltumVarReference::class.java)
        .withSuperParent(2, VoltumPath::class.java)
        .withSuperParent(3, VoltumStatement::class.java)
        .withSuperParent(4, VoltumBlockBody::class.java)

    val pathExpr = tokenElement(VoltumTypes.ID, "topKeyword")
        .withParent(
            psiElement(VoltumTypes.PATH) or psiElement(VoltumTypes.VAR_REFERENCE)
        )
    

    /**
     * A simple type name reference... for ex, in structs:
     *
     * ```
     * type A struct {
     *    b Object
     * }
     * ```
     * `Object` would be a type reference
     * `A` is also a type id, but is also a type reference
     *
     * Could also be for function args:
     * ```
     * function a(b Object) {}
     * ```
     * `Object` would be a type reference
     *
     */
    val typeReference = psiElement().andOr(
        psiElement(VoltumTypes.TYPE_ID),
        psiElement(VoltumTypes.FUNC_ID),
        psiElement(VoltumTypes.TYPE_REF)
    )
    
    val callExpr = psiElement(VoltumTypes.CALL_EXPR)

    /**
     * This would be a path like `a.b.c`
     */
    val qualifiedPathExpr = psiElement(VoltumTypes.PATH).with("withQualifier") { el ->
        return@with el is VoltumPath && el.qualifier != null
    }

    /**
     * This would be a path like `a`
     */
    val unqualifiedPathExpr = psiElement(VoltumTypes.VAR_REFERENCE).with("withNoQualifier") { el ->
        return@with el is VoltumVarReference && el.parent is VoltumPath && (el.parent as VoltumPath).qualifier == null
    }
    
    val variableDeclaration = psiElement(VoltumTypes.VAR_ID)
        .withSuperParent(2, VoltumVariableDeclaration::class.java)
}


fun <T : PsiElement, Self : ObjectPattern<T, Self>> ObjectPattern<T, Self>.afterNewLine(): Self =
    with("afterNewLine") { element, context ->
        val prev = element.skipWhitespacesBackwardWithoutNewLines() ?: return@with true
        VoltumPsiPatterns.newLine.accepts(prev, context)
    }

fun <T : PsiElement, Self : ObjectPattern<T, Self>> ObjectPattern<T, Self>.afterLeafNewLine(): Self =
    with("afterLeafNewLine") { element, context ->
        val prev = element.prevLeaf() ?: return@with true
        VoltumPsiPatterns.newLine.accepts(prev, context)
    }

fun <T : PsiElement, Self : ObjectPattern<T, Self>, P : PsiElement> ObjectPattern<T, Self>.afterSiblingNewLinesAware(
    pattern: PsiElementPattern.Capture<P>
): Self =
    with("afterSiblingWithoutNewLines") { element, context ->
        val prev = element.skipWhitespacesBackwardWithoutNewLines() ?: return@with true
        pattern.accepts(prev, context)
    }

fun <T : Any, Self : ObjectPattern<T, Self>> ObjectPattern<T, Self>.with(name: String, cond: (T) -> Boolean): Self =
    with(object : PatternCondition<T>(name) {
        override fun accepts(t: T, context: ProcessingContext?): Boolean = cond(t)
    })

fun <T : Any, Self : ObjectPattern<T, Self>> ObjectPattern<T, Self>.with(
    name: String,
    cond: (T, ProcessingContext?) -> Boolean
): Self = with(object : PatternCondition<T>(name) {
    override fun accepts(t: T, context: ProcessingContext?): Boolean = cond(t, context)
})


inline fun <reified I : PsiElement> psiElement(): PsiElementPattern.Capture<I> {
    return psiElement(I::class.java)
}

fun tokenElement(type: IElementType, contextName: String): PsiElementPattern.Capture<PsiElement> {
    return psiElement().withElementType(type).with("putIntoContext") { e, context ->
        context?.put(contextName, e)
        true
    }
}

inline fun <reified I : PsiElement> psiElement(contextName: String): PsiElementPattern.Capture<I> {
    return psiElement(I::class.java).with("putIntoContext") { e, context ->
        context?.put(contextName, e)
        true
    }
}

inline fun <reified I : PsiElement> PsiElementPattern.Capture<PsiElement>.withSuperParent(level: Int): PsiElementPattern.Capture<PsiElement> {
    return this.withSuperParent(level, I::class.java)
}

inline infix fun <reified I : PsiElement> ElementPattern<out I>.or(pattern: ElementPattern<out I>): PsiElementPattern.Capture<I> {
    return psiElement<I>().andOr(this, pattern)
}

/**
 * Similar with [TreeElementPattern.afterSiblingSkipping]
 * but it uses [PsiElement.getPrevSibling] to get previous sibling elements
 * instead of [PsiElement.getChildren].
 */
fun <T : PsiElement, Self : PsiElementPattern<T, Self>> PsiElementPattern<T, Self>.withPrevSiblingSkipping(
    skip: ElementPattern<out PsiElement>,
    pattern: ElementPattern<out T>
): Self = with("withPrevSiblingSkipping") { e ->
    val sibling = e.leftSiblings.dropWhile { skip.accepts(it) }
        .firstOrNull() ?: return@with false
    pattern.accepts(sibling)
}

fun <T : PsiElement, Self : PsiElementPattern<T, Self>> PsiElementPattern<T, Self>.withPrevLeafSkipping(
    skip: ElementPattern<out PsiElement>,
    pattern: ElementPattern<out T>
): Self = with("withPrevSiblingSkipping") { e ->
    val sibling = e.leftLeaves.dropWhile { skip.accepts(it) }
        .firstOrNull() ?: return@with false
    pattern.accepts(sibling)
}

