package com.voltum.voltumscript.ide.documentation

import com.intellij.lang.Language
import com.intellij.lang.documentation.DocumentationMarkup
import com.intellij.lang.documentation.QuickDocHighlightingHelper
import com.intellij.lang.documentation.*
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledCodeBlock
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledCodeFragment
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledFragment
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledInlineCode
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledLinkFragment
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendStyledSignatureFragment
import com.intellij.lang.documentation.QuickDocHighlightingHelper.appendWrappedWithInlineCodeTag
import com.intellij.lang.documentation.QuickDocHighlightingHelper.wrapWithInlineCodeTag
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.editor.markup.TextAttributes
import com.intellij.openapi.progress.ProcessCanceledException
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.util.text.StringUtil
import com.intellij.psi.PsiComment
import com.intellij.psi.PsiElement
import com.intellij.psi.PsiWhiteSpace
import com.intellij.psi.util.PsiTreeUtil
import com.intellij.psi.util.elementType
import com.intellij.psi.util.parentOfType
import com.voltum.voltumscript.ext.Printer
import com.voltum.voltumscript.lang.VoltumLanguage
import com.voltum.voltumscript.parser.COMMENTS
import com.voltum.voltumscript.parser.VoltumTokenTypes
import com.voltum.voltumscript.psi.VoltumDeclaration
import com.voltum.voltumscript.psi.VoltumDocumentationOwner
import com.voltum.voltumscript.psi.VoltumStatement
import com.voltum.voltumscript.psi.VoltumTypeDeclarationFieldMember
import com.voltum.voltumscript.psi.VoltumTypeDeclarationMethodMember
import com.voltum.voltumscript.psi.VoltumVariableDeclaration
import com.voltum.voltumscript.psi.ext.skipWhitespacesBackwardWithoutNewLines
import com.voltum.voltumscript.psi.ext.skipWhitespacesForwardWithoutNewLines
import org.intellij.markdown.IElementType
import org.intellij.markdown.MarkdownElementTypes
import org.intellij.markdown.flavours.MarkdownFlavourDescriptor
import org.intellij.markdown.flavours.gfm.GFMFlavourDescriptor
import org.intellij.markdown.html.GeneratingProvider
import org.intellij.markdown.html.HtmlGenerator
import org.intellij.markdown.parser.LinkMap
import org.intellij.markdown.parser.MarkdownParser
import java.net.URI

const val NO_WRAP = "white-space: nowrap"
const val SECTION_START_NO_WRAP = "<td valign='top' style='$NO_WRAP'>"

fun stripDocCommentBlockPrefixes(text: String): String {
    /** Doc comments are in the style of :
     * ```
     * /**
     *  * This is a doc comment
     *  */
     * ```
     * So we need to remove the leading `/**` and trailing `*/` and all leading `*` characters
     */
    return text.removePrefix("/**")
        .removeSuffix("*/")
        .trim()
        .replace(Regex("^\\s*\\*"), "")
        .trim()
}

fun stripAllCommentPrefixes(text: String): String {
    // should use `stripDocCommentBlockPrefixes` and `also remove `//` comments
    val t = text
        .replace(Regex("^\\s*//"), "")
        .replace(Regex("^\\s*\\*"), "")
        .trim()
    
    return stripDocCommentBlockPrefixes(t)
}

fun Printer.definition(block: Printer.() -> Unit) {
    a(DocumentationMarkup.DEFINITION_START)
    block()
    a(DocumentationMarkup.DEFINITION_END)
    a("\n")
}

fun Printer.sections(block: Printer.() -> Unit) {
    a(DocumentationMarkup.SECTIONS_START)
    block()
    a(DocumentationMarkup.SECTIONS_END)
    a("\n")
}

fun Printer.section(header: String, block: Printer.() -> Unit) {
    a(DocumentationMarkup.SECTION_HEADER_START)
    a(header)
    a(DocumentationMarkup.SECTION_END)
    block()
    if (!endsWith(DocumentationMarkup.SECTION_END)) {
        a(DocumentationMarkup.SECTION_END)
    }
    a("</tr>")
    a("\n")
}

fun Printer.cell(noWrap: Boolean = true, block: Printer.() -> Unit) {
    a(if (noWrap) SECTION_START_NO_WRAP else DocumentationMarkup.SECTION_START)
    block()
    if (!endsWith(DocumentationMarkup.SECTION_END)) {
        a(DocumentationMarkup.SECTION_END)
    }
}

fun Printer.content(block: Printer.() -> Unit) {
    a(DocumentationMarkup.CONTENT_START)
    block()
    a(DocumentationMarkup.CONTENT_END)
    a("\n")
}

fun Printer.cellDivider() {
    a(DocumentationMarkup.SECTION_CONTENT_CELL.style("padding-left: 10px"))
}

fun Printer.appendStyledCodeBlock(project: Project, language: Language?, code: @NlsSafe CharSequence): @NlsSafe Printer =
    apply { stringBuilder.appendStyledCodeBlock(project, language, code) }

fun Printer.appendStyledInlineCode(project: Project, language: Language?, @NlsSafe code: String): Printer =
    apply { stringBuilder.appendStyledInlineCode(project, language, code) }

fun Printer.appendStyledCodeFragment(project: Project, language: Language, @NlsSafe code: String): Printer =
    apply { stringBuilder.appendStyledCodeFragment(project, language, code) }

fun Printer.appendStyledLinkFragment(contents: String, textAttributes: TextAttributes): Printer =
    apply { stringBuilder.appendStyledLinkFragment(contents, textAttributes) }

fun Printer.appendStyledLinkFragment(contents: String, textAttributesKey: TextAttributesKey): Printer =
    apply { stringBuilder.appendStyledLinkFragment(contents, textAttributesKey) }

fun Printer.appendStyledSignatureFragment(contents: String, textAttributes: TextAttributes): Printer =
    apply { stringBuilder.appendStyledSignatureFragment(contents, textAttributes) }

fun Printer.appendStyledSignatureFragment(contents: String, textAttributesKey: TextAttributesKey): Printer =
    apply { stringBuilder.appendStyledSignatureFragment(contents, textAttributesKey) }

fun Printer.appendStyledSignatureFragment(project: Project, language: Language?, code: String): Printer =
    apply { stringBuilder.appendStyledSignatureFragment(project, language, code) }

fun Printer.appendStyledFragment(contents: String, textAttributes: TextAttributes): Printer =
    apply { stringBuilder.appendStyledFragment(contents, textAttributes) }

fun Printer.appendStyledFragment(contents: String, textAttributesKey: TextAttributesKey): Printer =
    apply { stringBuilder.appendStyledFragment(contents, textAttributesKey) }

fun Printer.appendWrappedWithInlineCodeTag(@NlsSafe contents: CharSequence): @NlsSafe Printer =
    apply { stringBuilder.appendWrappedWithInlineCodeTag(contents) }

fun Printer.wrapWithInlineCodeTag(): @NlsSafe Printer =
    apply { stringBuilder.wrapWithInlineCodeTag() }

fun Printer.documentationComment(element: PsiElement?, renderFn: Printer.(String) -> Unit = { content { a(it) } }) {
    var docElement: VoltumDocumentationOwner? = element as? VoltumDocumentationOwner

    val elementTest = PsiTreeUtil.findFirstParent(element) {
        it is VoltumStatement ||
                it is VoltumDeclaration ||
                it is VoltumVariableDeclaration ||
                it is VoltumTypeDeclarationFieldMember ||
                it is VoltumTypeDeclarationMethodMember
    }
    if (elementTest != null) {
        docElement = elementTest as VoltumDocumentationOwner
    }

    val comment = docElement?.docComment ?: return
    val rendered = VoltumDocumentationRenderer(comment).render() ?: return
    renderFn(rendered)

//    content { a(rendered) }
}

fun toHtml(project: Project, text: String): String {
    return try {
        QuickDocHighlightingHelper.getStyledCodeFragment(project, VoltumLanguage, text.replace("\t", "  "))
    } catch (e: ProcessCanceledException) {
        throw e
    } catch (e: Exception) {
        text
    }
}

@NlsSafe
fun documentationMarkdownToHtml(markdown: String?): String? {
    if (markdown.isNullOrBlank()) {
        return null
    }

    val flavour = VoltumMarkdownFlavourDescriptor()
    val root = MarkdownParser(flavour).buildMarkdownTreeFromString(markdown)
    return HtmlGenerator(markdown, root, flavour).generateHtml()
}

private class VoltumMarkdownFlavourDescriptor(
    private val base: MarkdownFlavourDescriptor = GFMFlavourDescriptor(
        useSafeLinks = false,
        absolutizeAnchorLinks = true
    ),
) : MarkdownFlavourDescriptor by base {

    override fun createHtmlGeneratingProviders(linkMap: LinkMap, baseURI: URI?): Map<IElementType, GeneratingProvider> {
        val generatingProviders = HashMap(base.createHtmlGeneratingProviders(linkMap, null))
        // Filter out MARKDOWN_FILE to avoid producing unnecessary <body> tags
        generatingProviders.remove(MarkdownElementTypes.MARKDOWN_FILE)
        return generatingProviders
    }
}

val PsiElement?.hasTrailingComment: Boolean
    get() = skipWhitespacesForwardWithoutNewLines() is PsiComment

val PsiElement?.isTrailingComment: Boolean
    get() {
        if (this !is PsiComment) {
            return false
        }
        val prev = skipWhitespacesBackwardWithoutNewLines()
        return prev != null && prev !is PsiWhiteSpace
    }

val PsiElement?.trailingDocComment: PsiComment?
    get() = (skipWhitespacesForwardWithoutNewLines() as? PsiComment)?.takeIf { it.isComment }

fun PsiElement.prevDocComment(includeTrailing: Boolean = false): PsiComment? {
    var prev = prevSibling
    if (prev is PsiWhiteSpace && StringUtil.countNewLines(prev.text) <= 1) prev = prev.prevSibling
    return prev?.takeIf { it.isComment && (includeTrailing || !it.isTrailingComment) } as? PsiComment
}

fun PsiElement.nextDocComment(includeTrailing: Boolean = false): PsiComment? {
    var next = nextSibling
    if (next is PsiWhiteSpace && StringUtil.countNewLines(next.text) <= 1) next = next.nextSibling
    return next?.takeIf { it.isComment && (includeTrailing || !it.isTrailingComment) } as? PsiComment
}

val PsiElement.isDocComment
    get() = elementType == VoltumTokenTypes.BLOCK_COMMENT

val PsiElement.isComment
    get() = elementType in COMMENTS

fun PsiElement.collectPrecedingDocComments(strict: Boolean = true): List<PsiComment> =
    generateSequence(if (strict) prevDocComment() else this) { it.prevDocComment() }
        .filterIsInstance<PsiComment>()
        .toList()
        .asReversed()