package com.voltum.voltumscript.lang.stubs

import com.voltum.voltumscript.psi.*


fun factory(name: String): VoltumStubElementType<*, *> = when (name) {

    "FUNC_DECLARATION"     -> VoltumFunctionStub.Type
    "ANONYMOUS_FUNC"       -> VoltumFunctionStub.Type

    "TYPE_DECLARATION"     -> VoltumTypeDeclarationStub.Type

    "VARIABLE_DECLARATION" -> VoltumVariableDeclarationStub.Type
    "SIGNAL_DECLARATION"   -> VoltumPlaceholderStub.Type("SIGNAL_DECLARATION", ::VoltumSignalDeclarationImpl)


    "DICTIONARY_VALUE"     -> VoltumDictionaryStub.Type
    "DICTIONARY_FIELD"     -> VoltumDictionaryFieldStub.Type
    "LIST_VALUE"           -> VoltumExprStubType("LIST_VALUE", ::VoltumListValueImpl)

    "EXPR"                 -> VoltumExprStubType("EXPR", ::VoltumExprImpl)
    "BINARY_EXPR"          -> VoltumExprStubType("BINARY_EXPR", ::VoltumBinaryExprImpl)
    "ATOM_EXPR"            -> VoltumExprStubType("ATOM_EXPR", ::VoltumAtomExprImpl)
    "PAREN_EXPR"           -> VoltumExprStubType("PAREN_EXPR", ::VoltumParenExprImpl)
    "POSTFIX_DEC_EXPR"     -> VoltumExprStubType("POSTFIX_DEC_EXPR", ::VoltumPostfixDecExprImpl)
    "POSTFIX_INC_EXPR"     -> VoltumExprStubType("POSTFIX_INC_EXPR", ::VoltumPostfixIncExprImpl)
    "PREFIX_INC_EXPR"      -> VoltumExprStubType("PREFIX_INC_EXPR", ::VoltumPrefixIncExprImpl)
    "PREFIX_DEC_EXPR"      -> VoltumExprStubType("PREFIX_DEC_EXPR", ::VoltumPrefixDecExprImpl)
    "UNARY_EXPR"           -> VoltumExprStubType("UNARY_EXPR", ::VoltumUnaryExprImpl)

    "PATH"                 -> VoltumExprStubType("PATH", ::VoltumPathImpl)
    "CALL_EXPR"            -> VoltumExprStubType("CALL_EXPR", ::VoltumCallExprImpl)
    "RETURN_EXPR"          -> VoltumExprStubType("RETURN_EXPR", ::VoltumReturnExprImpl)
    "BREAK_EXPR"           -> VoltumExprStubType("BREAK_EXPR", ::VoltumBreakExprImpl)
    "CONTINUE_EXPR"        -> VoltumExprStubType("CONTINUE_EXPR", ::VoltumContinueExprImpl)
    "DEFER_EXPR"           -> VoltumExprStubType("DEFER_EXPR", ::VoltumDeferExprImpl)
    "TUPLE_EXPR"           -> VoltumExprStubType("TUPLE_EXPR", ::VoltumTupleExprImpl)
    "RANGE_EXPR"           -> VoltumExprStubType("RANGE_EXPR", ::VoltumRangeExprImpl)
    "AWAIT_EXPR"           -> VoltumExprStubType("AWAIT_EXPR", ::VoltumAwaitExprImpl)

    "LITERAL_EXPR"         -> VoltumExprStubType("LITERAL_EXPR", ::VoltumLiteralExprImpl)
    "LITERAL_BOOL"         -> VoltumExprStubType("LITERAL_BOOL", ::VoltumLiteralBoolImpl)
    "LITERAL_FLOAT"        -> VoltumExprStubType("LITERAL_FLOAT", ::VoltumLiteralFloatImpl)
    "LITERAL_INT"          -> VoltumExprStubType("LITERAL_INT", ::VoltumLiteralIntImpl)
    "LITERAL_NULL"         -> VoltumExprStubType("LITERAL_NULL", ::VoltumLiteralNullImpl)
    "LITERAL_STRING"       -> VoltumExprStubType("LITERAL_STRING", ::VoltumLiteralStringImpl)
    /* 
    
    "LIST"                      -> VoltumExprStubType("LIST", ::VoltumListImpl)

   "CALL_EXPR"                 -> VoltumExprStubType("CALL_EXPR", ::VoltumCallExprImpl)
    "CALL_W_PARAMS_EXPR"        -> VoltumExprStubType("CALL_W_PARAMS_EXPR", ::VoltumCallWParamsExprImpl)
    "MEMBER_CALL_EXPR"          -> VoltumExprStubType("MEMBER_CALL_EXPR", ::VoltumMemberCallExprImpl)
    "MEMBER_CALL_W_PARAMS_EXPR" -> VoltumExprStubType("MEMBER_CALL_W_PARAMS_EXPR", ::VoltumMemberCallWParamsExprImpl)

    "ACCESS_EXPR"               -> VoltumExprStubType("ACCESS_EXPR", ::VoltumAccessExprImpl)
    "ASSIGN_EXPR"               -> VoltumExprStubType("ASSIGN_EXPR", ::VoltumAssignExprImpl)
    "BINARY_EXPR"               -> VoltumExprStubType("BINARY_EXPR", ::VoltumBinaryExprImpl)
    "CONDITIONAL_EXPR"          -> VoltumExprStubType("CONDITIONAL_EXPR", ::VoltumConditionalExprImpl)
    "UNARY_EXPR"                -> VoltumExprStubType("UNARY_EXPR", ::VoltumUnaryExprImpl)
    "VALUE_EXPR"                -> VoltumExprStubType("VALUE_EXPR", ::VoltumValueExprImpl)
    "EXPRESSION"                -> VoltumExprStubType("EXPRESSION", ::VoltumExpressionImpl)

    "LITERAL_BOOL"              -> VoltumExprStubType("LITERAL_BOOL", ::VoltumLiteralBoolImpl)
    "LITERAL_FLOAT"             -> VoltumExprStubType("LITERAL_FLOAT", ::VoltumLiteralFloatImpl)
    "LITERAL_INT"               -> VoltumExprStubType("LITERAL_INT", ::VoltumLiteralIntImpl)
    "LITERAL_NUMBER"            -> VoltumExprStubType("LITERAL_NUMBER", ::VoltumLiteralNumberImpl)
    "LITERAL_NULL"              -> VoltumExprStubType("LITERAL_NULL", ::VoltumLiteralNullImpl)
    "LITERAL_STRING"            -> VoltumExprStubType("LITERAL_STRING", ::VoltumLiteralStringImpl)
*/

    else                   -> error("Unknown element $name")
}