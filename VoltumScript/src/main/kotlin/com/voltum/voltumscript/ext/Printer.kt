package com.voltum.voltumscript.ext

import com.intellij.codeInsight.documentation.DocumentationManagerUtil
import com.intellij.openapi.editor.DefaultLanguageHighlighterColors
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.project.DumbService
import com.voltum.voltumscript.ide.documentation.appendStyledLinkFragment
import com.voltum.voltumscript.psi.VoltumElement
import com.voltum.voltumscript.psi.VoltumIdent
import kotlin.contracts.InvocationKind
import kotlin.contracts.contract

@Suppress("SameParameterValue")
open class Printer : CharSequence {
    val stringBuilder: StringBuilder = StringBuilder()
    var currentIndent = 0
    private val indentSymbol = " ".repeat(2)
    private var lastSymbolIsLineBreak = false

    override fun toString(): String = stringBuilder.toString()

    override val length: Int get() = stringBuilder.length
    override operator fun get(index: Int): Char = stringBuilder[index]
    override fun subSequence(startIndex: Int, endIndex: Int): CharSequence = stringBuilder.subSequence(startIndex, endIndex)

    private fun append(value: Char) {
        if (value == '\n') {
            lastSymbolIsLineBreak = true
        } else {
            lastSymbolIsLineBreak = false
        }
        stringBuilder.append(value)
    }

    private fun append(value: String) {
        value.forEach { append(it) }
    }

    fun writeIndent() {
        repeat(currentIndent) {
            append(indentSymbol)
        }
    }

    fun a(value: Any): Printer {
        a(value.toString())
        return this
    }

    fun a(value: String): Printer {
        if (value.isEmpty())
            return this
        if (lastSymbolIsLineBreak) {
            writeIndent()
        }
        append(value)
        return this
    }

    fun ln(value: String) {
        a(value)
        ln()
    }

    fun ln() {
        append('\n')
    }

    inline fun i(block: () -> Unit) {
        currentIndent++
        block()
        currentIndent--
    }

    inline fun b(body: () -> Unit) {
        i(body)
    }

    inline fun b(parens: ParenthesisKind, body: () -> Unit) {
        par(parens) {
            i(body)
        }
    }

    inline fun par(kind: ParenthesisKind = ParenthesisKind.ROUND, body: () -> Unit) {
        a(kind.open)
        body()
        a(kind.close)
    }

    inline fun <T> list(list: List<T>, separator: String = ", ", renderElement: (T) -> Unit) =
        list(list, { this.a(separator) }, renderElement)

    inline fun <T> verticalList(
        list: List<T>,
        title: String,
        renderElement: (T) -> Unit
    ) {
        verticalList(list, title, 1, true, renderElement)
    }

    inline fun <T> verticalList(
        list: List<T>,
        title: String,
        lineSeparatorNum: Int,
        lineBreakOnEnd: Boolean,
        renderElement: (T) -> Unit
    ) {
        if (list.isEmpty()) {
            return
        }
        this.ln(title)
        this.i {
            list(
                list,
                { repeat(lineSeparatorNum) { this.a("\n") } },
                {
                    this.a(" - ")
                    renderElement(it)
                }
            )
            if (lineBreakOnEnd) {
                this.ln()
            }
        }
    }

    inline fun <T> verticalList(list: List<T>, renderElement: (T) -> Unit) {
        this.i {
            if (list.isEmpty()) {
                this.ln(" - empty")
            } else {
                list(
                    list,
                    { this.a("\n") },
                    { renderElement(it) }
                )
            }
        }
    }

    inline fun <T> list(list: List<T>, separator: () -> Unit, renderElement: (T) -> Unit) {
        if (list.isEmpty()) return
        renderElement(list.first())
        for (element in list.subList(1, list.size)) {
            separator()
            renderElement(element)
        }
    }

    enum class ParenthesisKind(val open: String, val close: String) {
        ROUND("(", ")"),
        CURVED("{", "}"),
        ANGLE("<", ">")
    }
}

public inline fun buildPrinter(builderAction: Printer.() -> Unit): String {
    contract { callsInPlace(builderAction, InvocationKind.EXACTLY_ONCE) }
    return Printer().apply(builderAction).toString()
}

fun <T> Printer.typeParameterList(typeParameters: List<T>, renderElement: (T) -> Unit = { a(it.toString()) }) {
    if (typeParameters.isEmpty())
        return

    par(Printer.ParenthesisKind.ANGLE) {
        list(typeParameters) {
            renderElement(it)
        }
    }
}

fun Printer.addElementLink(element: VoltumElement, renderElement: (String) -> Unit = { a(it) }) {
    val linkText = element.text
    if (DumbService.isDumb(element.project)) {
        renderElement(linkText)
        return
    }

    val sb = StringBuilder()
    DocumentationManagerUtil.createHyperlink(
        sb,
        element,
        (element as VoltumIdent).documentationUrl,
        buildPrinter {
            appendStyledLinkFragment(
                linkText,
                TextAttributes().apply {
                    foregroundColor = EditorColorsManager.getInstance().globalScheme.getColor(DefaultLanguageHighlighterColors.DOC_COMMENT_LINK)
                }
            )
        },
        false,
        true
    )

    renderElement(sb.toString())
}