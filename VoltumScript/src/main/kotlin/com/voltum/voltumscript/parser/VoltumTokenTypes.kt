package com.voltum.voltumscript.parser

import com.intellij.psi.tree.IElementType
import com.voltum.voltumscript.psi.VoltumElementType
import com.voltum.voltumscript.psi.VoltumTokenType
import com.voltum.voltumscript.psi.VoltumTypes

//
//object VoltumTypes {
//    val ACCESS_EXPR: IElementType = factory("ACCESS_EXPR")
//    val ANONYMOUS_FUNC: IElementType = VoltumElementType("ANONYMOUS_FUNC")
//    val ARGUMENT_ID: IElementType = VoltumElementType("ARGUMENT_ID")
//}


//val NameToType = VoltumTypes.Classes
//    .elementTypes()
//    .associateBy {
//        it.debugName
//    } as LinkedHashMap<String, IElementType>


class VoltumTokenTypes  {
    companion object {
        @JvmField
        val BLOCK_COMMENT = VoltumTokenType("<BLOCK_COMMENT>")

        @JvmField
        val EOL_COMMENT = VoltumTokenType("<EOL_COMMENT>")

        @JvmField
        val UNARY_EXPR = VoltumElementType("UNARY_EXPR")

        @JvmField
        val BINARY_EXPR = VoltumElementType("BINARY_EXPR")
    }
    
}


//val BLOCK_COMMENT = VoltumTokenType("<BLOCK_COMMENT>")
//val EOL_COMMENT = VoltumTokenType("<EOL_COMMENT>")
//val UNARY_EXPR: IElementType = VoltumElementType("UNARY_EXPR")
//val BINARY_EXPR: IElementType = VoltumElementType("BINARY_EXPR")