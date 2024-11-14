package com.voltum.voltumscript.parser


import com.intellij.lang.PsiBuilder
import com.intellij.lang.PsiBuilderUtil
import com.intellij.lang.WhitespacesAndCommentsBinder
import com.intellij.lang.parser.GeneratedParserUtilBase
import com.intellij.openapi.diagnostic.logger
import com.intellij.openapi.util.Key
import com.intellij.psi.TokenType
import com.intellij.psi.tree.IElementType
import com.intellij.util.BitUtil
import com.intellij.util.containers.Stack
import com.voltum.voltumscript.VoltumBundle
import com.voltum.voltumscript.ext.makeBitMask
import com.voltum.voltumscript.psi.VoltumTypes
import com.voltum.voltumscript.psi.VoltumTypes.*
import com.voltum.voltumscript.psi.VoltumTypes.ID


@Suppress("UNUSED_PARAMETER")
object VoltumParserUtil : GeneratedParserUtilBase() {

    private val LOG = logger<VoltumParserUtil>()

    enum class IdentMode { ON, OFF }

    private val IDENT_MODE: Int = makeBitMask(1)
    private val BRACE_PARENS: Int = makeBitMask(2)
    private val BRACE_BRACKS: Int = makeBitMask(3)
    private val BRACE_BRACES: Int = makeBitMask(4)

    private val DEFAULT_FLAGS: Int = IDENT_MODE


    private val FLAGS: Key<Int> = Key("VoltumParserUtil.FLAGS")
    private var PsiBuilder.flags: Int
        get() = getUserData(FLAGS) ?: DEFAULT_FLAGS
        set(value) = putUserData(FLAGS, value)

    private val FLAG_STACK: Key<Stack<Int>> = Key("VoltumParserUtil.FLAG_STACK")
    private var PsiBuilder.flagStack: Stack<Int>
        get() = getUserData(FLAG_STACK) ?: Stack<Int>(0)
        set(value) = putUserData(FLAG_STACK, value)

    private fun PsiBuilder.popFlag() {
        flags = flagStack.pop()
    }

    @JvmStatic
    private fun PsiBuilder.pushFlag(flag: Int, mode: Boolean) {
        val stack = flagStack
        stack.push(flags)
        flagStack = stack
        flags = BitUtil.set(flags, flag, mode)
    }

    @JvmStatic
    private fun PsiBuilder.pushFlags(vararg flagsAndValues: Pair<Int, Boolean>) {
        val stack = flagStack
        stack.push(flags)
        flagStack = stack
        for (flagAndValue in flagsAndValues) {
            flags = BitUtil.set(flags, flagAndValue.first, flagAndValue.second)
        }
    }

    @JvmStatic
    fun resetFlags(b: PsiBuilder, level: Int): Boolean {
        b.popFlag()
        return true
    }

    private fun setBraces(flags: Int, mode: Braces): Int {
        val flag = when (mode) {
            Braces.PARENS -> BRACE_PARENS
            Braces.BRACKS -> BRACE_BRACKS
            Braces.BRACES -> BRACE_BRACES
        }
        return flags and (BRACE_PARENS or BRACE_BRACKS or BRACE_BRACES).inv() or flag
    }

    private fun getBraces(flags: Int): Braces? = when {
        BitUtil.isSet(flags, BRACE_PARENS) -> Braces.PARENS
        BitUtil.isSet(flags, BRACE_BRACKS) -> Braces.BRACKS
        BitUtil.isSet(flags, BRACE_BRACES) -> Braces.BRACES
        else                               -> null
    }

    private inline fun PsiBuilder.withRootBrace(currentBrace: Braces, f: (Braces) -> Boolean): Boolean {
        val oldFlags = flags
        val oldRootBrace = getBraces(oldFlags)
        if (oldRootBrace == null) {
            flags = setBraces(oldFlags, currentBrace)
        }
        try {
            return f(oldRootBrace ?: currentBrace)
        } finally {
            flags = oldFlags
        }
    }

    /** handles `<=` and `<` */
    @JvmStatic
    fun lessThanOrEqual(b: PsiBuilder, level: Int): Boolean {
        val pair = when (b.tokenType) {
            LT -> when (b.rawLookup(1)) {
                LT   -> when (b.rawLookup(2)) {
                    EQ   -> LTLTEQ to 3
                    else -> LTLT to 2
                }

                EQ   -> LTEQ to 2
                else -> LT to 1
            }
            else -> null
        }
        if(pair == null) 
            return false
        
        val marker = b.mark()
        PsiBuilderUtil.advance(b, pair.second)
        marker.collapse(pair.first)
        return true
    }
    @JvmStatic
    fun eqeqOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, EQEQ, EQ, EQ)

    @JvmStatic
    fun gtgteqOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, GTGTEQ, GT, GT, EQ)

    @JvmStatic
    fun gtgtOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, GTGT, GT, GT)

    @JvmStatic
    fun gteqOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, GTEQ, GT, EQ)

    @JvmStatic
    fun ltlteqOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, LTLTEQ, LT, LT, EQ)

    @JvmStatic
    fun ltltOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, LTLT, LT, LT)

    @JvmStatic
    fun lteqOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, LTEQ, LT, EQ)

    @JvmStatic
    fun ororOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, OROR, OR, OR)

    @JvmStatic
    fun andandOperator(b: PsiBuilder, level: Int): Boolean = collapse(b, ANDAND, AND, AND)

    @JvmStatic
    fun parseSecondPlusInIncrement(b: PsiBuilder, level: Int): Boolean = noWhiteSpaceBefore(b, PLUS)

    @JvmStatic
    fun parseSecondMinusInDecrement(b: PsiBuilder, level: Int): Boolean = noWhiteSpaceBefore(b, MINUS)

    @JvmStatic
    fun parseLParens(b: PsiBuilder, level: Int, param: Parser): Boolean {
        return parseBraces(b, level, param, Braces.PARENS)
    }

    @JvmStatic
    fun parseAnyBraces(b: PsiBuilder, level: Int, param: Parser): Boolean {
        val firstToken = b.tokenType ?: return false
        if (firstToken !in LBRACES) return false
        val leftBrace = Braces.fromTokenOrFail(firstToken)
        val pos = b.mark()

        val result = parseBraces(b, level, param, leftBrace)

        if (!result) {
            pos.rollbackTo()
        }

        return result
    }

    @JvmStatic
    fun parseBraces(b: PsiBuilder, level: Int, param: Parser, brace: Braces): Boolean {
        val pos = b.mark()
        b.advanceLexer() // Consume '{' or '(' or '['
        return b.withRootBrace(brace) { rootBrace ->
            if (!param.parse(b, level + 1)) {
                pos.rollbackTo()
                return false
            }

            val lastToken = b.tokenType
            if (lastToken == null || lastToken !in RBRACES) {
                b.error(VoltumBundle.message("parsing.error.expected2", brace.closeText))
                return pos.close(lastToken == null)
            }

            var rightBrace = Braces.fromToken(lastToken)
            if (rightBrace == brace) {
                b.advanceLexer() // Consume '}' or ')' or ']'
            } else {
                b.error(VoltumBundle.message("parsing.error.expected", brace.closeText))
                if (brace == rootBrace) {
                    // Recovery loop. Consume everything until [rightBrace] is [brace]
                    while (rightBrace != brace && !b.eof()) {
                        b.advanceLexer()
                        val tokenType = b.tokenType ?: break
                        rightBrace = Braces.fromToken(tokenType)
                    }
                    b.advanceLexer()
                }
            }

            pos.drop()
            return true
        }
    }


    @JvmField
    val ADJACENT_LINE_COMMENTS = WhitespacesAndCommentsBinder { tokens, _, getter ->
        var candidate = tokens.size
        for (i in 0 until tokens.size) {
            val token = tokens[i]
            // if (OUTER_BLOCK_DOC_COMMENT == token || OUTER_EOL_DOC_COMMENT == token) {
            //     candidate = minOf(candidate, i)
            //     break
            // }
            if (VoltumTokenTypes.EOL_COMMENT == token) {
                candidate = minOf(candidate, i)
            }
            if (TokenType.WHITE_SPACE == token && "\n\n" in getter[i]) {
                candidate = tokens.size
            }
        }
        //hook crashed: 
        // java. lang. ClassCastException: class java.lang.String cannot be cast to 
        // class com.intellij.lang.WhitespacesAndCommentsBinder 
        // (java.lang.String is in module java.base of loader 'bootstrap'; 
        // com.intellij.lang.WhitespacesAndCommentsBinder is in unnamed module 
        // of loader com.intellij.util.lang.PathClassLoader @546a03af
        // )
        candidate
    }

    /**
     * Idk why, but we need this for `prefix::name()` call expressions to work properly
     */
    @JvmStatic
    fun isBuiltin(builder: PsiBuilder, level: Int): Boolean {
        // val marker = builder.latestDoneMarker ?: return false
        // val parsedText = builder.originalText.subSequence(marker.startOffset, marker.endOffset).toString()
        return false
    }

    @JvmStatic
    fun setIdentMode(b: PsiBuilder, level: Int, mode: IdentMode): Boolean {
        b.pushFlag(IDENT_MODE, mode == IdentMode.ON)
        return true
    }

    @JvmStatic
    fun parseIdentifierType(b: PsiBuilder, l: Int, type: VoltumIdentifierKinds): Boolean {
        /*if (type == VoltumIdentifierKinds.UntypedIdentifier) {
            val m = enter_section_(b)
            val r = consumeToken(b, IDENTIFIER_TOKEN)
            *//*val el: IElementType? = when (type) {
                VoltumIdentifierKinds.UntypedIdentifier -> null
                else                                    -> type.elementType
            }*//*
            exit_section_(b, m, type.elementType, r)
            return r
        }*/
//        val m = b.mark()
//        consumeToken(b, IDENTIFIER_TOKEN)
        b.remapCurrentToken(type.elementType)
        b.advanceLexer()

//        consumeToken(b, IDENTIFIER_TOKEN)
//        m.collapse(type.elementType)
//        m.drop()

        return true
//        if (!recursion_guard_(b, l, "parseIdentifierType"))
//            return false
//        if (!nextTokenIs(b, IDENTIFIER_TOKEN))
//            return false

//        val m = enter_section_(b)
//        val r = consumeToken(b, IDENTIFIER_TOKEN)
//        val el: IElementType? = when (type) {
//            VoltumIdentifierKinds.UntypedIdentifier -> null
//            else                                    -> type.elementType
//        }
//        exit_section_(b, m, el, r)
//        return r

        // val marker = b.mark()
        // b.advanceLexer()
        // marker.done(type.elementType)
        // return true
    }

    @JvmStatic
    fun parseIdent(b: PsiBuilder, level: Int): Boolean {
        if (b.tokenType != VoltumTypes.ID && !KEYWORDS.contains(b.tokenType))
            return false


        if (b.tokenType != VoltumTypes.ID) {
//            val marker = b.mark()
            b.remapCurrentToken(VoltumTypes.ID)
            consumeToken(b, VoltumTypes.ID)
//            marker.done(ID)
        } else {
//            val marker = b.mark()
            b.advanceLexer()
//            marker.done(ID)
        }


        return true
    }

    /*@JvmStatic
    fun parseIdentifier(b: PsiBuilder, level: Int): Boolean {
        if (!BitUtil.isSet(b.flags, IDENT_MODE))
            return false
        return parseIdent(b, level)
    }
    
*/
    @JvmStatic
    fun parseCodeBlockLazy(builder: PsiBuilder, level: Int): Boolean {
        return PsiBuilderUtil.parseBlockLazy(
            builder,
            LCURLY,
            RCURLY,
            BLOCK_BODY
        ) != null
    }

    @JvmStatic
    private fun collapse(b: PsiBuilder, tokenType: IElementType, vararg parts: IElementType): Boolean {
        // We do not want whitespace between parts, so firstly we do raw lookup for each part,
        // and when we make sure that we have desired token, we consume and collapse it.
        parts.forEachIndexed { i, tt ->
            if (b.rawLookup(i) != tt) return false
        }
        val marker = b.mark()
        PsiBuilderUtil.advance(b, parts.size)
        marker.collapse(tokenType)
        return true
    }

    @JvmStatic
    private fun noWhiteSpaceBefore(b: PsiBuilder, token: IElementType): Boolean =
        if (b.tokenType == token && b.rawLookup(-1) == token) {
            b.advanceLexer()
            true
        } else {
            false
        }
    
    /* private fun contextualKeywordWithRollback(b: PsiBuilder, keyword: String, elementType: IElementType): Boolean {
            if (b.tokenType == VoltumTypes.ID && b.tokenText == keyword) {
                    val marker = b.mark()
                    b.advanceLexer()
                    marker.collapse(elementType)
                    return true
            }
            return false
    } */


    /*    @JvmStatic
        fun customParseExpression(b: PsiBuilder, level: Int): Boolean {
            if (b.isToken(MINUS, NOT)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseExpression(b, level + 1)
                if (!right) {
                    b.error("Expected expression")
                    return false
                }
    
                m.done(VoltumTokenTypes.UNARY_EXPR)
                return true
            }
    
            return customParseAssignment(b, level)
        }
    
        @JvmStatic
        fun customParseAssignment(b: PsiBuilder, level: Int): Boolean {
            val m = b.mark()
            val left = customParseLogicalOr(b, level + 1)
            if (!left) {
                m.error("Expected expression")
                return false
            }
    
            if(b.isToken(EQ)) {
                b.advanceLexer()
                val right = customParseAssignment(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(ASSIGN_EQ_EXPR)
                return true
            }
            
            m.drop()
            
            return false
        }
        
        @JvmStatic
        fun customParseLogicalOr(b: PsiBuilder, level: Int): Boolean {
            val left = customParseLogicalAnd(b, level + 1)
            if (!left) {
                return false
            }
            
            while (b.isToken(OR)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseLogicalAnd(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(VoltumTokenTypes.BINARY_EXPR)
            }
            return true
        }
        
        @JvmStatic
        fun customParseLogicalAnd(b: PsiBuilder, level: Int): Boolean {
            val left = customParseComparison(b, level + 1)
            if (!left) {
                return false
            }
            
            while (b.isToken(AND)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseComparison(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(VoltumTokenTypes.BINARY_EXPR)
            }
            return true
        }
    
        @JvmStatic
        fun customParseComparison(b: PsiBuilder, level: Int): Boolean {
            val left = customParseTerm(b, level + 1)
            if (!left) {
                return false
            }
            
            while (b.isToken(EQEQ, NE, LANGLE, RANGLE, LE, GE)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseTerm(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(VoltumTokenTypes.BINARY_EXPR)
            }
            return true
        }
    
        @JvmStatic
        fun customParseTerm(b: PsiBuilder, level: Int): Boolean {
            val left = customParseFactor(b, level + 1)
            if (!left) {
                return false
            }
            
            while (b.isToken(PLUS, MINUS)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseFactor(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(VoltumTokenTypes.BINARY_EXPR)
            }
            return true
        }
    
        @JvmStatic
        fun customParseFactor(b: PsiBuilder, level: Int): Boolean {
            val left = customParsePrimary(b, level + 1)
            if (!left) {
                return false
            }
            
            while (b.isToken(MUL, DIV)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParsePrimary(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                m.done(VoltumTokenTypes.BINARY_EXPR)
            }
            return true
        }
        
        @JvmStatic
        fun customParsePrimary(b: PsiBuilder, level: Int): Boolean {
            if (b.isToken(VALUE_INTEGER)) {
                val m = b.mark()
                b.advanceLexer()
                m.done(LITERAL_INT)
                return true
            }
            if (b.isToken(VALUE_FLOAT)) {
                val m = b.mark()
                b.advanceLexer()
                m.done(LITERAL_FLOAT)
                return true
            }
            
            if (b.isToken(STRING_LITERAL)) {
                val m = b.mark()
                b.advanceLexer()
                m.done(LITERAL_STRING)
                return true
            }
            if (b.isToken(VALUE_BOOL)) {
                val m = b.mark()
                b.advanceLexer()
                m.done(LITERAL_BOOL)
                return true
            }
            
            if (b.isToken(ID)) {
                val m = b.mark()
                b.advanceLexer()
                m.done(ID)
                return true
            }
            
            if (b.isToken(LPAREN)) {
                val m = b.mark()
                b.advanceLexer()
                val right = customParseExpression(b, level + 1)
                if (!right) {
                    m.error("Expected expression")
                    return false
                }
                if (!b.isToken(RPAREN)) {
                    m.error("Expected ')'")
                    return false
                }
                b.advanceLexer()
                m.drop()
                return true
            }
            
            return false
        }*/
}

