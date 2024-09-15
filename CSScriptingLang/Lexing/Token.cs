using System.Diagnostics;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public partial class Token
{
    public TokenType  Type;
    public string     Value;
    public TokenRange Range;

    public Keyword      Keyword { get; set; }
    public OperatorType Op      { get; set; }

    public string ErrorMessage { get; set; }

    private string FlagsString => Type.GetFlagString();

    public int Idx { get; set; }

    public Token Previous { get; set; }
    public Token Next     { get; set; }

    public Token(TokenType type, string value, TokenRange range) {
        Type  = type;
        Value = value;
        Range = range;
    }


    public bool IsOp(OperatorType          type)  => IsOperator && Op == type;
    public bool IsOp(params OperatorType[] types) => IsOperator && types.Contains(Op);

    public override string ToString() {
        var baseStr = $"Value=`{Value}` -> (flags={Type.GetFlagString()})";

        if (Type.HasAny(TokenType.Operator)) {
            baseStr += $" (op={Op})";
        }

        if (Keyword != Keyword.None) {
            baseStr += $" (keyword={Keyword.GetFlagString()})";
        }

        if (ErrorMessage != null) {
            baseStr += $" (error={ErrorMessage})";
        }

        baseStr += $" ({Range})";

        return baseStr;
    }

    public string GetDebuggerDisplay() {
        return ToString();
    }

    public bool IsNumber =>
        ((Type & TokenType.Int32) == TokenType.Int32) ||
        ((Type & TokenType.Int64) == TokenType.Int64) ||
        ((Type & TokenType.Float) == TokenType.Float) ||
        ((Type & TokenType.Double) == TokenType.Double);

    public bool IsFunctionDeclarationLike => ((Keyword & Keyword.Function) == Keyword.Function) ||
                                             ((Keyword & Keyword.Coroutine) == Keyword.Coroutine) ||
                                             ((Keyword & Keyword.Async) == Keyword.Async);


    public bool Is(Keyword type, bool exact = false) {
        return exact ? Keyword.HasAll(type) : Keyword.HasAny(type);
    }

}