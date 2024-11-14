package com.voltum.voltumscript.ide.highlighting

import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.fileTypes.SyntaxHighlighter
import com.intellij.openapi.fileTypes.SyntaxHighlighterFactory
import com.intellij.openapi.options.OptionsBundle
import com.intellij.openapi.options.colors.AttributesDescriptor
import com.intellij.openapi.options.colors.ColorDescriptor
import com.intellij.openapi.options.colors.ColorSettingsPage
import com.voltum.voltumscript.Icons
import com.voltum.voltumscript.VoltumBundle
import com.voltum.voltumscript.lang.VoltumLanguage
import javax.swing.Icon

class VoltumColorSettingsPage : ColorSettingsPage {
    override fun getColorDescriptors(): Array<ColorDescriptor> = ColorDescriptor.EMPTY_ARRAY

    override fun getDisplayName(): String = VoltumLanguage.displayName

    override fun getIcon(): Icon = Icons.Logo

    override fun getHighlighter(): SyntaxHighlighter =
        SyntaxHighlighterFactory.getSyntaxHighlighter(VoltumLanguage, null, null)

    override fun getDemoText(): String = DEMO_TEXT

    override fun getAttributeDescriptors(): Array<AttributesDescriptor> = DESCRIPTORS

    override fun getAdditionalHighlightingTagToDescriptorMap(): Map<String, TextAttributesKey> = ADDITIONAL_DESCRIPTORS
}

private val DESCRIPTORS = arrayOf(
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.doc.comment"), VoltumColors.DOC_COMMENT),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.line.comment"), VoltumColors.LINE_COMMENT),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.string"), VoltumColors.STRING_LITERAL),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.keyword"), VoltumColors.KEYWORD),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.identifier"), VoltumColors.IDENTIFIER),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.number"), VoltumColors.NUMBER),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.brackets"), VoltumColors.BRACKETS),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.parentheses"), VoltumColors.PARENTHESES),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.braces"), VoltumColors.BRACES),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.dot"), VoltumColors.DOT),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.comma"), VoltumColors.COMMA),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.operation"), VoltumColors.OPERATOR),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.class.name"), VoltumColors.TYPE_NAME),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.class.reference"), VoltumColors.TYPE_REFERENCE),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.parameter"), VoltumColors.PARAMETER),
    AttributesDescriptor(VoltumBundle.lazyMessage("voltum.color.settings.field.name"), VoltumColors.FIELD_NAME),
    AttributesDescriptor(VoltumBundle.lazyMessage("voltum.color.settings.field.reference"), VoltumColors.FIELD_REFERENCE),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.function.declaration"), VoltumColors.FUNCTION),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.instance.method"), VoltumColors.METHOD),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.local.variable"), VoltumColors.LOCAL_VARIABLE),
    AttributesDescriptor(OptionsBundle.messagePointer("options.language.defaults.global.variable"), VoltumColors.GLOBAL_VARIABLE),
)

private val ADDITIONAL_DESCRIPTORS = mapOf(
    "tn" to VoltumColors.TYPE_NAME,
    "tr" to VoltumColors.TYPE_REFERENCE,
    "param" to VoltumColors.PARAMETER,
    "fn" to VoltumColors.FIELD_NAME,
    "fr" to VoltumColors.FIELD_REFERENCE,
    "func" to VoltumColors.FUNCTION,
    "gv" to VoltumColors.GLOBAL_VARIABLE,
    "lv" to VoltumColors.LOCAL_VARIABLE,
)

private val DEMO_TEXT = """
    var <gv>globalVar</gv> = 0;
    
    // Line comment
    type <tn>Object</tn> struct
    {      
      <fn>fieldName</fn> <tr>int</tr>
      <fn>fieldName</fn> <tr>string</tr>
      
      def <func>toJson</func>() <tr>object</tr>;
      def <func>fromJson</func>(<tr>string</tr> <param>json</param>) <tr>object</tr>;
      /**
       * Block comment
       */
      def <func>setPrototype</func>(<tr>object</tr> <param>prototype</param>) <tr>object</tr>;
      
      <func>method</func>() <tr>void</tr> {
          var <lv>localVar</lv> = 0;
          localVar = localVar + 1;
      }
    }    
    """.trimIndent()