package com.voltum.voltumscript.lsp.lsp4ij

import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.psi.PsiFile
import com.redhat.devtools.lsp4ij.features.semanticTokens.DefaultSemanticTokensColorsProvider
import com.redhat.devtools.lsp4ij.features.semanticTokens.SemanticTokensColorsProvider
import com.voltum.voltumscript.ide.highlighting.VoltumColors
import com.voltum.voltumscript.psi.VoltumTypes.TYPE_REF


enum class SemanticTokenTypes(idString: String) {
    Namespace("namespace"),
    Type("type"),
    Class("class"),
    Enum("enum"),
    Interface("interface"),
    Struct("struct"),
    TypeParameter("typeParameter"),
    Parameter("parameter"),
    Variable("variable"),
    Property("property"),
    EnumMember("enumMember"),
    Event("event"),
    Function("function"),
    Method("method"),
    Macro("macro"),
    Keyword("keyword"),
    Modifier("modifier"),
    Comment("comment"),
    StringType("string"),
    Number("number"),
    Regexp("regexp"),
    Operator("operator"),
    Decorator("decorator")
    ;

    companion object {
        fun fromIdString(idString: String) = entries.find { it.name == idString }
        fun fromIdStringOrName(idString: String) = fromIdString(idString) ?: valueOf(idString)
    }
}

enum class SemanticTokenModifiers(idString: String) {
    Declaration("declaration"),
    Definition("definition"),
    Readonly("readonly"),
    Static("static"),
    Deprecated("deprecated"),
    Abstract("abstract"),
    Async("async"),
    Modification("modification"),
    Documentation("documentation"),
    DefaultLibrary("defaultLibrary"),
    Generic("generic") // non-standard generic modifier
    ;

    companion object {
        fun fromIdString(idString: String) = entries.find { it.name == idString }
        fun fromIdStringOrName(idString: String) = fromIdString(idString) ?: valueOf(idString)
    }
}

class VoltumSemanticTokensColorsProvider : DefaultSemanticTokensColorsProvider() {
    @JvmRecord
    private data class TokenHelper(val tokenModifiers: List<SemanticTokenModifiers>) {
        fun hasAny(vararg keys: SemanticTokenModifiers): Boolean {
            if (tokenModifiers.isEmpty()) return false
            for (key in keys) {
                if (tokenModifiers.contains(key)) {
                    return true
                }
            }
            return false
        }

        fun has(vararg keys: SemanticTokenModifiers): Boolean {
            if (tokenModifiers.isEmpty()) return false
            for (key in keys) {
                if (!tokenModifiers.contains(key)) {
                    return false
                }
            }
            return true
        }

        fun choice(modifier: SemanticTokenModifiers, onMatchModifier: TextAttributesKey, onNoMatchModifier: TextAttributesKey): TextAttributesKey {
            return if (tokenModifiers.contains(modifier)) onMatchModifier else onNoMatchModifier
        }

        fun choice(condition: Boolean, onMatchModifier: TextAttributesKey, onNoMatchModifier: TextAttributesKey): TextAttributesKey {
            return if (condition) onMatchModifier else onNoMatchModifier
        }

        val isDecl: Boolean
            get() = hasAny(SemanticTokenModifiers.Declaration, SemanticTokenModifiers.Definition)
    }

    override fun getTextAttributesKey(tokenType: String, tokenModifiers: MutableList<String>, file: PsiFile): TextAttributesKey? {
        val token = SemanticTokenTypes.fromIdString(tokenType)
        val modifiers = tokenModifiers.map { SemanticTokenModifiers.fromIdString(it)!! }
        val tok = TokenHelper(modifiers)

        val res = when (token) {
            SemanticTokenTypes.Comment       -> if (tok.has(SemanticTokenModifiers.Documentation)) VoltumColors.DOC_COMMENT else VoltumColors.LINE_COMMENT
            SemanticTokenTypes.Enum          -> if (tok.isDecl) VoltumColors.TYPE_NAME else VoltumColors.TYPE_REFERENCE
            SemanticTokenTypes.EnumMember    -> if (tok.isDecl) VoltumColors.FIELD_NAME else VoltumColors.FIELD_REFERENCE
            SemanticTokenTypes.Property      -> if (tok.isDecl) VoltumColors.FIELD_NAME else VoltumColors.FIELD_REFERENCE
            SemanticTokenTypes.Function      -> tok.choice(SemanticTokenModifiers.Generic, VoltumColors.FUNCTION, VoltumColors.FUNCTION)
            SemanticTokenTypes.Keyword       -> VoltumColors.KEYWORD
            SemanticTokenTypes.Method        -> tok.choice(tok.isDecl, VoltumColors.METHOD, VoltumColors.METHOD)
            SemanticTokenTypes.Number        -> VoltumColors.NUMBER
            SemanticTokenTypes.Operator      -> VoltumColors.OPERATOR
            SemanticTokenTypes.Parameter     -> VoltumColors.PARAMETER
            SemanticTokenTypes.StringType    -> VoltumColors.STRING_LITERAL
            SemanticTokenTypes.Struct        -> VoltumColors.STRUCT
            SemanticTokenTypes.Type          -> tok.choice(tok.isDecl, VoltumColors.TYPE_NAME, VoltumColors.TYPE_REFERENCE)
            SemanticTokenTypes.TypeParameter -> tok.choice(tok.isDecl, VoltumColors.TYPE_PARAM_DECL, VoltumColors.TYPE_PARAM)
            SemanticTokenTypes.Variable      -> tok.choice(tok.isDecl, VoltumColors.LOCAL_VARIABLE, VoltumColors.GLOBAL_VARIABLE)
            else                             -> null
        }
        
        return res ?: super.getTextAttributesKey(tokenType, tokenModifiers, file)
    }
}