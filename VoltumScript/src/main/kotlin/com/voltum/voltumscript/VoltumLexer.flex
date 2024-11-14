package com.voltum.voltumscript;

import com.intellij.lexer.FlexLexer;
import com.intellij.psi.tree.IElementType;
import com.voltum.voltumscript.parser.VoltumTokenTypes;

import static com.voltum.voltumscript.psi.VoltumTypes.*;
import static com.intellij.psi.TokenType.*;

%%

%{
  public VoltumLexer() {
    this((java.io.Reader)null);
  }
%}

%{}
  // Dedicated storage for starting position of some previously successful match
  private int zzPostponedMarkedPos = -1;
  /**
    * Dedicated nested-comment level counter
    */
  private int zzNestedCommentLevel = 0;
%}

%{
IElementType imbueBlockComment() {
  assert(zzNestedCommentLevel == 0);
  yybegin(YYINITIAL);

  zzStartRead = zzPostponedMarkedPos;
  zzPostponedMarkedPos = -1;

  return VoltumTokenTypes.BLOCK_COMMENT;
}
IElementType imbueOuterEolComment(){
  yybegin(YYINITIAL);

  zzStartRead = zzPostponedMarkedPos;
  zzPostponedMarkedPos = -1;

  return VoltumTokenTypes.EOL_COMMENT;
}
%}

%public
%class VoltumLexer
%implements FlexLexer
%function advance
%type IElementType

%s IN_BLOCK_COMMENT
%s IN_OUTER_EOL_COMMENT

%unicode

///////////////////////////////////////////////////////////////////////////////////////////////////
// Whitespaces
///////////////////////////////////////////////////////////////////////////////////////////////////

EOL_WS           = \n | \r | \r\n
LINE_WS          = [\ \t]
WHITE_SPACE_CHAR = {EOL_WS} | {LINE_WS}
WHITE_SPACE      = {WHITE_SPACE_CHAR}+

/*
EOL_WS           = \n | \r | \r\n
LINE_WS          = [\ \t]
WHITE_SPACE_CHAR = {EOL_WS} | {LINE_WS}
WHITE_SPACE      = {WHITE_SPACE_CHAR}+
NL = \R
WS = [ \t\f]
LINE_COMMENT="//".*
*/


//ID=[a-zA-Z_][a-zA-Z_0-9]*
ID         = [_\p{xidstart}][\p{xidcontinue}]*
SUFFIX     = {ID}

// BLOCK_COMMENT_START = "/*"
// BLOCK_COMMENT_END = "*/"
// COMMENT_CONTENT = [^*] | [*]+[^*/]

STRING_DOUBLE_QUOTE = \" ( [^\\\"] | \\[^] )* ( \" {SUFFIX}? | \\ )?
STRING_SINGLE_QUOTE = \' ( [^\\'] | \\[^] )* ( \' {SUFFIX}? | \\ )?
VAL_STRING          = {STRING_DOUBLE_QUOTE} | {STRING_SINGLE_QUOTE}

// DOUBLE_QUOUTE_STRING = "\"" (ESC | [^\"])*? ("\""|EOL_WS)
// SINGLE_QUOUTE_STRING = "\'" (ESC | [^'])*? ("\'"|EOL_WS)

// ESC = "\\" (["\\/"] | "b" | "f" | "n" | "r" | "t" | UNICODE)
// UNICODE = "u" HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT

VAL_INTEGER=[0-9]+

// DECIMALS = [0-9] ("_"? [0-9])*
// OCTAL_DIGIT = [0-7]
// HEX_DIGIT = [0-9a-fA-F]
// BIN_DIGIT = [0-1]
// EXPONENT = [eE] [+-]? DECIMALS

EOL_DOC_LINE  = {LINE_WS}*!(!("///".*)|("////".*))

/*<YYINITIAL> {
    {BLOCK_COMMENT_START}    { yybegin(BLOCK_COMMENT); return VoltumTypes.BLOCK_COMMENT; }
}
<BLOCK_COMMENT> {
  {COMMENT_CONTENT}        { return VoltumTypes.BLOCK_COMMENT; }
  {BLOCK_COMMENT_END}      { yybegin(YYINITIAL); return VoltumTypes.BLOCK_COMMENT; }
}*/

%%
<YYINITIAL> {
      // {WHITE_SPACE_CHAR}+         { return com.intellij.psi.TokenType.WHITE_SPACE; }
      // {NEW_LINE}+                 { return com.intellij.psi.TokenType.WHITE_SPACE; }
      // {LINE_COMMENT}                 { return LINE_COMMENT; }
      // {WHITE_SPACE}                  { return WHITE_SPACE; }
      // {TYPE_NAME_KEYWORDS}           { return VoltumTokenSets.getTYPE_NAME_KEYWORDS(); }
    
      // Characters
      ":"       { return COLON; }
      "::"      { return COLONCOLON; }
      "."       { return DOT; }
      ","       { return COMMA; }
      ";"       { return SEMICOLON; }
      "{"       { return LCURLY; }
      "}"       { return RCURLY; }
      "["       { return LBRACK; }
      "]"       { return RBRACK; }
      "("       { return LPAREN; }
      ")"       { return RPAREN; }
      ".."      { return DOTDOT; }
      "..."     { return DOTDOTDOT; }
      "[]"      { return BRACKET_PAIR;}
      "?"       { return QUESTION;}
      "~"       { return TILDE; }
      
      // Operators      
             
      "|"       { return OR; }    
      "&"       { return AND; }    
      "!"       { return EXCL; }
      "="       { return EQ; }
      "!="      { return EXCLEQ; } 
      "=="      { return EQEQ; } 
      "+="      { return PLUSEQ; }           
      "+"       { return PLUS; }     
      "++"      { return PLUSPLUS; }     
      "-="      { return MINUSEQ; }           
      "-"       { return MINUS; }     
      "--"      { return MINUSMINUS; }     
      "|="      { return OREQ; }           
      "&&"      { return ANDAND; }           
      "&="      { return ANDEQ; }          
      "<"       { return LT; }          
      "^="      { return XOREQ; }           
      "^"       { return XOR; }          
      "*="      { return MULEQ; }           
      "*"       { return MUL; }          
      "/="      { return DIVEQ; }           
      "/"       { return DIV; }          
      "%="      { return REMEQ; }           
      "%"       { return REM; }          
      ">"       { return GT; }          
      "."       { return DOT; }          
      ".."      { return DOTDOT; }           
      "..."     { return DOTDOTDOT; }            
      "=>"      { return FAT_ARROW; }           
      "->"      { return ARROW; }           
      ">>="     { return GTGTEQ; }            
      ">>"      { return GTGT; }           
      ">="      { return GTEQ; }           
      "<<="     { return LTLTEQ; }            
      "<<"      { return LTLT; }           
      "<="      { return LTEQ; }           
      "||"      { return OROR; }           
      "&&"      { return ANDAND; }           
    
      // Keywords
      "var"                          { return VAR_KW; }
      "type"                         { return TYPE_KW; }
      "struct"                       { return STRUCT_KW; }
      "interface"                    { return INTERFACE_KW; }
      "enum"                         { return ENUM_KW; }
      "function"                     { return FUNC_KW; }
      "signal"                       { return SIGNAL_KW; }
      "return"                       { return RETURN_KW; }
      "break"                        { return BREAK_KW; }
      "continue"                     { return CONTINUE_KW; }
      "if"                           { return IF_KW; }
      "else"                         { return ELSE_KW; }
      "for"                          { return FOR_KW; }
      "defer"                        { return DEFER_KW; }
      "def"                          { return DEF_KW; }
      "async"                        { return ASYNC_KW; }
      "await"                        { return AWAIT_KW; }
      "coroutine"                    { return COROUTINE_KW; }      
      "range"                        { return RANGE_KW; }
      "int" | "int32" | "i32"        { return INT_KW; }
      "float" | "float32" | "f32"    { return FLOAT_KW; }
      "double" | "float64" | "f64"   { return DOUBLE_KW; }
      "string" | "str"               { return STRING_KW; }
      "bool" | "boolean"             { return BOOL_KW; }
      "object" | "Object"            { return OBJECT_KW; }
      "array" | "Array"              { return ARRAY_KW; }
         
      "null" | "NULL"                { return VALUE_NULL; }
      "true" | "false"               { return VALUE_BOOL; }
      {VAL_STRING}                   { return STRING_LITERAL; }
      {VAL_INTEGER}                  { return VALUE_INTEGER; }
      // {DOUBLE_QUOUTE_STRING}      { return DOUBLE_QUOUTE_STRING; }
      // {SINGLE_QUOUTE_STRING}      { return SINGLE_QUOUTE_STRING; }
      
      "-"? [0-9]* "." [0-9]+ [eE][+\-]? [0-9]+ "f"  { return VALUE_FLOAT; }
      "-"? [0-9]+ [eE][+\-]? [0-9]+ "f"             { return VALUE_FLOAT; }
      "-"? [0-9]+ "f"                               { return VALUE_FLOAT; }
    
      {ID} { return ID; } 
      
      "/*"                            { yybegin(IN_BLOCK_COMMENT); yypushback(2); }
      "////" .*                       { return VoltumTokenTypes.EOL_COMMENT; }
      {EOL_DOC_LINE}                  { yybegin(IN_OUTER_EOL_COMMENT); zzPostponedMarkedPos = zzStartRead; }
      "//" .*                         { return VoltumTokenTypes.EOL_COMMENT; }
      
      // .    { return BAD_CHARACTER; }
      
      {WHITE_SPACE} { return WHITE_SPACE; }
}


<IN_BLOCK_COMMENT> {
    "/*"    { if (zzNestedCommentLevel++ == 0) zzPostponedMarkedPos = zzStartRead; }    
    "*/"    { if (--zzNestedCommentLevel == 0) return imbueBlockComment(); }    
    <<EOF>> { zzNestedCommentLevel = 0; return imbueBlockComment(); }    
    [^]     { }
}

<IN_OUTER_EOL_COMMENT>{
  {EOL_WS}{LINE_WS}*"////"   { yybegin(YYINITIAL); yypushback(yylength()); return imbueOuterEolComment(); }
  {EOL_WS}{EOL_DOC_LINE}     {}
  <<EOF>>                    { return imbueOuterEolComment(); }
  [^]                        { yybegin(YYINITIAL); yypushback(1); return imbueOuterEolComment(); }
}

[^] { return BAD_CHARACTER; }