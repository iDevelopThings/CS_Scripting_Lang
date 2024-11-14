package com.voltum.voltumscript.ide.formatting

import com.intellij.application.options.*
import com.intellij.openapi.application.ApplicationBundle
import com.intellij.psi.codeStyle.*
import com.intellij.psi.codeStyle.CodeStyleSettingsCustomizable.*
import com.intellij.psi.codeStyle.LanguageCodeStyleSettingsProvider.SettingsType.*
import com.voltum.voltumscript.VoltumBundle
import com.voltum.voltumscript.lang.VoltumLanguage

class VoltumCodeStyleSettingsProvider : LanguageCodeStyleSettingsProvider() {
    override fun getLanguage() = VoltumLanguage
    override fun getConfigurableDisplayName(): String = VoltumLanguage.displayName
    override fun createCustomSettings(settings: CodeStyleSettings) = VoltumCodeStyleSettings(settings)

    override fun createConfigurable(settings: CodeStyleSettings, modelSettings: CodeStyleSettings): CodeStyleConfigurable {
        return object : CodeStyleAbstractConfigurable(settings, modelSettings, configurableDisplayName) {
            override fun createPanel(settings: CodeStyleSettings): CodeStyleAbstractPanel {
                return VoltumCodeStyleMainPanel(currentSettings, settings)
            }
        }
    }

    override fun getCodeSample(settingsType: SettingsType): String =
        when (settingsType) {
            INDENT_SETTINGS -> INDENT_SAMPLE
            SPACING_SETTINGS -> SPACING_SAMPLE
            WRAPPING_AND_BRACES_SETTINGS -> WRAPPING_AND_BRACES_SAMPLE
            BLANK_LINES_SETTINGS -> BLANK_LINES_SAMPLE
            else -> ""
        }

    override fun customizeDefaults(commonSettings: CommonCodeStyleSettings, indentOptions: CommonCodeStyleSettings.IndentOptions) {
        commonSettings.RIGHT_MARGIN = 100
        commonSettings.ALIGN_MULTILINE_PARAMETERS_IN_CALLS = true

        commonSettings.LINE_COMMENT_AT_FIRST_COLUMN = false
        commonSettings.LINE_COMMENT_ADD_SPACE = true
        commonSettings.BLOCK_COMMENT_AT_FIRST_COLUMN = false

        indentOptions.CONTINUATION_INDENT_SIZE = indentOptions.INDENT_SIZE
    }

    override fun customizeSettings(consumer: CodeStyleSettingsCustomizable, settingsType: SettingsType) {
        when (settingsType) {
            BLANK_LINES_SETTINGS -> {
                consumer.showStandardOptions(
                    BlankLinesOption.KEEP_BLANK_LINES_IN_DECLARATIONS.name,
                    BlankLinesOption.KEEP_BLANK_LINES_IN_CODE.name
                )

                consumer.showCustomOption(
                    VoltumCodeStyleSettings::class.java,
                    "MIN_NUMBER_OF_BLANKS_BETWEEN_ITEMS",
                    VoltumBundle.message("settings.voltum.code.style.between.declarations"),
                    CodeStyleSettingsCustomizableOptions.getInstance().BLANK_LINES
                )
            }

            SPACING_SETTINGS -> {
                
            }

            WRAPPING_AND_BRACES_SETTINGS -> {
                consumer.showStandardOptions(
                    WrappingOrBraceOption.KEEP_LINE_BREAKS.name,
                    WrappingOrBraceOption.RIGHT_MARGIN.name,
                    WrappingOrBraceOption.ALIGN_MULTILINE_CHAINED_METHODS.name,
                    WrappingOrBraceOption.ALIGN_MULTILINE_PARAMETERS.name,
                    WrappingOrBraceOption.ALIGN_MULTILINE_PARAMETERS_IN_CALLS.name
                )

                consumer.showCustomOption(
                    VoltumCodeStyleSettings::class.java,
                    "ALIGN_RET_TYPE",
                    VoltumBundle.message("settings.voltum.code.style.align.return.type"),
                    CodeStyleSettingsCustomizableOptions.getInstance().WRAPPING_METHOD_PARAMETERS
                )

                consumer.showCustomOption(
                    VoltumCodeStyleSettings::class.java,
                    "ALIGN_TYPE_PARAMS",
                    ApplicationBundle.message("wrapping.align.when.multiline"),
                    CodeStyleSettingsCustomizableOptions.getInstance().SPACES_IN_TYPE_PARAMETERS
                )

            }

            COMMENTER_SETTINGS -> {
                consumer.showStandardOptions(
                    CommenterOption.LINE_COMMENT_AT_FIRST_COLUMN.name,
                    CommenterOption.LINE_COMMENT_ADD_SPACE.name,
                    CommenterOption.BLOCK_COMMENT_AT_FIRST_COLUMN.name
                )
            }

            else -> Unit
        }
    }

    override fun getIndentOptionsEditor(): IndentOptionsEditor = SmartIndentOptionsEditor()

    private class VoltumCodeStyleMainPanel(currentSettings: CodeStyleSettings?, settings: CodeStyleSettings?) : TabbedLanguageCodeStylePanel(VoltumLanguage, currentSettings, settings!!) {

        override fun initTabs(settings: CodeStyleSettings) {
            addIndentOptionsTab(settings)
            addSpacesTab(settings)
            addWrappingAndBracesTab(settings)
            addBlankLinesTab(settings)
            addTab(GenerationCodeStylePanel(settings, VoltumLanguage))
        }

    }


}


private fun sample(@org.intellij.lang.annotations.Language("Voltum") code: String) = code.trim()

private val INDENT_SAMPLE = sample(
    """
type Vector struct {
    x float
    y float
    z float
    
    add(Vector other) Vector {
        var v = new<Vector>();
        v.x = this.x + other.x;
        v.y = this.y + other.y;
        v.z = this.z + other.z;
        return v;
    }
}
"""
)

private val SPACING_SAMPLE = sample(
    """
type Vector struct {
    x float
    y float
    z float
    
    add(Vector other) Vector {
        var v = new<Vector>();
        v.x = this.x + other.x;
        v.y = this.y + other.y;
        v.z = this.z + other.z;
        return v;
    }
}
"""
)


private val WRAPPING_AND_BRACES_SAMPLE = sample(
    """
type Vector struct {
    x float
    y float
    z float
    
    add(Vector other) Vector {
        var v = new<Vector>();
        v.x = this.x + other.x;
        v.y = this.y + other.y;
        v.z = this.z + other.z;
        return v;
    }
}
"""
)


private val BLANK_LINES_SAMPLE = sample(
    """



type Vector struct {


    x float

    y float


    z float
    
    add(Vector other) Vector {
        var v = new<Vector>();
        
        v.x = this.x + other.x;
        
        v.y = this.y + other.y;
        v.z = this.z + other.z;
        
        
        return v;
    }
    
    
    
    
    sub(Vector other) Vector {
    
    
        var v = new<Vector>();
        
        v.x = this.x - other.x;
        
        v.y = this.y - other.y;
        v.z = this.z - other.z;
        
        
        return v;
    }
}
"""
)
