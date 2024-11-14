package com.voltum.voltumscript.ide.highlighting

import com.intellij.lexer.Lexer
import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.fileTypes.SyntaxHighlighterBase
import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.lexer.VoltumLexerAdapter
import com.voltum.voltumscript.parser.*
import com.voltum.voltumscript.psi.VoltumTypes

class VoltumSyntaxHighlighter : SyntaxHighlighterBase() {
    override fun getHighlightingLexer(): Lexer = VoltumLexerAdapter()

    override fun getTokenHighlights(tokenType: IElementType): Array<TextAttributesKey> =
        TextAttributesKey.EMPTY_ARRAY
//        pack(ATTRIBUTES[tokenType])
}

private val ATTRIBUTES = buildMap<IElementType, TextAttributesKey> {
    put(VoltumTokenTypes.BLOCK_COMMENT, VoltumColors.DOC_COMMENT)
    put(VoltumTokenTypes.EOL_COMMENT, VoltumColors.LINE_COMMENT)
    put(VoltumTypes.STRING_LITERAL, VoltumColors.STRING_LITERAL)
    put(VoltumTypes.ID, VoltumColors.IDENTIFIER)
    put(VoltumTypes.VALUE_INTEGER, VoltumColors.NUMBER)
    put(VoltumTypes.VALUE_FLOAT, VoltumColors.NUMBER)
    put(VoltumTypes.COMMA, VoltumColors.COMMA)
    put(VoltumTypes.DOT, VoltumColors.DOT)
    put(VoltumTypes.EQ, VoltumColors.OPERATOR)
    put(VoltumTypes.COLON, VoltumColors.OPERATOR)
    put(VoltumTypes.QUESTION, VoltumColors.OPERATOR)
    put(VoltumTypes.EXCL, VoltumColors.OPERATOR)

    SyntaxHighlighterBase.fillMap(this, VoltumKeywords.nonTypeNameKeywords(), VoltumColors.KEYWORD)
    SyntaxHighlighterBase.fillMap(this, SQ_BRACKETS, VoltumColors.BRACKETS)
    SyntaxHighlighterBase.fillMap(this, CURLY_BRACKETS, VoltumColors.BRACES)
    SyntaxHighlighterBase.fillMap(this, PARENS, VoltumColors.PARENTHESES)
}