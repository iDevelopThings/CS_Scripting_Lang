package com.voltum.voltumscript.ide

import com.intellij.codeInsight.editorActions.SimpleTokenSetQuoteHandler
import com.voltum.voltumscript.psi.VoltumTypes

class VoltumQuoteHandler : SimpleTokenSetQuoteHandler(VoltumTypes.STRING_LITERAL)