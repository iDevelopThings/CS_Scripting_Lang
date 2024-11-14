package com.voltum.voltumscript.ide.completion

import com.intellij.codeInsight.lookup.LookupElement
import com.intellij.codeInsight.lookup.LookupElementPresentation
import com.intellij.psi.PsiElement
import com.intellij.psi.util.PsiTreeUtil
import com.voltum.voltumscript.VoltumTestCase
import org.intellij.lang.annotations.Language

abstract class VoltumCompletionTestBase(private val defaultFileName: String = "main.vlt") : VoltumTestCase() {

    protected lateinit var completionFixture: VoltumCompletionTestFixture

    override fun setUp() {
        super.setUp()
        completionFixture = VoltumCompletionTestFixture(myFixture, defaultFileName)
        completionFixture.setUp()
    }

    override fun tearDown() {
        completionFixture.tearDown()
        super.tearDown()
    }

    // Prefer using `doSingleCompletion` instead
    @Deprecated(
        "Use doSingleCompletion, because it's simpler and checks caret position as well",
        replaceWith = ReplaceWith("doSingleCompletion(code, code)")
    )
    protected fun checkSingleCompletion(target: String, @Language("Voltum") code: String) {
        InlineFile(code).withCaret()
        executeSoloCompletion()

        val normName = target
            .substringBeforeLast("()")
            .substringBeforeLast(" {}")
            .substringAfterLast("::")
            .substringAfterLast(".")

        val shift = when {
            target.endsWith("()") || target.endsWith("::") -> 3
            target.endsWith(" {}") -> 4
            else -> 2
        }
        val element = myFixture.file.findElementAt(myFixture.caretOffset - shift)!!
        val skipTextCheck = normName.isEmpty() || normName.contains(' ')
        check((skipTextCheck || element.text == normName) && (element.fitsHierarchically(target) || element.fitsLinearly(target))) {
            "Wrong completion, expected `$target`, but got\n${myFixture.file.text}"
        }
    }

    protected fun doFirstCompletion(
        @Language("Voltum") before: String,
        @Language("Voltum") after: String
    ) = completionFixture.doFirstCompletion(before, after)

    protected fun doSingleCompletion(
        @Language("Voltum") before: String,
        @Language("Voltum") after: String
    ) = completionFixture.doSingleCompletion(before, after)

    protected fun doSingleCompletionWithLiveTemplate(
        @Language("Voltum") before: String,
        toType: String,
        @Language("Voltum") after: String
    ) = checkByTextWithLiveTemplate(before, after, toType) {
        executeSoloCompletion()
    }

    protected fun checkContainsCompletion(
        variant: String,
        @Language("Voltum") code: String,
        render: LookupElement.() -> String = { lookupString }
    ) = completionFixture.checkContainsCompletion(code, listOf(variant), render)

    protected fun checkContainsCompletion(
        variants: List<String>,
        @Language("Voltum") code: String,
        render: LookupElement.() -> String = { lookupString }
    ) = completionFixture.checkContainsCompletion(code, variants, render)

    protected fun checkContainsCompletionPrefixes(
        prefixes: List<String>,
        @Language("Voltum") code: String
    ) = completionFixture.checkContainsCompletionPrefixes(code, prefixes)

    protected fun checkCompletion(
        lookupString: String,
        @Language("Voltum") before: String,
        @Language("Voltum") after: String,
        completionChar: Char = '\n'
    ) = completionFixture.checkCompletion(lookupString, before, after, completionChar)

    protected fun checkCompletion(
        lookupString: String,
        tailText: String,
        @Language("Voltum") before: String,
        @Language("Voltum") after: String,
        completionChar: Char = '\n'
    ) = completionFixture.checkCompletion(lookupString, tailText, before, after, completionChar)

    fun checkCompletionWithLiveTemplate(
        lookupString: String,
        @Language("Voltum") before: String,
        toType: String,
        @Language("Voltum") after: String
    ) {
        checkByTextWithLiveTemplate(before.trimIndent(), after.trimIndent(), toType) {
            val items = myFixture.completeBasic()!!
            val lookupItem = items.find {
                it.presentation.itemText == lookupString
            } ?: error("Lookup string $lookupString not found")
            myFixture.lookup.currentItem = lookupItem
            myFixture.type('\n')
        }
    }

    protected fun checkNotContainsCompletion(
        variant: String,
        @Language("Voltum") code: String,
        render: LookupElement.() -> String = { lookupString }
    ) = completionFixture.checkNotContainsCompletion(code, setOf(variant), render)

    protected fun checkNotContainsCompletion(
        variants: Set<String>,
        @Language("Voltum") code: String,
        render: LookupElement.() -> String = { lookupString }
    ) = completionFixture.checkNotContainsCompletion(code, variants, render)

    protected fun checkNotContainsCompletion(
        variants: List<String>,
        @Language("Voltum") code: String,
        render: LookupElement.() -> String = { lookupString }
    ) {
        completionFixture.checkNotContainsCompletion(code, variants.toSet(), render)
    }

    protected open fun checkNoCompletion(@Language("Voltum") code: String) = completionFixture.checkNoCompletion(code)

    protected fun executeSoloCompletion() = completionFixture.executeSoloCompletion()

    private fun PsiElement.fitsHierarchically(target: String): Boolean = when {
        text == target -> true
        text.length > target.length -> false
        parent != null -> parent.fitsHierarchically(target)
        else -> false
    }

    private fun PsiElement.fitsLinearly(target: String) =
        checkLinearly(target, Direction.LEFT) || checkLinearly(target, Direction.RIGHT)

    private fun PsiElement.checkLinearly(target: String, direction: Direction): Boolean {
        var el = this
        var text = ""
        while (text.length < target.length) {
            text = if (direction == Direction.LEFT) el.text + text else text + el.text
            if (text == target) return true
            el = (if (direction == Direction.LEFT) PsiTreeUtil.prevVisibleLeaf(el) else PsiTreeUtil.nextVisibleLeaf(el)) ?: break
        }
        return false
    }

    private enum class Direction {
        LEFT,
        RIGHT
    }
}

val LookupElement.presentation: LookupElementPresentation
    get() = LookupElementPresentation().also { renderElement(it) }
