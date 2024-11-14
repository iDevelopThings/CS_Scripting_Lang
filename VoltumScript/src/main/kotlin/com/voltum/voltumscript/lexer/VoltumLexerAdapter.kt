package com.voltum.voltumscript.lexer

import com.intellij.lexer.FlexAdapter
import com.voltum.voltumscript.VoltumLexer

class VoltumLexerAdapter : FlexAdapter(VoltumLexer(null)) 