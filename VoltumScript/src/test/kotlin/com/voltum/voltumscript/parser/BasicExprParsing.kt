package com.voltum.voltumscript.parser

import com.intellij.testFramework.TestDataPath

@TestDataPath("\$CONTENT_ROOT/testData/parser/fixtures/expressions")
class BasicExprParsing : VoltumParsingTestCaseBase("expressions") {
    fun `test quoted expr`() = doTest(true, true)
    fun `test unquoted expr`() = doTest(true, true)

    fun `test assign`() = doTest(true, true)
    fun `test comp`() = doTest(true, true)
    fun `test rel_comp`() = doTest(true, true)
    fun `test bit_shift`() = doTest(true, true)
    fun `test add`() = doTest(true, true)
    fun `test mul`() = doTest(true, true)
    fun `test prefix`() = doTest(true, true)
    fun `test postfix`() = doTest(true, true)
    fun `test unary`() = doTest(true, true)

    fun `test call`() = doTest(true, true)
    fun `test call with args`() = doTest(true, true)
    fun `test await call`() = doTest(true, true)
    fun `test call with type args`() = doTest(true, true)
    
    fun `test index access`() = doTest(true, true)

    fun `test dictionary`() = doTest(true, true)
    fun `test list`() = doTest(true, true)
    fun `test tuple`() = doTest(true, true)

    fun `test for while`() = doTest(true, true)
    fun `test for i`() = doTest(true, true)
    fun `test for i, with predefined var`() = doTest(true, true)
    fun `test for range`() = doTest(true, true)
    fun `test for range with tuple`() = doTest(true, true)

    fun `test function decl`() = doTest(true, true)
    fun `test async function decl`() = doTest(true, true)

    fun `test signal decl`() = doTest(true, true)

}