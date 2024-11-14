// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.psi;

import com.intellij.psi.tree.IElementType;
import com.intellij.psi.PsiElement;
import com.intellij.lang.ASTNode;
import com.voltum.voltumscript.lang.stubs.StubFactoryKt;
import com.intellij.psi.impl.source.tree.CompositePsiElement;

public interface VoltumTypes {

  IElementType ANONYMOUS_FUNC = StubFactoryKt.factory("ANONYMOUS_FUNC");
  IElementType ARGUMENT_ID = new VoltumElementType("ARGUMENT_ID");
  IElementType ATOM_EXPR = StubFactoryKt.factory("ATOM_EXPR");
  IElementType ATTRIBUTE = new VoltumElementType("ATTRIBUTE");
  IElementType AWAIT_EXPR = StubFactoryKt.factory("AWAIT_EXPR");
  IElementType BINARY_EXPR = StubFactoryKt.factory("BINARY_EXPR");
  IElementType BINARY_OP = new VoltumElementType("BINARY_OP");
  IElementType BLOCK_BODY = new VoltumElementType("BLOCK_BODY");
  IElementType BREAK_EXPR = StubFactoryKt.factory("BREAK_EXPR");
  IElementType CALL_EXPR = StubFactoryKt.factory("CALL_EXPR");
  IElementType CONTINUE_EXPR = StubFactoryKt.factory("CONTINUE_EXPR");
  IElementType DEFER_EXPR = StubFactoryKt.factory("DEFER_EXPR");
  IElementType DICTIONARY_FIELD = StubFactoryKt.factory("DICTIONARY_FIELD");
  IElementType DICTIONARY_VALUE = StubFactoryKt.factory("DICTIONARY_VALUE");
  IElementType ELSE_STATEMENT = new VoltumElementType("ELSE_STATEMENT");
  IElementType EXPR = StubFactoryKt.factory("EXPR");
  IElementType EXPRESSION_CODE_FRAGMENT_ELEMENT = new VoltumElementType("EXPRESSION_CODE_FRAGMENT_ELEMENT");
  IElementType FIELD_ID = new VoltumElementType("FIELD_ID");
  IElementType FOR_LOOP_STATEMENT = new VoltumElementType("FOR_LOOP_STATEMENT");
  IElementType FUNC_DECLARATION = StubFactoryKt.factory("FUNC_DECLARATION");
  IElementType FUNC_ID = new VoltumElementType("FUNC_ID");
  IElementType IDENTIFIER_WITH_TYPE = new VoltumElementType("IDENTIFIER_WITH_TYPE");
  IElementType IF_STATEMENT = new VoltumElementType("IF_STATEMENT");
  IElementType LIST_VALUE = StubFactoryKt.factory("LIST_VALUE");
  IElementType LITERAL_BOOL = StubFactoryKt.factory("LITERAL_BOOL");
  IElementType LITERAL_EXPR = StubFactoryKt.factory("LITERAL_EXPR");
  IElementType LITERAL_FLOAT = StubFactoryKt.factory("LITERAL_FLOAT");
  IElementType LITERAL_INT = StubFactoryKt.factory("LITERAL_INT");
  IElementType LITERAL_NULL = StubFactoryKt.factory("LITERAL_NULL");
  IElementType LITERAL_STRING = StubFactoryKt.factory("LITERAL_STRING");
  IElementType PAREN_EXPR = StubFactoryKt.factory("PAREN_EXPR");
  IElementType PATH = StubFactoryKt.factory("PATH");
  IElementType POSTFIX_DEC_EXPR = StubFactoryKt.factory("POSTFIX_DEC_EXPR");
  IElementType POSTFIX_INC_EXPR = StubFactoryKt.factory("POSTFIX_INC_EXPR");
  IElementType PREFIX_DEC_EXPR = StubFactoryKt.factory("PREFIX_DEC_EXPR");
  IElementType PREFIX_INC_EXPR = StubFactoryKt.factory("PREFIX_INC_EXPR");
  IElementType RANGE_EXPR = StubFactoryKt.factory("RANGE_EXPR");
  IElementType RETURN_EXPR = StubFactoryKt.factory("RETURN_EXPR");
  IElementType SIGNAL_DECLARATION = StubFactoryKt.factory("SIGNAL_DECLARATION");
  IElementType STATEMENT = new VoltumElementType("STATEMENT");
  IElementType STATEMENT_CODE_FRAGMENT_ELEMENT = new VoltumElementType("STATEMENT_CODE_FRAGMENT_ELEMENT");
  IElementType TUPLE_EXPR = StubFactoryKt.factory("TUPLE_EXPR");
  IElementType TYPE_ARGUMENT_LIST = new VoltumElementType("TYPE_ARGUMENT_LIST");
  IElementType TYPE_DECLARATION = StubFactoryKt.factory("TYPE_DECLARATION");
  IElementType TYPE_DECLARATION_BODY = new VoltumElementType("TYPE_DECLARATION_BODY");
  IElementType TYPE_DECLARATION_CONSTRUCTOR = new VoltumElementType("TYPE_DECLARATION_CONSTRUCTOR");
  IElementType TYPE_DECLARATION_FIELD_MEMBER = new VoltumElementType("TYPE_DECLARATION_FIELD_MEMBER");
  IElementType TYPE_DECLARATION_METHOD_MEMBER = new VoltumElementType("TYPE_DECLARATION_METHOD_MEMBER");
  IElementType TYPE_ID = new VoltumElementType("TYPE_ID");
  IElementType TYPE_REF = new VoltumElementType("TYPE_REF");
  IElementType UNARY_EXPR = StubFactoryKt.factory("UNARY_EXPR");
  IElementType VARIABLE_DECLARATION = StubFactoryKt.factory("VARIABLE_DECLARATION");
  IElementType VAR_ID = new VoltumElementType("VAR_ID");
  IElementType VAR_REFERENCE = new VoltumElementType("VAR_REFERENCE");

  IElementType AND = new VoltumTokenType("&");
  IElementType ANDAND = new VoltumTokenType("&&");
  IElementType ANDEQ = new VoltumTokenType("&=");
  IElementType ARRAY_KW = new VoltumTokenType("ARRAY_KW");
  IElementType ARROW = new VoltumTokenType("->");
  IElementType ASYNC_KW = new VoltumTokenType("async");
  IElementType AWAIT_KW = new VoltumTokenType("await");
  IElementType BOOL_KW = new VoltumTokenType("BOOL_KW");
  IElementType BRACKET_PAIR = new VoltumTokenType("[]");
  IElementType BREAK_KW = new VoltumTokenType("break");
  IElementType COLON = new VoltumTokenType(":");
  IElementType COLONCOLON = new VoltumTokenType("::");
  IElementType COMMA = new VoltumTokenType(",");
  IElementType CONTINUE_KW = new VoltumTokenType("continue");
  IElementType COROUTINE_KW = new VoltumTokenType("coroutine");
  IElementType DEFER_KW = new VoltumTokenType("defer");
  IElementType DEF_KW = new VoltumTokenType("def");
  IElementType DIV = new VoltumTokenType("/");
  IElementType DIVEQ = new VoltumTokenType("/=");
  IElementType DOT = new VoltumTokenType(".");
  IElementType DOTDOT = new VoltumTokenType("..");
  IElementType DOTDOTDOT = new VoltumTokenType("...");
  IElementType DOUBLE_KW = new VoltumTokenType("DOUBLE_KW");
  IElementType ELSE_KW = new VoltumTokenType("else");
  IElementType ENUM_KW = new VoltumTokenType("enum");
  IElementType EQ = new VoltumTokenType("=");
  IElementType EQEQ = new VoltumTokenType("==");
  IElementType EXCL = new VoltumTokenType("!");
  IElementType EXCLEQ = new VoltumTokenType("!=");
  IElementType FAT_ARROW = new VoltumTokenType("=>");
  IElementType FLOAT_KW = new VoltumTokenType("FLOAT_KW");
  IElementType FOR_KW = new VoltumTokenType("for");
  IElementType FUNC_KW = new VoltumTokenType("function");
  IElementType GT = new VoltumTokenType(">");
  IElementType GTEQ = new VoltumTokenType(">=");
  IElementType GTGT = new VoltumTokenType(">>");
  IElementType GTGTEQ = new VoltumTokenType(">>=");
  IElementType ID = new VoltumTokenType("ID");
  IElementType IF_KW = new VoltumTokenType("if");
  IElementType INTERFACE_KW = new VoltumTokenType("interface");
  IElementType INT_KW = new VoltumTokenType("INT_KW");
  IElementType LBRACK = new VoltumTokenType("[");
  IElementType LCURLY = new VoltumTokenType("{");
  IElementType LPAREN = new VoltumTokenType("(");
  IElementType LT = new VoltumTokenType("<");
  IElementType LTEQ = new VoltumTokenType("<=");
  IElementType LTLT = new VoltumTokenType("<<");
  IElementType LTLTEQ = new VoltumTokenType("<<=");
  IElementType MINUS = new VoltumTokenType("-");
  IElementType MINUSEQ = new VoltumTokenType("-=");
  IElementType MINUSMINUS = new VoltumTokenType("--");
  IElementType MUL = new VoltumTokenType("*");
  IElementType MULEQ = new VoltumTokenType("*=");
  IElementType OBJECT_KW = new VoltumTokenType("OBJECT_KW");
  IElementType OR = new VoltumTokenType("|");
  IElementType OREQ = new VoltumTokenType("|=");
  IElementType OROR = new VoltumTokenType("||");
  IElementType PLUS = new VoltumTokenType("+");
  IElementType PLUSEQ = new VoltumTokenType("+=");
  IElementType PLUSPLUS = new VoltumTokenType("++");
  IElementType QUESTION = new VoltumTokenType("?");
  IElementType RANGE_KW = new VoltumTokenType("range");
  IElementType RBRACK = new VoltumTokenType("]");
  IElementType RCURLY = new VoltumTokenType("}");
  IElementType REM = new VoltumTokenType("%");
  IElementType REMEQ = new VoltumTokenType("%=");
  IElementType RETURN_KW = new VoltumTokenType("return");
  IElementType RPAREN = new VoltumTokenType(")");
  IElementType SEMICOLON = new VoltumTokenType(";");
  IElementType SIGNAL_KW = new VoltumTokenType("signal");
  IElementType STRING_KW = new VoltumTokenType("STRING_KW");
  IElementType STRING_LITERAL = new VoltumTokenType("STRING_LITERAL");
  IElementType STRUCT_KW = new VoltumTokenType("struct");
  IElementType TILDE = new VoltumTokenType("~");
  IElementType TYPE_KW = new VoltumTokenType("type");
  IElementType VALUE_BOOL = new VoltumTokenType("VALUE_BOOL");
  IElementType VALUE_FLOAT = new VoltumTokenType("VALUE_FLOAT");
  IElementType VALUE_INTEGER = new VoltumTokenType("VALUE_INTEGER");
  IElementType VALUE_NULL = new VoltumTokenType("VALUE_NULL");
  IElementType VAR_KW = new VoltumTokenType("var");
  IElementType XOR = new VoltumTokenType("^");
  IElementType XOREQ = new VoltumTokenType("^=");
  IElementType YIELD_KW = new VoltumTokenType("YIELD_KW");

  class Factory {
    public static PsiElement createElement(ASTNode node) {
      IElementType type = node.getElementType();
      if (type == ANONYMOUS_FUNC) {
        return new VoltumAnonymousFuncImpl(node);
      }
      else if (type == ATOM_EXPR) {
        return new VoltumAtomExprImpl(node);
      }
      else if (type == AWAIT_EXPR) {
        return new VoltumAwaitExprImpl(node);
      }
      else if (type == BINARY_EXPR) {
        return new VoltumBinaryExprImpl(node);
      }
      else if (type == BREAK_EXPR) {
        return new VoltumBreakExprImpl(node);
      }
      else if (type == CALL_EXPR) {
        return new VoltumCallExprImpl(node);
      }
      else if (type == CONTINUE_EXPR) {
        return new VoltumContinueExprImpl(node);
      }
      else if (type == DEFER_EXPR) {
        return new VoltumDeferExprImpl(node);
      }
      else if (type == DICTIONARY_FIELD) {
        return new VoltumDictionaryFieldImpl(node);
      }
      else if (type == DICTIONARY_VALUE) {
        return new VoltumDictionaryValueImpl(node);
      }
      else if (type == EXPR) {
        return new VoltumExprImpl(node);
      }
      else if (type == FUNC_DECLARATION) {
        return new VoltumFuncDeclarationImpl(node);
      }
      else if (type == LIST_VALUE) {
        return new VoltumListValueImpl(node);
      }
      else if (type == LITERAL_BOOL) {
        return new VoltumLiteralBoolImpl(node);
      }
      else if (type == LITERAL_EXPR) {
        return new VoltumLiteralExprImpl(node);
      }
      else if (type == LITERAL_FLOAT) {
        return new VoltumLiteralFloatImpl(node);
      }
      else if (type == LITERAL_INT) {
        return new VoltumLiteralIntImpl(node);
      }
      else if (type == LITERAL_NULL) {
        return new VoltumLiteralNullImpl(node);
      }
      else if (type == LITERAL_STRING) {
        return new VoltumLiteralStringImpl(node);
      }
      else if (type == PAREN_EXPR) {
        return new VoltumParenExprImpl(node);
      }
      else if (type == PATH) {
        return new VoltumPathImpl(node);
      }
      else if (type == POSTFIX_DEC_EXPR) {
        return new VoltumPostfixDecExprImpl(node);
      }
      else if (type == POSTFIX_INC_EXPR) {
        return new VoltumPostfixIncExprImpl(node);
      }
      else if (type == PREFIX_DEC_EXPR) {
        return new VoltumPrefixDecExprImpl(node);
      }
      else if (type == PREFIX_INC_EXPR) {
        return new VoltumPrefixIncExprImpl(node);
      }
      else if (type == RANGE_EXPR) {
        return new VoltumRangeExprImpl(node);
      }
      else if (type == RETURN_EXPR) {
        return new VoltumReturnExprImpl(node);
      }
      else if (type == SIGNAL_DECLARATION) {
        return new VoltumSignalDeclarationImpl(node);
      }
      else if (type == TUPLE_EXPR) {
        return new VoltumTupleExprImpl(node);
      }
      else if (type == TYPE_DECLARATION) {
        return new VoltumTypeDeclarationImpl(node);
      }
      else if (type == UNARY_EXPR) {
        return new VoltumUnaryExprImpl(node);
      }
      else if (type == VARIABLE_DECLARATION) {
        return new VoltumVariableDeclarationImpl(node);
      }
      throw new AssertionError("Unknown element type: " + type);
    }

    public static CompositePsiElement createElement(IElementType type) {
       if (type == ARGUMENT_ID) {
        return new VoltumArgumentIdImpl(type);
      }
      else if (type == ATTRIBUTE) {
        return new VoltumAttributeImpl(type);
      }
      else if (type == BINARY_OP) {
        return new VoltumBinaryOpImpl(type);
      }
      else if (type == BLOCK_BODY) {
        return new VoltumBlockBodyImpl(type);
      }
      else if (type == ELSE_STATEMENT) {
        return new VoltumElseStatementImpl(type);
      }
      else if (type == EXPRESSION_CODE_FRAGMENT_ELEMENT) {
        return new VoltumExpressionCodeFragmentElementImpl(type);
      }
      else if (type == FIELD_ID) {
        return new VoltumFieldIdImpl(type);
      }
      else if (type == FOR_LOOP_STATEMENT) {
        return new VoltumForLoopStatementImpl(type);
      }
      else if (type == FUNC_ID) {
        return new VoltumFuncIdImpl(type);
      }
      else if (type == IDENTIFIER_WITH_TYPE) {
        return new VoltumIdentifierWithTypeImpl(type);
      }
      else if (type == IF_STATEMENT) {
        return new VoltumIfStatementImpl(type);
      }
      else if (type == STATEMENT) {
        return new VoltumStatementImpl(type);
      }
      else if (type == STATEMENT_CODE_FRAGMENT_ELEMENT) {
        return new VoltumStatementCodeFragmentElementImpl(type);
      }
      else if (type == TYPE_ARGUMENT_LIST) {
        return new VoltumTypeArgumentListImpl(type);
      }
      else if (type == TYPE_DECLARATION_BODY) {
        return new VoltumTypeDeclarationBodyImpl(type);
      }
      else if (type == TYPE_DECLARATION_CONSTRUCTOR) {
        return new VoltumTypeDeclarationConstructorImpl(type);
      }
      else if (type == TYPE_DECLARATION_FIELD_MEMBER) {
        return new VoltumTypeDeclarationFieldMemberImpl(type);
      }
      else if (type == TYPE_DECLARATION_METHOD_MEMBER) {
        return new VoltumTypeDeclarationMethodMemberImpl(type);
      }
      else if (type == TYPE_ID) {
        return new VoltumTypeIdImpl(type);
      }
      else if (type == TYPE_REF) {
        return new VoltumTypeRefImpl(type);
      }
      else if (type == VAR_ID) {
        return new VoltumVarIdImpl(type);
      }
      else if (type == VAR_REFERENCE) {
        return new VoltumVarReferenceImpl(type);
      }
      throw new AssertionError("Unknown element type: " + type);
    }
  }
}
