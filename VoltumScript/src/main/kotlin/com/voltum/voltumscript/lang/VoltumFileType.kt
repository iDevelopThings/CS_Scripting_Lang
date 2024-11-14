package com.voltum.voltumscript.lang

import com.intellij.openapi.fileTypes.LanguageFileType
import com.voltum.voltumscript.Constants
import com.voltum.voltumscript.Icons
import javax.swing.Icon


class VoltumFileType : LanguageFileType(VoltumLanguage) {
    override fun getName(): String = Constants.NAME
    override fun getDescription(): String = Constants.NAME + " file"
    override fun getDefaultExtension(): String = Constants.FILE_EXTENSION
    override fun getIcon(): Icon = Icons.Logo

    companion object {
        val INSTANCE = VoltumFileType()
    }
}

