package com.voltum.voltumscript.ide.formatting

import com.intellij.psi.codeStyle.CodeStyleSettings
import com.intellij.psi.codeStyle.CommonCodeStyleSettings
import com.intellij.psi.codeStyle.CustomCodeStyleSettings
import com.voltum.voltumscript.lang.VoltumLanguage


val CodeStyleSettings.common: CommonCodeStyleSettings
    get() = getCommonSettings(VoltumLanguage)

val CodeStyleSettings.voltum: VoltumCodeStyleSettings
    get() = getCustomSettings(VoltumCodeStyleSettings::class.java)

class VoltumCodeStyleSettings(container: CodeStyleSettings) : CustomCodeStyleSettings(VoltumCodeStyleSettings::class.java.simpleName, container) {

    @JvmField
    var ALIGN_RET_TYPE: Boolean = true

    @JvmField
    var ALIGN_TYPE_PARAMS: Boolean = false
    
    @JvmField
    var MIN_NUMBER_OF_BLANKS_BETWEEN_ITEMS: Int = 1
}