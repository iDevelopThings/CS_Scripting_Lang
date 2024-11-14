package com.voltum.voltumscript.ide.completion

class TypeBasedCompletionTest : VoltumCompletionTestBase() {
    fun `test method call`() = doSingleCompletion(
        """
        type S struct {
            testMethod() {}
        }
        var s = new<S>()
        s.tes/*caret*/
    """, """
        type S struct {
            testMethod() {}
        }
        var s = new<S>()
        s.testMethod()/*caret*/
    """
    )

}