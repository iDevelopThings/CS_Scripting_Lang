package com.voltum.voltumscript.parser

import com.intellij.psi.PsiElement
import com.intellij.psi.tree.IElementType
import com.intellij.psi.tree.TokenSet
import com.intellij.psi.util.elementType
import com.voltum.voltumscript.ext.BitFlagInstanceBuilder
import com.voltum.voltumscript.ext.isSet
import com.voltum.voltumscript.ext.setFlag
import com.voltum.voltumscript.lang.types.PrototypeFlag
import com.voltum.voltumscript.lang.types.Ty
import com.voltum.voltumscript.lang.types.TypeFlagsDelegate
import com.voltum.voltumscript.psi.VoltumTypes
import kotlin.properties.ReadWriteProperty
import kotlin.reflect.KProperty

typealias KeywordCompletionFlags = Int

object KeywordCompletionFlag : BitFlagInstanceBuilder<KeywordCompletionFlags>(Limit.INT) {
    // Keyword shows in top level completion
    val TOP_LEVEL = next("TOP_LEVEL")

    // Keyword shows in type completion
    val TYPE_NAME = next("TYPE_NAME")
    
    // Keyword shows in `type x {here}` completion
    val TYPE_DECL_KIND = next("TYPE_DECL_KIND")
    
    // Keyword is available in function body
    val BLOCK_BODY = next("BLOCK_BODY")
}

class KeywordFlagsDelegate(val flagType: KeywordCompletionFlags) : ReadWriteProperty<VoltumKeywords, Boolean> {
    override fun getValue(thisRef: VoltumKeywords, property: KProperty<*>): Boolean = thisRef.flags.isSet(flagType)
    override fun setValue(thisRef: VoltumKeywords, property: KProperty<*>, value: Boolean) {
        thisRef.flags = thisRef.flags.setFlag(flagType, value)
    }
}

enum class VoltumKeywords {
    VAR("var", VoltumTypes.VAR_KW, true, false, KeywordCompletionFlag.TOP_LEVEL or KeywordCompletionFlag.BLOCK_BODY),
    TYPE("type", VoltumTypes.TYPE_KW, true, false, KeywordCompletionFlag.TOP_LEVEL),
    STRUCT("struct", VoltumTypes.STRUCT_KW, true, false, KeywordCompletionFlag.TYPE_DECL_KIND),
    INTERFACE("interface", VoltumTypes.INTERFACE_KW, true, false, KeywordCompletionFlag.TYPE_DECL_KIND),
    ENUM("enum", VoltumTypes.ENUM_KW, true, false, KeywordCompletionFlag.TOP_LEVEL),
    FUNCTION("function", VoltumTypes.FUNC_KW, true, false, KeywordCompletionFlag.TOP_LEVEL or KeywordCompletionFlag.BLOCK_BODY),
    SIGNAL("signal", VoltumTypes.SIGNAL_KW, true, false, KeywordCompletionFlag.TOP_LEVEL or KeywordCompletionFlag.BLOCK_BODY),
    RANGE("range", VoltumTypes.RANGE_KW, true, false, KeywordCompletionFlag.TOP_LEVEL or KeywordCompletionFlag.BLOCK_BODY),
    RETURN("return", VoltumTypes.RETURN_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    BREAK("break", VoltumTypes.BREAK_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    CONTINUE("continue", VoltumTypes.CONTINUE_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    IF("if", VoltumTypes.IF_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    ELSE("else", VoltumTypes.ELSE_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    FOR("for", VoltumTypes.FOR_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    DEFER("defer", VoltumTypes.DEFER_KW, true, false, KeywordCompletionFlag.BLOCK_BODY),
    DEF("def", VoltumTypes.DEF_KW, true, false, KeywordCompletionFlag.TOP_LEVEL),
    ASYNC("async", VoltumTypes.ASYNC_KW, true, false, KeywordCompletionFlag.TOP_LEVEL),
    AWAIT("await", VoltumTypes.AWAIT_KW, true, false, KeywordCompletionFlag.TOP_LEVEL.or(KeywordCompletionFlag.BLOCK_BODY)),
    COROUTINE("coroutine", VoltumTypes.COROUTINE_KW, true, false, KeywordCompletionFlag.TOP_LEVEL),
    INT(listOf("int", "int32", "i32"), tokenSetOf(VoltumTypes.INT_KW), false, true, KeywordCompletionFlag.TYPE_NAME),
    FLOAT(listOf("float", "float32", "f32"), tokenSetOf(VoltumTypes.FLOAT_KW), false, true, KeywordCompletionFlag.TYPE_NAME),
    DOUBLE(listOf("double", "float64", "f64"), tokenSetOf(VoltumTypes.DOUBLE_KW), false, true, KeywordCompletionFlag.TYPE_NAME),
    STRING(listOf("string", "str"), tokenSetOf(VoltumTypes.STRING_KW), false, true, KeywordCompletionFlag.TYPE_NAME),
    BOOL(listOf("bool", "boolean"), tokenSetOf(VoltumTypes.BOOL_KW), false, true, KeywordCompletionFlag.TYPE_NAME),
    OBJECT(listOf("object", "Object"), tokenSetOf(VoltumTypes.OBJECT_KW), true, true, KeywordCompletionFlag.TYPE_NAME),
    ARRAY(listOf("array", "Array"), tokenSetOf(VoltumTypes.ARRAY_KW), true, true, KeywordCompletionFlag.TYPE_NAME),
    NULL("null", VoltumTypes.VALUE_NULL, false, false, KeywordCompletionFlag.TYPE_NAME)
    ;

    var keywords: List<String> = listOf()
    var tokenTypes: TokenSet = tokenSetOf()
    var canBeUsedAsIdentifier: Boolean = false
    var isTypeName: Boolean = false
    var flags: KeywordCompletionFlags = 0

    var completionIsTopLevel: Boolean by KeywordFlagsDelegate(KeywordCompletionFlag.TOP_LEVEL)
    val completionIsTypeName: Boolean by KeywordFlagsDelegate(KeywordCompletionFlag.TYPE_NAME)
    val completionIsTypeDeclKind: Boolean by KeywordFlagsDelegate(KeywordCompletionFlag.TYPE_DECL_KIND)
    val completionIsBlockBody: Boolean by KeywordFlagsDelegate(KeywordCompletionFlag.BLOCK_BODY)
    
    constructor(
        keyword: String,
        tokenType: IElementType,
        canBeUsedAsIdentifier: Boolean = true,
        isTypeName: Boolean = false,
        flags: KeywordCompletionFlags = 0
    ) {
        this.keywords = listOf(keyword)
        this.tokenTypes = tokenSetOf(tokenType)
        this.canBeUsedAsIdentifier = canBeUsedAsIdentifier
        this.isTypeName = isTypeName
        this.flags = flags
    }

    constructor(
        keywords: List<String>,
        tokenTypes: TokenSet,
        canBeUsedAsIdentifier: Boolean = true,
        isTypeName: Boolean = false,
        flags: KeywordCompletionFlags = 0
    ) {
        this.keywords = keywords
        this.tokenTypes = tokenTypes
        this.canBeUsedAsIdentifier = canBeUsedAsIdentifier
        this.isTypeName = isTypeName
        this.flags = flags
    }

    companion object {
        fun allIdentifierKeywords(): Sequence<VoltumKeywords> {
            return entries.asSequence().filter { it.canBeUsedAsIdentifier }
        }
        fun forFlags(flags: KeywordCompletionFlags): Sequence<VoltumKeywords> {
            return entries.asSequence().filter { it.flags.isSet(flags) }
        }

        fun allTypeNames(): Sequence<VoltumKeywords> {
            return entries.asSequence().filter { it.isTypeName }
        }

        fun typeNames(): TokenSet {
            return entries.filter { it.isTypeName }.map {
                it.tokenTypes
            }.reduce { acc, tokenSet ->
                TokenSet.orSet(acc, tokenSet)
            }
        }
        
        fun nonTypeNameKeywords(): TokenSet {
            return entries.filter { !it.isTypeName }.map {
                it.tokenTypes
            }.reduce { acc, tokenSet ->
                TokenSet.orSet(acc, tokenSet)
            }
        }

        fun keywords(): TokenSet {
            return entries.map {
                it.tokenTypes
            }.reduce { acc, tokenSet ->
                TokenSet.orSet(acc, tokenSet)
            }
        }
        
    }
}


val PsiElement.isKeyword: Boolean
    get() = elementType in KEYWORDS
