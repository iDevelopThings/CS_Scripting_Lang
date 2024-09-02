using System.Diagnostics;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

[DebuggerDisplay("{Value} ({FlagsString})")]
public partial struct Token
{
    public TokenType  Type;
    public string     Value;
    public TokenRange Range;

    private string FlagsString => Type.GetFlagString();

    public Token(TokenType type, string value, TokenRange range) {
        Type  = type;
        Value = value;
        Range = range;
    }

    public OperatorType Op { get; set; }

    public string ErrorMessage { get; set; }

    public bool IsOp(OperatorType          type)  => IsOperator && Op == type;
    public bool IsOp(params OperatorType[] types) => IsOperator && types.Contains(Op);
    
    public override string ToString() {
        var baseStr = $"{Value} -> (flags={Type.GetFlagString()})";

        if (Type.HasAny(TokenType.Operator)) {
            baseStr += $" (op={Op})";
        }

        if (ErrorMessage != null) {
            baseStr += $" (error={ErrorMessage})";
        }

        baseStr += $" ({Range})";

        return baseStr;
    }

    public bool IsNumber =>
        ((Type & TokenType.Int32) == TokenType.Int32) ||
        ((Type & TokenType.Int64) == TokenType.Int64) ||
        ((Type & TokenType.Float) == TokenType.Float) ||
        ((Type & TokenType.Double) == TokenType.Double);
}