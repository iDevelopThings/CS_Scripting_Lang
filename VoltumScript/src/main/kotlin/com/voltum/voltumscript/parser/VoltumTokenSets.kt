package com.voltum.voltumscript.parser

import com.intellij.lang.BracePair
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.TokenSet
import com.voltum.voltumscript.psi.VoltumTypes.*

fun tokenSetOf(vararg tokens: IElementType) = TokenSet.create(*tokens)
fun tokenOrSetOf(vararg sets: TokenSet) = TokenSet.orSet(*sets)

val IDENTIFIERS = tokenSetOf(
    ID,
    VAR_ID,
    FUNC_ID,
    TYPE_ID,
    TYPE_REF,
    PATH,
    VAR_REFERENCE,
)

val KEYWORDS = VoltumKeywords.keywords()
val TYPE_NAMES = VoltumKeywords.typeNames()

val COMMENTS = tokenSetOf(VoltumTokenTypes.BLOCK_COMMENT, VoltumTokenTypes.EOL_COMMENT)
//val WHITESPACES = tokenSetOf(TokenType.WHITE_SPACE, NLS)

val STRING = tokenSetOf(STRING_LITERAL)
val LITERAL_VALUES = tokenSetOf(
    VALUE_INTEGER,
    VALUE_FLOAT,
    STRING_LITERAL,
    VALUE_NULL,
    VALUE_BOOL,
)

val LBRACES = tokenSetOf(LPAREN, LCURLY, LBRACK)
val RBRACES = tokenSetOf(RPAREN, RCURLY, RBRACK)
val PARENS = tokenSetOf(LPAREN, RPAREN)
val CURLY_BRACKETS = tokenSetOf(LCURLY, RCURLY)
val SQ_BRACKETS = tokenSetOf(LBRACK, RBRACK)

val VALUE_ITEMS = tokenOrSetOf(
    LITERAL_VALUES,
    tokenSetOf(
        DICTIONARY_VALUE,
        LIST_VALUE,
        FUNC_DECLARATION,
        TYPE_DECLARATION,
    )

)

val PREFIX_AND_POSTFIX_OPERATORS = tokenSetOf(
    PLUSPLUS,
    MINUSMINUS,
)

val OPERATORS = tokenSetOf(
    OR, AND, EXCL, EQ, EXCLEQ, EQEQ, PLUSEQ, PLUS, PLUSPLUS,
    MINUSEQ, MINUS, MINUSMINUS, OREQ, ANDAND, ANDEQ, LT, XOREQ, XOR, MULEQ, MUL,
    DIVEQ, DIV, REMEQ, REM, GT, DOT, DOTDOT, DOTDOTDOT, FAT_ARROW, ARROW, GTGTEQ,
    GTGT, GTEQ, LTLTEQ, LTLT, LTEQ, OROR, ANDAND,
)

object VoltumTokenSets {
    /**
     * Custom keyword token types
     */

//    val NLS: IElementType = VoltumTokenType("Voltum_WS_NEW_LINES")

    // @JvmStatic
    // val TYPE_NAME_KEYWORDS = VoltumTokenType("TYPE_NAME_KEYWORDS")

//    val KEYWORDS = tokenSetOf(
//        VAR_KW,
//        RETURN_KW,
//        TYPE_KW,
//        INTERFACE_KW,
//        STRUCT_KW,
//        FUNC_KW,
//        ENUM_KW,
//        IF_KW,
//        ELSE_KW,
//        FOR_KW,
//        ENUM_KW,
//        DEFER_KW,
//    )

//    val KEYWORD_IDENTIFIERS = tokenOrSetOf(
//        tokenSetOf(
//            IF_KW,
//            ELSE_KW,
//            FOR_KW,
//            
//        ),
//        KEYWORDS
//    )

    // This will allow us to use keywords as identifiers
    // for ex, with `object <name>`, or a field, we can use any of these keywords
//    val KEYWORDS_WHICH_ARE_ALSO_IDENTS = tokenSetOf(
//        IF_KW,
//        ELSE_KW,
//        FOR_KW,
//    )

}

enum class Braces(
    val openText: String,
    val closeText: String,
    val openToken: IElementType,
    val closeToken: IElementType,
    val structural: Boolean = true,
    val pair: BracePair = BracePair(openToken, closeToken, structural)
) {
    PARENS("(", ")", LPAREN, RPAREN, true),
    BRACKS("[", "]", LBRACK, RBRACK, false),
    BRACES("{", "}", LCURLY, RCURLY, true);

    fun wrap(text: CharSequence): String =
        openText + text + closeText

    val needsSemicolon: Boolean
        get() = this != BRACES

    companion object {
        fun all() = values().toList()
        fun pairs() = all().map { it.pair }

        fun fromToken(token: IElementType): Braces? = when (token) {
            LPAREN, RPAREN -> PARENS
            LBRACK, RBRACK -> BRACKS
            LCURLY, RCURLY -> BRACES
            else           -> null
        }

        fun fromOpenToken(token: IElementType): Braces? = when (token) {
            LPAREN -> PARENS
            LBRACK -> BRACKS
            LCURLY -> BRACES
            else   -> null
        }

        fun fromTokenOrFail(token: IElementType): Braces =
            fromToken(token) ?: error("Given token is not a brace: $token")
    }
}