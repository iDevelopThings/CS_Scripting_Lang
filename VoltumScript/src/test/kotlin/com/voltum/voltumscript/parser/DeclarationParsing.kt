package com.voltum.voltumscript.parser

class DeclarationParsing : VoltumParsingTestCaseBase("declarations") {
    fun `test struct declaration`() = doTest(true, true)
    fun `test struct declaration with attributes`() = doTest(true, true)
}