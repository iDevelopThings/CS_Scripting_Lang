package com.voltum.voltumscript.ide.completion

import com.intellij.codeInsight.completion.InsertionContext
import com.intellij.psi.PsiElement
import com.intellij.psi.util.PsiTreeUtil

inline fun <reified T : PsiElement> InsertionContext.getElementOfType(strict: Boolean = false): T? =
    PsiTreeUtil.findElementOfClassAtOffset(file, tailOffset - 1, T::class.java, strict)

private fun CharSequence.indexOfSkippingSpace(c: Char, startIndex: Int): Int? {
    for (i in startIndex until this.length) {
        val currentChar = this[i]
        if (c == currentChar) return i
        if (currentChar != ' ' && currentChar != '\t') return null
    }
    return null
}

fun InsertionContext.nextCharIs(c: Char): Boolean =
    document.charsSequence.indexOfSkippingSpace(c, tailOffset) != null

val InsertionContext.alreadyHasCallParens: Boolean
    get() = nextCharIs('(')

val InsertionContext.alreadyHasAngleBrackets: Boolean
    get() = nextCharIs('<')

val InsertionContext.alreadyHasBraces: Boolean
    get() = nextCharIs('{')

fun InsertionContext.doNotAddOpenParenCompletionChar() {
    if (completionChar == '(') {
        setAddCompletionChar(false)
    }
}