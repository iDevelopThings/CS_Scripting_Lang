package com.voltum.voltumscript.parser

import com.intellij.lang.PsiBuilder
import com.intellij.lang.parser.GeneratedParserUtilBase
import com.intellij.psi.tree.IElementType


inline fun <T> PsiBuilder.probe(action: () -> T): T {
    val mark = mark()
    try {
        return action()
    } finally {
        mark.rollbackTo()
    }
}

inline fun PsiBuilder.rollbackIfFalse(action: () -> Boolean): Boolean {
    val mark = mark()
    return if (action()) {
        true
    } else {
        mark.rollbackTo()
        false
    }
}

fun PsiBuilder.Marker.close(result: Boolean): Boolean {
    if (result) {
        drop()
    } else {
        rollbackTo()
    }
    return result
}

fun PsiBuilder.clearFrame() {
    val state = GeneratedParserUtilBase.ErrorState.get(this)
    val currentFrame = state.currentFrame
    if (currentFrame != null) {
        currentFrame.errorReportedAt = -1
        currentFrame.lastVariantAt = -1
    }
}

/** Similar to [com.intellij.lang.PsiBuilderUtil.rawTokenText] */
fun PsiBuilder.rawLookupText(steps: Int): CharSequence {
    val start = rawTokenTypeStart(steps)
    val end = rawTokenTypeStart(steps + 1)
    return if (start == -1 || end == -1) "" else originalText.subSequence(start, end)
}

fun PsiBuilder.expect(expectedType: IElementType): Boolean {
    if (tokenType === expectedType) {
        advanceLexer()
        return true
    }
    return false
}

fun PsiBuilder.expect(expectedType: IElementType, message: String): Boolean {
    if (tokenType === expectedType) {
        advanceLexer()
        return true
    }
    error(message)
    return false
}

fun PsiBuilder.isToken(token: IElementType): Boolean = tokenType === token
fun PsiBuilder.isToken(vararg tokens: IElementType): Boolean = tokens.any { tokenType === it }


/*fun PsiBuilder.markerScope(action: (PsiBuilder.Marker) -> Unit): PsiBuilder.Marker {
    val mark = mark()
    try {
        action(mark)
    } finally {

    }
    return mark
}
fun PsiBuilder.markerScope(action: (PsiBuilder.Marker) -> IElementType): PsiBuilder.Marker {
    val mark = mark()
    var resultElementType: IElementType? = null
    try {
        resultElementType = action(mark)
    } finally {
        if (resultElementType != null) {
            mark.done(resultElementType)
        } else {
            mark.drop()
        }
    }
    return mark
}
fun PsiBuilder.markerScopeBool(action: (PsiBuilder.Marker) -> Boolean): PsiBuilder.Marker {
    val mark = mark()
    return if (action(mark)) {
        mark.drop()
        mark
    } else {
        mark.rollbackTo()
        mark
    }
}*/

