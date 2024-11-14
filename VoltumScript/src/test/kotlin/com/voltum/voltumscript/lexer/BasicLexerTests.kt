package com.voltum.voltumscript.lexer

import com.intellij.lexer.Lexer
import java.util.*

class BasicLexerTests : VoltumLexingTestCaseBase() {
    override fun getTestDataPath(): String = "lexer/fixtures"

    override fun createLexer(): Lexer = VoltumLexerAdapter()

    fun `test handling comparison exprs`() = doTest()

}
