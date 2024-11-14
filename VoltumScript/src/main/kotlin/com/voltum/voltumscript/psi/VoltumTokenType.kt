package com.voltum.voltumscript.psi

import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.lang.VoltumLanguage

open class VoltumTokenType(debugName: String) : IElementType(debugName, VoltumLanguage)