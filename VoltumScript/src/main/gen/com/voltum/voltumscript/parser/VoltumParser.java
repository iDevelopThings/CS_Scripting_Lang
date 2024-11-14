// This is a generated file. Not intended for manual editing.
package com.voltum.voltumscript.parser;

import com.intellij.lang.PsiBuilder;
import com.intellij.lang.PsiBuilder.Marker;
import static com.voltum.voltumscript.psi.VoltumTypes.*;
import static com.voltum.voltumscript.parser.VoltumParserUtil.*;
import com.intellij.psi.tree.IElementType;
import com.intellij.lang.ASTNode;
import com.intellij.psi.tree.TokenSet;
import com.intellij.lang.PsiParser;
import com.intellij.lang.LightPsiParser;

@SuppressWarnings({"SimplifiableIfStatement", "UnusedAssignment"})
public class VoltumParser implements PsiParser, LightPsiParser {

  public ASTNode parse(IElementType t, PsiBuilder b) {
    parseLight(t, b);
    return b.getTreeBuilt();
  }

  public void parseLight(IElementType t, PsiBuilder b) {
    boolean r;
    b = adapt_builder_(t, b, this, EXTENDS_SETS_);
    Marker m = enter_section_(b, 0, _COLLAPSE_, null);
    r = parse_root_(t, b);
    exit_section_(b, 0, m, t, r, true, TRUE_CONDITION);
  }

  protected boolean parse_root_(IElementType t, PsiBuilder b) {
    return parse_root_(t, b, 0);
  }

  static boolean parse_root_(IElementType t, PsiBuilder b, int l) {
    boolean r;
    if (t == BLOCK_BODY) {
      r = block_body(b, l + 1);
    }
    else if (t == EXPR) {
      r = expr(b, l + 1, -1);
    }
    else if (t == EXPRESSION_CODE_FRAGMENT_ELEMENT) {
      r = expression_code_fragment_element(b, l + 1);
    }
    else if (t == STATEMENT_CODE_FRAGMENT_ELEMENT) {
      r = statement_code_fragment_element(b, l + 1);
    }
    else {
      r = program(b, l + 1);
    }
    return r;
  }

  public static final TokenSet[] EXTENDS_SETS_ = new TokenSet[] {
    create_token_set_(LITERAL_BOOL, LITERAL_FLOAT, LITERAL_INT, LITERAL_NULL,
      LITERAL_STRING),
    create_token_set_(ARGUMENT_ID, FIELD_ID, FUNC_ID, TYPE_ID,
      VAR_ID, VAR_REFERENCE),
    create_token_set_(ATOM_EXPR, AWAIT_EXPR, BINARY_EXPR, BREAK_EXPR,
      CALL_EXPR, CONTINUE_EXPR, DEFER_EXPR, EXPR,
      LITERAL_EXPR, PAREN_EXPR, POSTFIX_DEC_EXPR, POSTFIX_INC_EXPR,
      PREFIX_DEC_EXPR, PREFIX_INC_EXPR, RANGE_EXPR, RETURN_EXPR,
      TUPLE_EXPR, UNARY_EXPR),
  };

  /* ********************************************************** */
  // '+' | '-'
  public static boolean additive_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "additive_binary_op")) return false;
    if (!nextTokenIs(b, "<additive binary op>", MINUS, PLUS)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<additive binary op>");
    r = consumeToken(b, PLUS);
    if (!r) r = consumeToken(b, MINUS);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // (ASYNC_KW? COROUTINE_KW? FUNC_KW argument_declaration_list [':' type_ref] block_body) 
  //     | (ASYNC_KW? COROUTINE_KW? argument_declaration_list [':' type_ref] FAT_ARROW block_body)
  //     | (ASYNC_KW? COROUTINE_KW? argument_declaration_list [':' type_ref] FAT_ARROW statement)
  public static boolean anonymous_func(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, ANONYMOUS_FUNC, "<anonymous func>");
    r = anonymous_func_0(b, l + 1);
    if (!r) r = anonymous_func_1(b, l + 1);
    if (!r) r = anonymous_func_2(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // ASYNC_KW? COROUTINE_KW? FUNC_KW argument_declaration_list [':' type_ref] block_body
  private static boolean anonymous_func_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = anonymous_func_0_0(b, l + 1);
    r = r && anonymous_func_0_1(b, l + 1);
    r = r && consumeToken(b, FUNC_KW);
    r = r && argument_declaration_list(b, l + 1);
    r = r && anonymous_func_0_4(b, l + 1);
    r = r && block_body(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ASYNC_KW?
  private static boolean anonymous_func_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_0_0")) return false;
    consumeToken(b, ASYNC_KW);
    return true;
  }

  // COROUTINE_KW?
  private static boolean anonymous_func_0_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_0_1")) return false;
    consumeToken(b, COROUTINE_KW);
    return true;
  }

  // [':' type_ref]
  private static boolean anonymous_func_0_4(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_0_4")) return false;
    anonymous_func_0_4_0(b, l + 1);
    return true;
  }

  // ':' type_ref
  private static boolean anonymous_func_0_4_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_0_4_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COLON);
    r = r && type_ref(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ASYNC_KW? COROUTINE_KW? argument_declaration_list [':' type_ref] FAT_ARROW block_body
  private static boolean anonymous_func_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_1")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = anonymous_func_1_0(b, l + 1);
    r = r && anonymous_func_1_1(b, l + 1);
    r = r && argument_declaration_list(b, l + 1);
    r = r && anonymous_func_1_3(b, l + 1);
    r = r && consumeToken(b, FAT_ARROW);
    r = r && block_body(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ASYNC_KW?
  private static boolean anonymous_func_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_1_0")) return false;
    consumeToken(b, ASYNC_KW);
    return true;
  }

  // COROUTINE_KW?
  private static boolean anonymous_func_1_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_1_1")) return false;
    consumeToken(b, COROUTINE_KW);
    return true;
  }

  // [':' type_ref]
  private static boolean anonymous_func_1_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_1_3")) return false;
    anonymous_func_1_3_0(b, l + 1);
    return true;
  }

  // ':' type_ref
  private static boolean anonymous_func_1_3_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_1_3_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COLON);
    r = r && type_ref(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ASYNC_KW? COROUTINE_KW? argument_declaration_list [':' type_ref] FAT_ARROW statement
  private static boolean anonymous_func_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_2")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = anonymous_func_2_0(b, l + 1);
    r = r && anonymous_func_2_1(b, l + 1);
    r = r && argument_declaration_list(b, l + 1);
    r = r && anonymous_func_2_3(b, l + 1);
    r = r && consumeToken(b, FAT_ARROW);
    r = r && statement(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ASYNC_KW?
  private static boolean anonymous_func_2_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_2_0")) return false;
    consumeToken(b, ASYNC_KW);
    return true;
  }

  // COROUTINE_KW?
  private static boolean anonymous_func_2_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_2_1")) return false;
    consumeToken(b, COROUTINE_KW);
    return true;
  }

  // [':' type_ref]
  private static boolean anonymous_func_2_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_2_3")) return false;
    anonymous_func_2_3_0(b, l + 1);
    return true;
  }

  // ':' type_ref
  private static boolean anonymous_func_2_3_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "anonymous_func_2_3_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COLON);
    r = r && type_ref(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // <<parseAnyBraces <<param>>>>
  static boolean any_braces(PsiBuilder b, int l, Parser _param) {
    return parseAnyBraces(b, l + 1, _param);
  }

  /* ********************************************************** */
  // expr
  public static boolean any_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "any_expr")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, EXPR, "<expr>");
    r = expr(b, l + 1, -1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // "(" <<comma_separated_list <<identifier_with_type>> >>? ")"
  static boolean argument_declaration_list(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "argument_declaration_list")) return false;
    if (!nextTokenIs(b, LPAREN)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, LPAREN);
    r = r && argument_declaration_list_1(b, l + 1);
    r = r && consumeToken(b, RPAREN);
    exit_section_(b, m, null, r);
    return r;
  }

  // <<comma_separated_list <<identifier_with_type>> >>?
  private static boolean argument_declaration_list_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "argument_declaration_list_1")) return false;
    comma_separated_list(b, l + 1, argument_declaration_list_1_0_0_parser_);
    return true;
  }

  /* ********************************************************** */
  // id
  public static boolean argument_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "argument_id")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, ARGUMENT_ID, "<argument id>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // '|=' | '^=' | '&=' | '=' | '+=' | '-=' | '*=' | '/=' | '%='
  public static boolean assign_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "assign_binary_op")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<assign binary op>");
    r = consumeToken(b, OREQ);
    if (!r) r = consumeToken(b, XOREQ);
    if (!r) r = consumeToken(b, ANDEQ);
    if (!r) r = consumeToken(b, EQ);
    if (!r) r = consumeToken(b, PLUSEQ);
    if (!r) r = consumeToken(b, MINUSEQ);
    if (!r) r = consumeToken(b, MULEQ);
    if (!r) r = consumeToken(b, DIVEQ);
    if (!r) r = consumeToken(b, REMEQ);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // LBRACK (call_expr | id) RBRACK
  public static boolean attribute(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "attribute")) return false;
    if (!nextTokenIs(b, LBRACK)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, LBRACK);
    r = r && attribute_1(b, l + 1);
    r = r && consumeToken(b, RBRACK);
    exit_section_(b, m, ATTRIBUTE, r);
    return r;
  }

  // call_expr | id
  private static boolean attribute_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "attribute_1")) return false;
    boolean r;
    r = call_expr(b, l + 1);
    if (!r) r = id(b, l + 1);
    return r;
  }

  /* ********************************************************** */
  // '&'
  public static boolean bit_and_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_and_binary_op")) return false;
    if (!nextTokenIs(b, AND)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, AND);
    exit_section_(b, m, BINARY_OP, r);
    return r;
  }

  /* ********************************************************** */
  // '|'
  public static boolean bit_or_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_or_binary_op")) return false;
    if (!nextTokenIs(b, OR)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, OR);
    exit_section_(b, m, BINARY_OP, r);
    return r;
  }

  /* ********************************************************** */
  // '<<' | '>>'
  public static boolean bit_shift_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_shift_binary_op")) return false;
    if (!nextTokenIs(b, "<bit shift binary op>", GTGT, LTLT)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<bit shift binary op>");
    r = consumeToken(b, LTLT);
    if (!r) r = consumeToken(b, GTGT);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // '^'
  public static boolean bit_xor_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_xor_binary_op")) return false;
    if (!nextTokenIs(b, XOR)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, XOR);
    exit_section_(b, m, BINARY_OP, r);
    return r;
  }

  /* ********************************************************** */
  // "{" block_statements "}"
  public static boolean block_body(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_body")) return false;
    if (!nextTokenIs(b, LCURLY)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, BLOCK_BODY, null);
    r = consumeToken(b, LCURLY);
    p = r; // pin = 1
    r = r && report_error_(b, block_statements(b, l + 1));
    r = p && consumeToken(b, RCURLY) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  /* ********************************************************** */
  // !('}' | expr_first | VAR_KW | semi | id)
  static boolean block_element_recover(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_element_recover")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !block_element_recover_0(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // '}' | expr_first | VAR_KW | semi | id
  private static boolean block_element_recover_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_element_recover_0")) return false;
    boolean r;
    r = consumeTokenFast(b, RCURLY);
    if (!r) r = expr_first(b, l + 1);
    if (!r) r = consumeTokenFast(b, VAR_KW);
    if (!r) r = semi(b, l + 1);
    if (!r) r = id(b, l + 1);
    return r;
  }

  /* ********************************************************** */
  // !'}' (statement|expr) semi?
  static boolean block_statement(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_statement")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = block_statement_0(b, l + 1);
    p = r; // pin = 1
    r = r && report_error_(b, block_statement_1(b, l + 1));
    r = p && block_statement_2(b, l + 1) && r;
    exit_section_(b, l, m, r, p, VoltumParser::block_element_recover);
    return r || p;
  }

  // !'}'
  private static boolean block_statement_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_statement_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !consumeToken(b, RCURLY);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // statement|expr
  private static boolean block_statement_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_statement_1")) return false;
    boolean r;
    r = statement(b, l + 1);
    if (!r) r = expr(b, l + 1, -1);
    return r;
  }

  // semi?
  private static boolean block_statement_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_statement_2")) return false;
    semi(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // block_statement*
  static boolean block_statements(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "block_statements")) return false;
    while (true) {
      int c = current_position_(b);
      if (!block_statement(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "block_statements", c)) break;
    }
    return true;
  }

  /* ********************************************************** */
  // ANDAND
  public static boolean bool_and_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bool_and_binary_op")) return false;
    if (!nextTokenIs(b, ANDAND)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, ANDAND);
    exit_section_(b, m, BINARY_OP, r);
    return r;
  }

  /* ********************************************************** */
  // OROR
  public static boolean bool_or_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bool_or_binary_op")) return false;
    if (!nextTokenIs(b, OROR)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, OROR);
    exit_section_(b, m, BINARY_OP, r);
    return r;
  }

  /* ********************************************************** */
  // path? type_argument_list? '(' (expr_list)? ')'
  public static boolean call_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "call_expr")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, CALL_EXPR, "<call expr>");
    r = call_expr_0(b, l + 1);
    r = r && call_expr_1(b, l + 1);
    r = r && consumeToken(b, LPAREN);
    r = r && call_expr_3(b, l + 1);
    r = r && consumeToken(b, RPAREN);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // path?
  private static boolean call_expr_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "call_expr_0")) return false;
    path(b, l + 1);
    return true;
  }

  // type_argument_list?
  private static boolean call_expr_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "call_expr_1")) return false;
    type_argument_list(b, l + 1);
    return true;
  }

  // (expr_list)?
  private static boolean call_expr_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "call_expr_3")) return false;
    call_expr_3_0(b, l + 1);
    return true;
  }

  // (expr_list)
  private static boolean call_expr_3_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "call_expr_3_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = expr_list(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  static Parser comma_separated_list_$(Parser _param) {
    return (b, l) -> comma_separated_list(b, l + 1, _param);
  }

  // <<param>> ( ',' <<param>> )* ','?
  static boolean comma_separated_list(PsiBuilder b, int l, Parser _param) {
    if (!recursion_guard_(b, l, "comma_separated_list")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = _param.parse(b, l);
    r = r && comma_separated_list_1(b, l + 1, _param);
    r = r && comma_separated_list_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ( ',' <<param>> )*
  private static boolean comma_separated_list_1(PsiBuilder b, int l, Parser _param) {
    if (!recursion_guard_(b, l, "comma_separated_list_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!comma_separated_list_1_0(b, l + 1, _param)) break;
      if (!empty_element_parsed_guard_(b, "comma_separated_list_1", c)) break;
    }
    return true;
  }

  // ',' <<param>>
  private static boolean comma_separated_list_1_0(PsiBuilder b, int l, Parser _param) {
    if (!recursion_guard_(b, l, "comma_separated_list_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COMMA);
    r = r && _param.parse(b, l);
    exit_section_(b, m, null, r);
    return r;
  }

  // ','?
  private static boolean comma_separated_list_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "comma_separated_list_2")) return false;
    consumeToken(b, COMMA);
    return true;
  }

  /* ********************************************************** */
  // '==' | '!='
  public static boolean comparison_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "comparison_binary_op")) return false;
    if (!nextTokenIs(b, "<comparison binary op>", EQEQ, EXCLEQ)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<comparison binary op>");
    r = consumeToken(b, EQEQ);
    if (!r) r = consumeToken(b, EXCLEQ);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // MINUSMINUS
  static boolean dec(PsiBuilder b, int l) {
    return consumeToken(b, MINUSMINUS);
  }

  /* ********************************************************** */
  // (field_id/*|literal_string|literal_int*/) ':' any_expr
  public static boolean dictionary_field(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_field")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, DICTIONARY_FIELD, "<dictionary field>");
    r = dictionary_field_0(b, l + 1);
    r = r && consumeToken(b, COLON);
    r = r && any_expr(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // (field_id/*|literal_string|literal_int*/)
  private static boolean dictionary_field_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_field_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = field_id(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // dictionary_field (',' dictionary_field)* ','?
  static boolean dictionary_fields(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_fields")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = dictionary_field(b, l + 1);
    r = r && dictionary_fields_1(b, l + 1);
    r = r && dictionary_fields_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // (',' dictionary_field)*
  private static boolean dictionary_fields_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_fields_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!dictionary_fields_1_0(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "dictionary_fields_1", c)) break;
    }
    return true;
  }

  // ',' dictionary_field
  private static boolean dictionary_fields_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_fields_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COMMA);
    r = r && dictionary_field(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ','?
  private static boolean dictionary_fields_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_fields_2")) return false;
    consumeToken(b, COMMA);
    return true;
  }

  /* ********************************************************** */
  // '{' dictionary_fields? '}'
  public static boolean dictionary_value(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_value")) return false;
    if (!nextTokenIs(b, LCURLY)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, DICTIONARY_VALUE, null);
    r = consumeToken(b, LCURLY);
    p = r; // pin = 1
    r = r && report_error_(b, dictionary_value_1(b, l + 1));
    r = p && consumeToken(b, RCURLY) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // dictionary_fields?
  private static boolean dictionary_value_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "dictionary_value_1")) return false;
    dictionary_fields(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // !('if' '{') block_body | if_statement
  static boolean else_chain(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "else_chain")) return false;
    if (!nextTokenIs(b, "", IF_KW, LCURLY)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = else_chain_0(b, l + 1);
    if (!r) r = if_statement(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !('if' '{') block_body
  private static boolean else_chain_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "else_chain_0")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = else_chain_0_0(b, l + 1);
    p = r; // pin = 1
    r = r && block_body(b, l + 1);
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // !('if' '{')
  private static boolean else_chain_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "else_chain_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !else_chain_0_0_0(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // 'if' '{'
  private static boolean else_chain_0_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "else_chain_0_0_0")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = consumeTokens(b, 1, IF_KW, LCURLY);
    p = r; // pin = 1
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  /* ********************************************************** */
  // 'else' else_chain
  public static boolean else_statement(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "else_statement")) return false;
    if (!nextTokenIs(b, ELSE_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, ELSE_KW);
    r = r && else_chain(b, l + 1);
    exit_section_(b, m, ELSE_STATEMENT, r);
    return r;
  }

  /* ********************************************************** */
  // RETURN_KW | '|' | '{' | '[' | '(' | '..' | '...'
  //   | '+' | '-' | '*' | '!' | '&' | literal_value_expr_first | FOR_KW | IF_KW | CONTINUE_KW | BREAK_KW
  //   | YIELD_KW | ASYNC_KW
  static boolean expr_first(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expr_first")) return false;
    boolean r;
    r = consumeTokenFast(b, RETURN_KW);
    if (!r) r = consumeTokenFast(b, OR);
    if (!r) r = consumeTokenFast(b, LCURLY);
    if (!r) r = consumeTokenFast(b, LBRACK);
    if (!r) r = consumeTokenFast(b, LPAREN);
    if (!r) r = consumeTokenFast(b, DOTDOT);
    if (!r) r = consumeTokenFast(b, DOTDOTDOT);
    if (!r) r = consumeTokenFast(b, PLUS);
    if (!r) r = consumeTokenFast(b, MINUS);
    if (!r) r = consumeTokenFast(b, MUL);
    if (!r) r = consumeTokenFast(b, EXCL);
    if (!r) r = consumeTokenFast(b, AND);
    if (!r) r = literal_value_expr_first(b, l + 1);
    if (!r) r = consumeTokenFast(b, FOR_KW);
    if (!r) r = consumeTokenFast(b, IF_KW);
    if (!r) r = consumeTokenFast(b, CONTINUE_KW);
    if (!r) r = consumeTokenFast(b, BREAK_KW);
    if (!r) r = consumeTokenFast(b, YIELD_KW);
    if (!r) r = consumeTokenFast(b, ASYNC_KW);
    return r;
  }

  /* ********************************************************** */
  // expr ( ',' expr )* ','?
  static boolean expr_list(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expr_list")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = expr(b, l + 1, -1);
    r = r && expr_list_1(b, l + 1);
    r = r && expr_list_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ( ',' expr )*
  private static boolean expr_list_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expr_list_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!expr_list_1_0(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "expr_list_1", c)) break;
    }
    return true;
  }

  // ',' expr
  private static boolean expr_list_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expr_list_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COMMA);
    r = r && expr(b, l + 1, -1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ','?
  private static boolean expr_list_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expr_list_2")) return false;
    consumeToken(b, COMMA);
    return true;
  }

  /* ********************************************************** */
  // expr?
  public static boolean expression_code_fragment_element(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "expression_code_fragment_element")) return false;
    Marker m = enter_section_(b, l, _NONE_, EXPRESSION_CODE_FRAGMENT_ELEMENT, "<expression code fragment element>");
    expr(b, l + 1, -1);
    exit_section_(b, l, m, true, false, null);
    return true;
  }

  /* ********************************************************** */
  // id
  public static boolean field_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "field_id")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, FIELD_ID, "<field id>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // expr ';'
  static boolean for_condition(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_condition")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = expr(b, l + 1, -1);
    r = r && consumeToken(b, SEMICOLON);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // variable_declaration? ';'?
  static boolean for_initializer(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_initializer")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = for_initializer_0(b, l + 1);
    r = r && for_initializer_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // variable_declaration?
  private static boolean for_initializer_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_initializer_0")) return false;
    variable_declaration(b, l + 1);
    return true;
  }

  // ';'?
  private static boolean for_initializer_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_initializer_1")) return false;
    consumeToken(b, SEMICOLON);
    return true;
  }

  /* ********************************************************** */
  // FOR_KW 
  // ( 
  //     LPAREN
  //         (for_initializer for_condition? for_update?) 
  //     RPAREN 
  // )?
  // block_body
  public static boolean for_loop_statement(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement")) return false;
    if (!nextTokenIs(b, FOR_KW)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, FOR_LOOP_STATEMENT, null);
    r = consumeToken(b, FOR_KW);
    p = r; // pin = 1
    r = r && report_error_(b, for_loop_statement_1(b, l + 1));
    r = p && block_body(b, l + 1) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // ( 
  //     LPAREN
  //         (for_initializer for_condition? for_update?) 
  //     RPAREN 
  // )?
  private static boolean for_loop_statement_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement_1")) return false;
    for_loop_statement_1_0(b, l + 1);
    return true;
  }

  // LPAREN
  //         (for_initializer for_condition? for_update?) 
  //     RPAREN
  private static boolean for_loop_statement_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement_1_0")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = consumeToken(b, LPAREN);
    p = r; // pin = 1
    r = r && report_error_(b, for_loop_statement_1_0_1(b, l + 1));
    r = p && consumeToken(b, RPAREN) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // for_initializer for_condition? for_update?
  private static boolean for_loop_statement_1_0_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement_1_0_1")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = for_initializer(b, l + 1);
    p = r; // pin = 1
    r = r && report_error_(b, for_loop_statement_1_0_1_1(b, l + 1));
    r = p && for_loop_statement_1_0_1_2(b, l + 1) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // for_condition?
  private static boolean for_loop_statement_1_0_1_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement_1_0_1_1")) return false;
    for_condition(b, l + 1);
    return true;
  }

  // for_update?
  private static boolean for_loop_statement_1_0_1_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "for_loop_statement_1_0_1_2")) return false;
    for_update(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // expr
  static boolean for_update(PsiBuilder b, int l) {
    return expr(b, l + 1, -1);
  }

  /* ********************************************************** */
  // attribute* DEF_KW? ASYNC_KW? COROUTINE_KW? FUNC_KW func_id type_argument_list? argument_declaration_list type_ref? block_body?
  public static boolean func_declaration(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, FUNC_DECLARATION, "<func declaration>");
    r = func_declaration_0(b, l + 1);
    r = r && func_declaration_1(b, l + 1);
    r = r && func_declaration_2(b, l + 1);
    r = r && func_declaration_3(b, l + 1);
    r = r && consumeToken(b, FUNC_KW);
    r = r && func_id(b, l + 1);
    p = r; // pin = func_id
    r = r && report_error_(b, func_declaration_6(b, l + 1));
    r = p && report_error_(b, argument_declaration_list(b, l + 1)) && r;
    r = p && report_error_(b, func_declaration_8(b, l + 1)) && r;
    r = p && func_declaration_9(b, l + 1) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // attribute*
  private static boolean func_declaration_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "func_declaration_0", c)) break;
    }
    return true;
  }

  // DEF_KW?
  private static boolean func_declaration_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_1")) return false;
    consumeToken(b, DEF_KW);
    return true;
  }

  // ASYNC_KW?
  private static boolean func_declaration_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_2")) return false;
    consumeToken(b, ASYNC_KW);
    return true;
  }

  // COROUTINE_KW?
  private static boolean func_declaration_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_3")) return false;
    consumeToken(b, COROUTINE_KW);
    return true;
  }

  // type_argument_list?
  private static boolean func_declaration_6(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_6")) return false;
    type_argument_list(b, l + 1);
    return true;
  }

  // type_ref?
  private static boolean func_declaration_8(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_8")) return false;
    type_ref(b, l + 1);
    return true;
  }

  // block_body?
  private static boolean func_declaration_9(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_declaration_9")) return false;
    block_body(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // id
  public static boolean func_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "func_id")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, FUNC_ID, "<func id>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // <<parseIdent>> | ID
  static boolean id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "id")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = parseIdent(b, l + 1);
    if (!r) r = consumeToken(b, ID);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // type_ref argument_id
  public static boolean identifier_with_type(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "identifier_with_type")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, IDENTIFIER_WITH_TYPE, "<identifier with type>");
    r = type_ref(b, l + 1);
    r = r && argument_id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // 'if' '(' any_expr ')' block_body else_statement?
  public static boolean if_statement(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "if_statement")) return false;
    if (!nextTokenIs(b, IF_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokens(b, 0, IF_KW, LPAREN);
    r = r && any_expr(b, l + 1);
    r = r && consumeToken(b, RPAREN);
    r = r && block_body(b, l + 1);
    r = r && if_statement_5(b, l + 1);
    exit_section_(b, m, IF_STATEMENT, r);
    return r;
  }

  // else_statement?
  private static boolean if_statement_5(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "if_statement_5")) return false;
    else_statement(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // PLUSPLUS
  static boolean inc(PsiBuilder b, int l) {
    return consumeToken(b, PLUSPLUS);
  }

  /* ********************************************************** */
  // '[' expr_list? ']'
  public static boolean list_value(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "list_value")) return false;
    if (!nextTokenIs(b, LBRACK)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, LIST_VALUE, null);
    r = consumeToken(b, LBRACK);
    p = r; // pin = 1
    r = r && report_error_(b, list_value_1(b, l + 1));
    r = p && consumeToken(b, RBRACK) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // expr_list?
  private static boolean list_value_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "list_value_1")) return false;
    expr_list(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // VALUE_BOOL
  public static boolean literal_bool(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_bool")) return false;
    if (!nextTokenIs(b, VALUE_BOOL)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, VALUE_BOOL);
    exit_section_(b, m, LITERAL_BOOL, r);
    return r;
  }

  /* ********************************************************** */
  // literal_int
  //     | literal_float
  //     | literal_string
  //     | literal_bool
  //     | literal_null
  public static boolean literal_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_expr")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, LITERAL_EXPR, "<literal expr>");
    r = literal_int(b, l + 1);
    if (!r) r = literal_float(b, l + 1);
    if (!r) r = literal_string(b, l + 1);
    if (!r) r = literal_bool(b, l + 1);
    if (!r) r = literal_null(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // VALUE_FLOAT
  public static boolean literal_float(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_float")) return false;
    if (!nextTokenIs(b, VALUE_FLOAT)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, VALUE_FLOAT);
    exit_section_(b, m, LITERAL_FLOAT, r);
    return r;
  }

  /* ********************************************************** */
  // VALUE_INTEGER
  public static boolean literal_int(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_int")) return false;
    if (!nextTokenIs(b, VALUE_INTEGER)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, VALUE_INTEGER);
    exit_section_(b, m, LITERAL_INT, r);
    return r;
  }

  /* ********************************************************** */
  // VALUE_NULL
  public static boolean literal_null(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_null")) return false;
    if (!nextTokenIs(b, VALUE_NULL)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, VALUE_NULL);
    exit_section_(b, m, LITERAL_NULL, r);
    return r;
  }

  /* ********************************************************** */
  // STRING_LITERAL
  public static boolean literal_string(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_string")) return false;
    if (!nextTokenIs(b, STRING_LITERAL)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, STRING_LITERAL);
    exit_section_(b, m, LITERAL_STRING, r);
    return r;
  }

  /* ********************************************************** */
  // VALUE_INTEGER | VALUE_FLOAT | STRING_LITERAL | VALUE_BOOL | VALUE_NULL
  static boolean literal_value_expr_first(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "literal_value_expr_first")) return false;
    boolean r;
    r = consumeTokenFast(b, VALUE_INTEGER);
    if (!r) r = consumeTokenFast(b, VALUE_FLOAT);
    if (!r) r = consumeTokenFast(b, STRING_LITERAL);
    if (!r) r = consumeTokenFast(b, VALUE_BOOL);
    if (!r) r = consumeTokenFast(b, VALUE_NULL);
    return r;
  }

  /* ********************************************************** */
  // '*' | '/' | '%'
  public static boolean multiplicative_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "multiplicative_binary_op")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<multiplicative binary op>");
    r = consumeToken(b, MUL);
    if (!r) r = consumeToken(b, DIV);
    if (!r) r = consumeToken(b, REM);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // '\n' | '\r\n' | '\r'
  static boolean nl(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "nl")) return false;
    boolean r;
    r = consumeToken(b, "\\n");
    if (!r) r = consumeToken(b, "\\r\\n");
    if (!r) r = consumeToken(b, "\\r");
    return r;
  }

  /* ********************************************************** */
  // <<parseLParens <<comma_separated_list <<param>>>>>>
  static boolean optional_argument_list(PsiBuilder b, int l, Parser _param) {
    return parseLParens(b, l + 1, comma_separated_list_$(_param));
  }

  /* ********************************************************** */
  // path_start (path_member_access | path_index_access)* type_argument_list?
  public static boolean path(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, PATH, "<path>");
    r = path_start(b, l + 1);
    r = r && path_1(b, l + 1);
    r = r && path_2(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // (path_member_access | path_index_access)*
  private static boolean path_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!path_1_0(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "path_1", c)) break;
    }
    return true;
  }

  // path_member_access | path_index_access
  private static boolean path_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_1_0")) return false;
    boolean r;
    r = path_member_access(b, l + 1);
    if (!r) r = path_index_access(b, l + 1);
    return r;
  }

  // type_argument_list?
  private static boolean path_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_2")) return false;
    type_argument_list(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // '[' any_expr ']'
  public static boolean path_index_access(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_index_access")) return false;
    if (!nextTokenIs(b, LBRACK)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _LEFT_, PATH, null);
    r = consumeToken(b, LBRACK);
    r = r && any_expr(b, l + 1);
    r = r && consumeToken(b, RBRACK);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // '.' var_reference?
  public static boolean path_member_access(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_member_access")) return false;
    if (!nextTokenIs(b, DOT)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _LEFT_, PATH, null);
    r = consumeToken(b, DOT);
    r = r && path_member_access_1(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // var_reference?
  private static boolean path_member_access_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_member_access_1")) return false;
    var_reference(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // var_reference type_argument_list?
  public static boolean path_start(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_start")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, PATH, "<path start>");
    r = var_reference(b, l + 1);
    r = r && path_start_1(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // type_argument_list?
  private static boolean path_start_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "path_start_1")) return false;
    type_argument_list(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // root_items
  static boolean program(PsiBuilder b, int l) {
    return root_items(b, l + 1);
  }

  /* ********************************************************** */
  // '<=' | '<' | '>=' | '>'
  public static boolean relational_binary_op(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "relational_binary_op")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, BINARY_OP, "<relational binary op>");
    r = consumeToken(b, LTEQ);
    if (!r) r = consumeToken(b, LT);
    if (!r) r = consumeToken(b, GTEQ);
    if (!r) r = consumeToken(b, GT);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // !top_level_declaration_first
  static boolean root_item_recover(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "root_item_recover")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !top_level_declaration_first(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // !<<eof>> top_level_declaration
  static boolean root_item_with_recover(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "root_item_with_recover")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = root_item_with_recover_0(b, l + 1);
    p = r; // pin = 1
    r = r && top_level_declaration(b, l + 1);
    exit_section_(b, l, m, r, p, VoltumParser::root_item_recover);
    return r || p;
  }

  // !<<eof>>
  private static boolean root_item_with_recover_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "root_item_with_recover_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !eof(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // root_item_with_recover*
  static boolean root_items(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "root_items")) return false;
    while (true) {
      int c = current_position_(b);
      if (!root_item_with_recover(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "root_items", c)) break;
    }
    return true;
  }

  /* ********************************************************** */
  // nl | ';' | <<eof>>
  static boolean semi(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "semi")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = nl(b, l + 1);
    if (!r) r = consumeToken(b, SEMICOLON);
    if (!r) r = eof(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // attribute* SIGNAL_KW id argument_declaration_list
  public static boolean signal_declaration(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "signal_declaration")) return false;
    if (!nextTokenIs(b, "<signal declaration>", LBRACK, SIGNAL_KW)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, SIGNAL_DECLARATION, "<signal declaration>");
    r = signal_declaration_0(b, l + 1);
    r = r && consumeToken(b, SIGNAL_KW);
    r = r && id(b, l + 1);
    p = r; // pin = id
    r = r && argument_declaration_list(b, l + 1);
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // attribute*
  private static boolean signal_declaration_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "signal_declaration_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "signal_declaration_0", c)) break;
    }
    return true;
  }

  /* ********************************************************** */
  // if_statement
  //     | for_loop_statement
  //     | variable_declaration 
  //     | return_expr
  public static boolean statement(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "statement")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, STATEMENT, "<statement>");
    r = if_statement(b, l + 1);
    if (!r) r = for_loop_statement(b, l + 1);
    if (!r) r = variable_declaration(b, l + 1);
    if (!r) r = return_expr(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // statement?
  public static boolean statement_code_fragment_element(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "statement_code_fragment_element")) return false;
    Marker m = enter_section_(b, l, _NONE_, STATEMENT_CODE_FRAGMENT_ELEMENT, "<statement code fragment element>");
    statement(b, l + 1);
    exit_section_(b, l, m, true, false, null);
    return true;
  }

  /* ********************************************************** */
  // (
  //   type_declaration
  //   | func_declaration 
  //   | signal_declaration
  //   | for_loop_statement
  //   | statement
  //   | !SIGNAL_KW expr
  // ) semi?
  static boolean top_level_declaration(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = top_level_declaration_0(b, l + 1);
    r = r && top_level_declaration_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // type_declaration
  //   | func_declaration 
  //   | signal_declaration
  //   | for_loop_statement
  //   | statement
  //   | !SIGNAL_KW expr
  private static boolean top_level_declaration_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = type_declaration(b, l + 1);
    if (!r) r = func_declaration(b, l + 1);
    if (!r) r = signal_declaration(b, l + 1);
    if (!r) r = for_loop_statement(b, l + 1);
    if (!r) r = statement(b, l + 1);
    if (!r) r = top_level_declaration_0_5(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !SIGNAL_KW expr
  private static boolean top_level_declaration_0_5(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration_0_5")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = top_level_declaration_0_5_0(b, l + 1);
    r = r && expr(b, l + 1, -1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !SIGNAL_KW
  private static boolean top_level_declaration_0_5_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration_0_5_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !consumeToken(b, SIGNAL_KW);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // semi?
  private static boolean top_level_declaration_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration_1")) return false;
    semi(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // FUNC_KW | SIGNAL_KW | TYPE_KW | ASYNC_KW | AWAIT_KW | DEF_KW | VAR_KW | FOR_KW | RETURN_KW | id | ID
  static boolean top_level_declaration_first(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "top_level_declaration_first")) return false;
    boolean r;
    r = consumeTokenFast(b, FUNC_KW);
    if (!r) r = consumeTokenFast(b, SIGNAL_KW);
    if (!r) r = consumeTokenFast(b, TYPE_KW);
    if (!r) r = consumeTokenFast(b, ASYNC_KW);
    if (!r) r = consumeTokenFast(b, AWAIT_KW);
    if (!r) r = consumeTokenFast(b, DEF_KW);
    if (!r) r = consumeTokenFast(b, VAR_KW);
    if (!r) r = consumeTokenFast(b, FOR_KW);
    if (!r) r = consumeTokenFast(b, RETURN_KW);
    if (!r) r = id(b, l + 1);
    if (!r) r = consumeTokenFast(b, ID);
    return r;
  }

  /* ********************************************************** */
  // LPAREN <<comma_separated_list <<any_expr>> >> RPAREN
  static boolean tuple_expr_list(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_list")) return false;
    if (!nextTokenIs(b, LPAREN)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_);
    r = consumeToken(b, LPAREN);
    p = r; // pin = 1
    r = r && report_error_(b, comma_separated_list(b, l + 1, tuple_expr_list_1_0_parser_));
    r = p && consumeToken(b, RPAREN) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  /* ********************************************************** */
  // ',' [ any_expr (',' any_expr)* ','? ] RPAREN
  public static boolean tuple_expr_upper(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper")) return false;
    if (!nextTokenIs(b, COMMA)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _UPPER_, TUPLE_EXPR, null);
    r = consumeToken(b, COMMA);
    p = r; // pin = 1
    r = r && report_error_(b, tuple_expr_upper_1(b, l + 1));
    r = p && consumeToken(b, RPAREN) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // [ any_expr (',' any_expr)* ','? ]
  private static boolean tuple_expr_upper_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper_1")) return false;
    tuple_expr_upper_1_0(b, l + 1);
    return true;
  }

  // any_expr (',' any_expr)* ','?
  private static boolean tuple_expr_upper_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = any_expr(b, l + 1);
    r = r && tuple_expr_upper_1_0_1(b, l + 1);
    r = r && tuple_expr_upper_1_0_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // (',' any_expr)*
  private static boolean tuple_expr_upper_1_0_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper_1_0_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!tuple_expr_upper_1_0_1_0(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "tuple_expr_upper_1_0_1", c)) break;
    }
    return true;
  }

  // ',' any_expr
  private static boolean tuple_expr_upper_1_0_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper_1_0_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COMMA);
    r = r && any_expr(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ','?
  private static boolean tuple_expr_upper_1_0_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_expr_upper_1_0_2")) return false;
    consumeToken(b, COMMA);
    return true;
  }

  /* ********************************************************** */
  // LPAREN any_expr (tuple_expr_upper | RPAREN)
  public static boolean tuple_or_paren_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_or_paren_expr")) return false;
    if (!nextTokenIsFast(b, LPAREN)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, PAREN_EXPR, null);
    r = consumeTokenFast(b, LPAREN);
    r = r && any_expr(b, l + 1);
    p = r; // pin = 2
    r = r && tuple_or_paren_expr_2(b, l + 1);
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // tuple_expr_upper | RPAREN
  private static boolean tuple_or_paren_expr_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "tuple_or_paren_expr_2")) return false;
    boolean r;
    r = tuple_expr_upper(b, l + 1);
    if (!r) r = consumeTokenFast(b, RPAREN);
    return r;
  }

  /* ********************************************************** */
  // LT <<comma_separated_list <<type_ref>> >> GT
  public static boolean type_argument_list(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_argument_list")) return false;
    if (!nextTokenIs(b, LT)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, LT);
    r = r && comma_separated_list(b, l + 1, type_argument_list_1_0_parser_);
    r = r && consumeToken(b, GT);
    exit_section_(b, m, TYPE_ARGUMENT_LIST, r);
    return r;
  }

  /* ********************************************************** */
  // attribute* type_declaration_header type_declaration_body
  public static boolean type_declaration(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration")) return false;
    if (!nextTokenIs(b, "<type declaration>", LBRACK, TYPE_KW)) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, TYPE_DECLARATION, "<type declaration>");
    r = type_declaration_0(b, l + 1);
    r = r && type_declaration_header(b, l + 1);
    r = r && type_declaration_body(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // attribute*
  private static boolean type_declaration_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_0", c)) break;
    }
    return true;
  }

  /* ********************************************************** */
  // '{' type_declaration_member* '}'
  public static boolean type_declaration_body(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_body")) return false;
    if (!nextTokenIs(b, LCURLY)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, TYPE_DECLARATION_BODY, null);
    r = consumeToken(b, LCURLY);
    p = r; // pin = 1
    r = r && report_error_(b, type_declaration_body_1(b, l + 1));
    r = p && consumeToken(b, RCURLY) && r;
    exit_section_(b, l, m, r, p, null);
    return r || p;
  }

  // type_declaration_member*
  private static boolean type_declaration_body_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_body_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!type_declaration_member(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_body_1", c)) break;
    }
    return true;
  }

  /* ********************************************************** */
  // attribute* DEF_KW? func_id argument_declaration_list block_body? semi?
  public static boolean type_declaration_constructor(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_constructor")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, TYPE_DECLARATION_CONSTRUCTOR, "<type declaration constructor>");
    r = type_declaration_constructor_0(b, l + 1);
    r = r && type_declaration_constructor_1(b, l + 1);
    r = r && func_id(b, l + 1);
    r = r && argument_declaration_list(b, l + 1);
    r = r && type_declaration_constructor_4(b, l + 1);
    r = r && type_declaration_constructor_5(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // attribute*
  private static boolean type_declaration_constructor_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_constructor_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_constructor_0", c)) break;
    }
    return true;
  }

  // DEF_KW?
  private static boolean type_declaration_constructor_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_constructor_1")) return false;
    consumeToken(b, DEF_KW);
    return true;
  }

  // block_body?
  private static boolean type_declaration_constructor_4(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_constructor_4")) return false;
    block_body(b, l + 1);
    return true;
  }

  // semi?
  private static boolean type_declaration_constructor_5(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_constructor_5")) return false;
    semi(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // attribute* !DEF_KW var_id !argument_declaration_list type_ref semi?
  public static boolean type_declaration_field_member(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_field_member")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, TYPE_DECLARATION_FIELD_MEMBER, "<type declaration field member>");
    r = type_declaration_field_member_0(b, l + 1);
    r = r && type_declaration_field_member_1(b, l + 1);
    r = r && var_id(b, l + 1);
    r = r && type_declaration_field_member_3(b, l + 1);
    r = r && type_ref(b, l + 1);
    r = r && type_declaration_field_member_5(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // attribute*
  private static boolean type_declaration_field_member_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_field_member_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_field_member_0", c)) break;
    }
    return true;
  }

  // !DEF_KW
  private static boolean type_declaration_field_member_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_field_member_1")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !consumeToken(b, DEF_KW);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // !argument_declaration_list
  private static boolean type_declaration_field_member_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_field_member_3")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !argument_declaration_list(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // semi?
  private static boolean type_declaration_field_member_5(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_field_member_5")) return false;
    semi(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // attribute* 'type' type_id ('struct' | 'interface' | 'enum')
  static boolean type_declaration_header(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_header")) return false;
    if (!nextTokenIs(b, "", LBRACK, TYPE_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = type_declaration_header_0(b, l + 1);
    r = r && consumeToken(b, TYPE_KW);
    r = r && type_id(b, l + 1);
    r = r && type_declaration_header_3(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // attribute*
  private static boolean type_declaration_header_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_header_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_header_0", c)) break;
    }
    return true;
  }

  // 'struct' | 'interface' | 'enum'
  private static boolean type_declaration_header_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_header_3")) return false;
    boolean r;
    r = consumeToken(b, STRUCT_KW);
    if (!r) r = consumeToken(b, INTERFACE_KW);
    if (!r) r = consumeToken(b, ENUM_KW);
    return r;
  }

  /* ********************************************************** */
  // type_declaration_field_member | type_declaration_method_member | type_declaration_constructor
  static boolean type_declaration_member(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_member")) return false;
    boolean r;
    r = type_declaration_field_member(b, l + 1);
    if (!r) r = type_declaration_method_member(b, l + 1);
    if (!r) r = type_declaration_constructor(b, l + 1);
    return r;
  }

  /* ********************************************************** */
  // attribute* DEF_KW? ASYNC_KW? COROUTINE_KW? func_id argument_declaration_list type_ref? block_body? semi?
  public static boolean type_declaration_method_member(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, TYPE_DECLARATION_METHOD_MEMBER, "<type declaration method member>");
    r = type_declaration_method_member_0(b, l + 1);
    r = r && type_declaration_method_member_1(b, l + 1);
    r = r && type_declaration_method_member_2(b, l + 1);
    r = r && type_declaration_method_member_3(b, l + 1);
    r = r && func_id(b, l + 1);
    r = r && argument_declaration_list(b, l + 1);
    r = r && type_declaration_method_member_6(b, l + 1);
    r = r && type_declaration_method_member_7(b, l + 1);
    r = r && type_declaration_method_member_8(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // attribute*
  private static boolean type_declaration_method_member_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_0")) return false;
    while (true) {
      int c = current_position_(b);
      if (!attribute(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "type_declaration_method_member_0", c)) break;
    }
    return true;
  }

  // DEF_KW?
  private static boolean type_declaration_method_member_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_1")) return false;
    consumeToken(b, DEF_KW);
    return true;
  }

  // ASYNC_KW?
  private static boolean type_declaration_method_member_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_2")) return false;
    consumeToken(b, ASYNC_KW);
    return true;
  }

  // COROUTINE_KW?
  private static boolean type_declaration_method_member_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_3")) return false;
    consumeToken(b, COROUTINE_KW);
    return true;
  }

  // type_ref?
  private static boolean type_declaration_method_member_6(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_6")) return false;
    type_ref(b, l + 1);
    return true;
  }

  // block_body?
  private static boolean type_declaration_method_member_7(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_7")) return false;
    block_body(b, l + 1);
    return true;
  }

  // semi?
  private static boolean type_declaration_method_member_8(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_declaration_method_member_8")) return false;
    semi(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // id
  public static boolean type_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_id")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, TYPE_ID, "<type id>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // id type_argument_list?
  static boolean type_name_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_name_id")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = id(b, l + 1);
    r = r && type_name_id_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // type_argument_list?
  private static boolean type_name_id_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_name_id_1")) return false;
    type_argument_list(b, l + 1);
    return true;
  }

  /* ********************************************************** */
  // type_name_id '[]'
  static boolean type_name_id_array(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_name_id_array")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = type_name_id(b, l + 1);
    r = r && consumeToken(b, BRACKET_PAIR);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // variadic_type_id | type_name_id | type_name_id_array
  public static boolean type_ref(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "type_ref")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NONE_, TYPE_REF, "<type ref>");
    r = variadic_type_id(b, l + 1);
    if (!r) r = type_name_id(b, l + 1);
    if (!r) r = type_name_id_array(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // id
  public static boolean var_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "var_id")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, VAR_ID, "<var id>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // <<comma_separated_list <<var_id>> >>
  static boolean var_id_list(PsiBuilder b, int l) {
    return comma_separated_list(b, l + 1, var_id_list_0_0_parser_);
  }

  /* ********************************************************** */
  // id
  public static boolean var_reference(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "var_reference")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, VAR_REFERENCE, "<var reference>");
    r = id(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  /* ********************************************************** */
  // VAR_KW (
  //     (!LPAREN var_id [ '=' any_expr ]) | 
  //     (LPAREN (var_id ( ',' var_id )* ','?) RPAREN [ '=' (any_expr|tuple_expr_list) ])
  // )
  public static boolean variable_declaration(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration")) return false;
    if (!nextTokenIs(b, VAR_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, VAR_KW);
    r = r && variable_declaration_1(b, l + 1);
    exit_section_(b, m, VARIABLE_DECLARATION, r);
    return r;
  }

  // (!LPAREN var_id [ '=' any_expr ]) | 
  //     (LPAREN (var_id ( ',' var_id )* ','?) RPAREN [ '=' (any_expr|tuple_expr_list) ])
  private static boolean variable_declaration_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = variable_declaration_1_0(b, l + 1);
    if (!r) r = variable_declaration_1_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !LPAREN var_id [ '=' any_expr ]
  private static boolean variable_declaration_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = variable_declaration_1_0_0(b, l + 1);
    r = r && var_id(b, l + 1);
    r = r && variable_declaration_1_0_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !LPAREN
  private static boolean variable_declaration_1_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !consumeToken(b, LPAREN);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // [ '=' any_expr ]
  private static boolean variable_declaration_1_0_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_0_2")) return false;
    variable_declaration_1_0_2_0(b, l + 1);
    return true;
  }

  // '=' any_expr
  private static boolean variable_declaration_1_0_2_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_0_2_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, EQ);
    r = r && any_expr(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // LPAREN (var_id ( ',' var_id )* ','?) RPAREN [ '=' (any_expr|tuple_expr_list) ]
  private static boolean variable_declaration_1_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, LPAREN);
    r = r && variable_declaration_1_1_1(b, l + 1);
    r = r && consumeToken(b, RPAREN);
    r = r && variable_declaration_1_1_3(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // var_id ( ',' var_id )* ','?
  private static boolean variable_declaration_1_1_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_1")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = var_id(b, l + 1);
    r = r && variable_declaration_1_1_1_1(b, l + 1);
    r = r && variable_declaration_1_1_1_2(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ( ',' var_id )*
  private static boolean variable_declaration_1_1_1_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_1_1")) return false;
    while (true) {
      int c = current_position_(b);
      if (!variable_declaration_1_1_1_1_0(b, l + 1)) break;
      if (!empty_element_parsed_guard_(b, "variable_declaration_1_1_1_1", c)) break;
    }
    return true;
  }

  // ',' var_id
  private static boolean variable_declaration_1_1_1_1_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_1_1_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, COMMA);
    r = r && var_id(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // ','?
  private static boolean variable_declaration_1_1_1_2(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_1_2")) return false;
    consumeToken(b, COMMA);
    return true;
  }

  // [ '=' (any_expr|tuple_expr_list) ]
  private static boolean variable_declaration_1_1_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_3")) return false;
    variable_declaration_1_1_3_0(b, l + 1);
    return true;
  }

  // '=' (any_expr|tuple_expr_list)
  private static boolean variable_declaration_1_1_3_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_3_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, EQ);
    r = r && variable_declaration_1_1_3_0_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // any_expr|tuple_expr_list
  private static boolean variable_declaration_1_1_3_0_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variable_declaration_1_1_3_0_1")) return false;
    boolean r;
    r = any_expr(b, l + 1);
    if (!r) r = tuple_expr_list(b, l + 1);
    return r;
  }

  /* ********************************************************** */
  // '...' type_name_id
  static boolean variadic_type_id(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "variadic_type_id")) return false;
    if (!nextTokenIs(b, DOTDOTDOT)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeToken(b, DOTDOTDOT);
    r = r && type_name_id(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  /* ********************************************************** */
  // Expression root: expr
  // Operator priority table:
  // 0: ATOM(return_expr)
  // 1: ATOM(continue_expr)
  // 2: ATOM(break_expr)
  // 3: ATOM(defer_expr)
  // 4: PREFIX(range_expr)
  // 5: PREFIX(await_expr)
  // 6: BINARY(assign_binary_expr)
  // 7: BINARY(bool_or_binary_expr)
  // 8: BINARY(bool_and_binary_expr)
  // 9: BINARY(comp_binary_expr)
  // 10: BINARY(rel_comp_binary_expr)
  // 11: BINARY(bit_or_binary_expr)
  // 12: BINARY(bit_xor_binary_expr)
  // 13: BINARY(bit_and_binary_expr)
  // 14: BINARY(bit_shift_binary_expr)
  // 15: PREFIX(prefix_inc_expr)
  // 16: PREFIX(prefix_dec_expr)
  // 17: POSTFIX(postfix_inc_expr)
  // 18: POSTFIX(postfix_dec_expr)
  // 19: BINARY(add_binary_expr)
  // 20: BINARY(mul_binary_expr)
  // 21: PREFIX(unary_expr)
  // 22: ATOM(atom_expr)
  public static boolean expr(PsiBuilder b, int l, int g) {
    if (!recursion_guard_(b, l, "expr")) return false;
    addVariant(b, "<expr>");
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, "<expr>");
    r = return_expr(b, l + 1);
    if (!r) r = continue_expr(b, l + 1);
    if (!r) r = break_expr(b, l + 1);
    if (!r) r = defer_expr(b, l + 1);
    if (!r) r = range_expr(b, l + 1);
    if (!r) r = await_expr(b, l + 1);
    if (!r) r = prefix_inc_expr(b, l + 1);
    if (!r) r = prefix_dec_expr(b, l + 1);
    if (!r) r = unary_expr(b, l + 1);
    if (!r) r = atom_expr(b, l + 1);
    p = r;
    r = r && expr_0(b, l + 1, g);
    exit_section_(b, l, m, null, r, p, null);
    return r || p;
  }

  public static boolean expr_0(PsiBuilder b, int l, int g) {
    if (!recursion_guard_(b, l, "expr_0")) return false;
    boolean r = true;
    while (true) {
      Marker m = enter_section_(b, l, _LEFT_, null);
      if (g < 6 && assign_binary_op(b, l + 1)) {
        r = expr(b, l, 5);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 7 && bool_or_binary_op(b, l + 1)) {
        r = expr(b, l, 7);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 8 && bool_and_binary_op(b, l + 1)) {
        r = expr(b, l, 8);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 9 && comparison_binary_op(b, l + 1)) {
        r = expr(b, l, 9);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 10 && relational_binary_op(b, l + 1)) {
        r = expr(b, l, 10);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 11 && bit_or_binary_expr_0(b, l + 1)) {
        r = expr(b, l, 11);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 12 && bit_xor_binary_op(b, l + 1)) {
        r = expr(b, l, 12);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 13 && bit_and_binary_expr_0(b, l + 1)) {
        r = expr(b, l, 13);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 14 && bit_shift_binary_op(b, l + 1)) {
        r = expr(b, l, 14);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 17 && inc(b, l + 1)) {
        r = true;
        exit_section_(b, l, m, POSTFIX_INC_EXPR, r, true, null);
      }
      else if (g < 18 && postfix_dec_expr_0(b, l + 1)) {
        r = true;
        exit_section_(b, l, m, POSTFIX_DEC_EXPR, r, true, null);
      }
      else if (g < 19 && additive_binary_op(b, l + 1)) {
        r = expr(b, l, 19);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else if (g < 20 && multiplicative_binary_op(b, l + 1)) {
        r = expr(b, l, 20);
        exit_section_(b, l, m, BINARY_EXPR, r, true, null);
      }
      else {
        exit_section_(b, l, m, null, false, false, null);
        break;
      }
    }
    return r;
  }

  // RETURN_KW expr?
  public static boolean return_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "return_expr")) return false;
    if (!nextTokenIsSmart(b, RETURN_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, RETURN_KW);
    r = r && return_expr_1(b, l + 1);
    exit_section_(b, m, RETURN_EXPR, r);
    return r;
  }

  // expr?
  private static boolean return_expr_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "return_expr_1")) return false;
    expr(b, l + 1, -1);
    return true;
  }

  // CONTINUE_KW
  public static boolean continue_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "continue_expr")) return false;
    if (!nextTokenIsSmart(b, CONTINUE_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, CONTINUE_KW);
    exit_section_(b, m, CONTINUE_EXPR, r);
    return r;
  }

  // BREAK_KW literal_int?
  public static boolean break_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "break_expr")) return false;
    if (!nextTokenIsSmart(b, BREAK_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, BREAK_KW);
    r = r && break_expr_1(b, l + 1);
    exit_section_(b, m, BREAK_EXPR, r);
    return r;
  }

  // literal_int?
  private static boolean break_expr_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "break_expr_1")) return false;
    literal_int(b, l + 1);
    return true;
  }

  // DEFER_KW (anonymous_func | block_body)
  public static boolean defer_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "defer_expr")) return false;
    if (!nextTokenIsSmart(b, DEFER_KW)) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, DEFER_KW);
    r = r && defer_expr_1(b, l + 1);
    exit_section_(b, m, DEFER_EXPR, r);
    return r;
  }

  // anonymous_func | block_body
  private static boolean defer_expr_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "defer_expr_1")) return false;
    boolean r;
    r = anonymous_func(b, l + 1);
    if (!r) r = block_body(b, l + 1);
    return r;
  }

  public static boolean range_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "range_expr")) return false;
    if (!nextTokenIsSmart(b, RANGE_KW)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, null);
    r = consumeTokenSmart(b, RANGE_KW);
    p = r;
    r = p && expr(b, l, 4);
    exit_section_(b, l, m, RANGE_EXPR, r, p, null);
    return r || p;
  }

  public static boolean await_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "await_expr")) return false;
    if (!nextTokenIsSmart(b, AWAIT_KW)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, null);
    r = consumeTokenSmart(b, AWAIT_KW);
    p = r;
    r = p && expr(b, l, 5);
    exit_section_(b, l, m, AWAIT_EXPR, r, p, null);
    return r || p;
  }

  // !(OROR) bit_or_binary_op
  private static boolean bit_or_binary_expr_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_or_binary_expr_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = bit_or_binary_expr_0_0(b, l + 1);
    r = r && bit_or_binary_op(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !(OROR)
  private static boolean bit_or_binary_expr_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_or_binary_expr_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !bit_or_binary_expr_0_0_0(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // (OROR)
  private static boolean bit_or_binary_expr_0_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_or_binary_expr_0_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, OROR);
    exit_section_(b, m, null, r);
    return r;
  }

  // !(ANDAND) bit_and_binary_op
  private static boolean bit_and_binary_expr_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_and_binary_expr_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = bit_and_binary_expr_0_0(b, l + 1);
    r = r && bit_and_binary_op(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !(ANDAND)
  private static boolean bit_and_binary_expr_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_and_binary_expr_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !bit_and_binary_expr_0_0_0(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // (ANDAND)
  private static boolean bit_and_binary_expr_0_0_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "bit_and_binary_expr_0_0_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = consumeTokenSmart(b, ANDAND);
    exit_section_(b, m, null, r);
    return r;
  }

  public static boolean prefix_inc_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "prefix_inc_expr")) return false;
    if (!nextTokenIsSmart(b, PLUSPLUS)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, null);
    r = inc(b, l + 1);
    p = r;
    r = p && expr(b, l, 15);
    exit_section_(b, l, m, PREFIX_INC_EXPR, r, p, null);
    return r || p;
  }

  public static boolean prefix_dec_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "prefix_dec_expr")) return false;
    if (!nextTokenIsSmart(b, MINUSMINUS)) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, null);
    r = dec(b, l + 1);
    p = r;
    r = p && expr(b, l, 16);
    exit_section_(b, l, m, PREFIX_DEC_EXPR, r, p, null);
    return r || p;
  }

  // dec !expr_first
  private static boolean postfix_dec_expr_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "postfix_dec_expr_0")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = dec(b, l + 1);
    r = r && postfix_dec_expr_0_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !expr_first
  private static boolean postfix_dec_expr_0_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "postfix_dec_expr_0_1")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !expr_first(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  public static boolean unary_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "unary_expr")) return false;
    boolean r, p;
    Marker m = enter_section_(b, l, _NONE_, null);
    r = unary_expr_0(b, l + 1);
    p = r;
    r = p && expr(b, l, 21);
    exit_section_(b, l, m, UNARY_EXPR, r, p, null);
    return r || p;
  }

  // '-' | '+' | '*' | '!' | '&'
  private static boolean unary_expr_0(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "unary_expr_0")) return false;
    boolean r;
    r = consumeTokenSmart(b, MINUS);
    if (!r) r = consumeTokenSmart(b, PLUS);
    if (!r) r = consumeTokenSmart(b, MUL);
    if (!r) r = consumeTokenSmart(b, EXCL);
    if (!r) r = consumeTokenSmart(b, AND);
    return r;
  }

  // literal_expr
  //     | dictionary_value
  //     | list_value
  //     | path !'('
  //     | call_expr 
  //     | anonymous_func
  //     | tuple_or_paren_expr
  public static boolean atom_expr(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "atom_expr")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _COLLAPSE_, ATOM_EXPR, "<atom expr>");
    r = literal_expr(b, l + 1);
    if (!r) r = dictionary_value(b, l + 1);
    if (!r) r = list_value(b, l + 1);
    if (!r) r = atom_expr_3(b, l + 1);
    if (!r) r = call_expr(b, l + 1);
    if (!r) r = anonymous_func(b, l + 1);
    if (!r) r = tuple_or_paren_expr(b, l + 1);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  // path !'('
  private static boolean atom_expr_3(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "atom_expr_3")) return false;
    boolean r;
    Marker m = enter_section_(b);
    r = path(b, l + 1);
    r = r && atom_expr_3_1(b, l + 1);
    exit_section_(b, m, null, r);
    return r;
  }

  // !'('
  private static boolean atom_expr_3_1(PsiBuilder b, int l) {
    if (!recursion_guard_(b, l, "atom_expr_3_1")) return false;
    boolean r;
    Marker m = enter_section_(b, l, _NOT_);
    r = !consumeTokenSmart(b, LPAREN);
    exit_section_(b, l, m, r, false, null);
    return r;
  }

  static final Parser argument_declaration_list_1_0_0_parser_ = (b, l) -> identifier_with_type(b, l + 1);
  static final Parser tuple_expr_list_1_0_parser_ = (b, l) -> any_expr(b, l + 1);
  static final Parser type_argument_list_1_0_parser_ = (b, l) -> type_ref(b, l + 1);
  static final Parser var_id_list_0_0_parser_ = (b, l) -> var_id(b, l + 1);
}
