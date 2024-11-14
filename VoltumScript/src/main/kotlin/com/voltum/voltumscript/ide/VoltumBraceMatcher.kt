package com.voltum.voltumscript.ide

import com.intellij.lang.BracePair
import com.intellij.lang.PairedBraceMatcher
import com.intellij.psi.PsiFile
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.parser.Braces

class VoltumBraceMatcher : PairedBraceMatcher {
    override fun getPairs(): Array<BracePair> =
        Braces.pairs().toTypedArray()

    override fun isPairedBracesAllowedBeforeType(lbraceType: IElementType, contextType: IElementType?): Boolean =
        true

    override fun getCodeConstructStart(file: PsiFile?, openingBraceOffset: Int): Int =
        openingBraceOffset
}
