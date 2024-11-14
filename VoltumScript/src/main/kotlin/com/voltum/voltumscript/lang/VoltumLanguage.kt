package com.voltum.voltumscript.lang

import com.intellij.lang.Language
import com.voltum.voltumscript.Constants

object VoltumLanguage : Language(Constants.NAME) {
    private fun readResolve(): Any = VoltumLanguage
    override fun isCaseSensitive() = true
    override fun getDisplayName() = Constants.NAME
}