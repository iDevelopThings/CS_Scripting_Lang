package com.voltum.voltumscript.ide.highlighting

import com.intellij.openapi.editor.DefaultLanguageHighlighterColors
import com.intellij.openapi.editor.colors.TextAttributesKey.createTextAttributesKey

object VoltumColors {
    val DOC_COMMENT = createTextAttributesKey("VOLTUM_DOC_COMMENT", DefaultLanguageHighlighterColors.DOC_COMMENT)
    val LINE_COMMENT = createTextAttributesKey("VOLTUM_LINE_COMMENT", DefaultLanguageHighlighterColors.LINE_COMMENT)
    val STRING_LITERAL = createTextAttributesKey("VOLTUM_STRING_LITERAL", DefaultLanguageHighlighterColors.STRING)
    val KEYWORD = createTextAttributesKey("VOLTUM_KEYWORD", DefaultLanguageHighlighterColors.KEYWORD)
    val IDENTIFIER = createTextAttributesKey("VOLTUM_IDENTIFIER", DefaultLanguageHighlighterColors.IDENTIFIER)
    val NUMBER = createTextAttributesKey("VOLTUM_NUMBER", DefaultLanguageHighlighterColors.NUMBER)
    val BRACKETS = createTextAttributesKey("VOLTUM_BRACKETS", DefaultLanguageHighlighterColors.BRACKETS)
    val PARENTHESES = createTextAttributesKey("VOLTUM_PARENTHESES", DefaultLanguageHighlighterColors.PARENTHESES)
    val BRACES = createTextAttributesKey("VOLTUM_BRACES", DefaultLanguageHighlighterColors.BRACES)
    val DOT = createTextAttributesKey("VOLTUM_DOT", DefaultLanguageHighlighterColors.DOT)
    val COMMA = createTextAttributesKey("VOLTUM_COMMA", DefaultLanguageHighlighterColors.COMMA)
    val OPERATOR = createTextAttributesKey("VOLTUM_OPERATION_SIGN", DefaultLanguageHighlighterColors.OPERATION_SIGN)

    val TYPE_PARAM_DECL = createTextAttributesKey("VOLTUM_TYPE_PARAM_DECL", DefaultLanguageHighlighterColors.PARAMETER)
    val TYPE_PARAM = createTextAttributesKey("VOLTUM_TYPE_PARAM", DefaultLanguageHighlighterColors.PARAMETER)
    
    val TYPE_NAME = createTextAttributesKey("VOLTUM_TYPE_NAME", DefaultLanguageHighlighterColors.CLASS_NAME)
    val TYPE_REFERENCE = createTextAttributesKey("VOLTUM_TYPE_REFERENCE", DefaultLanguageHighlighterColors.CLASS_REFERENCE)

    val STRUCT = createTextAttributesKey("VOLTUM_STRUCT", TYPE_NAME)
    
    //    val TYPE_REFERENCE = createTextAttributesKey("VOLTUM_TYPE_REFERENCE", TYPE_NAME)
    val PARAMETER = createTextAttributesKey("VOLTUM_PARAMETER", DefaultLanguageHighlighterColors.PARAMETER)
    val FIELD_NAME = createTextAttributesKey("VOLTUM_FIELD_NAME", DefaultLanguageHighlighterColors.INSTANCE_FIELD)
    val FIELD_REFERENCE = createTextAttributesKey("VOLTUM_FIELD_REFERENCE", FIELD_NAME)
    val FUNCTION = createTextAttributesKey("VOLTUM_FUNCTION", DefaultLanguageHighlighterColors.FUNCTION_DECLARATION)
    val METHOD = createTextAttributesKey("VOLTUM_METHOD", DefaultLanguageHighlighterColors.INSTANCE_METHOD)
    val LOCAL_VARIABLE = createTextAttributesKey("VOLTUM_LOCAL_VARIABLE", DefaultLanguageHighlighterColors.LOCAL_VARIABLE)
    val GLOBAL_VARIABLE = createTextAttributesKey("VOLTUM_GLOBAL_VARIABLE", DefaultLanguageHighlighterColors.GLOBAL_VARIABLE)
}