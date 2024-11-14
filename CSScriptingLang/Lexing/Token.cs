using System.Diagnostics;
using CSScriptingLang.Interpreter.Modules;
using CSScriptingLang.IncrementalParsing;
using CSScriptingLang.Utils;

namespace CSScriptingLang.Lexing;

[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public partial class Token
{
    public TokenType   Type;
    public string      Value;
    public TokenRange  Range;
    public SourceRange SourceRange => Range;

    public Keyword      Keyword { get; set; }
    public OperatorType Op      { get; set; }

    public string ErrorMessage { get; set; }

    private string FlagsString => Type.GetFlagString();

    public int    Idx         { get; set; }
    public int    ScriptId    { get; set; }
    public Script GetScript() => ModuleResolver.GetScriptById(ScriptId);

    public Token Previous { get; set; }
    public Token Next     { get; set; }

    public Token(TokenType type, string value, TokenRange range) {
        Type  = type;
        Value = value;
        Range = range;
    }


    public bool IsOp(OperatorType          type)  => IsOperator && Op == type;
    public bool IsOp(params OperatorType[] types) => IsOperator && types.Contains(Op);

    /// <summary>
    /// Assignment, PlusEquals, MinusEquals, MultiplyAssign, DivideAssign, ModulusAssign
    /// </summary>
    public bool IsAssignmentOp() => IsOperator && Op.IsAssignment();
    /// <summary>
    /// Not, Minus, BitNot, Plus
    /// </summary>
    public bool IsUnaryOp() => IsOperator && Op.IsUnary();
    /// <summary>
    /// Increment, Decrement
    /// </summary>
    public bool IsUnaryAssignOp() => IsOperator && Op.IsUnaryAssign();
    /// <summary>
    /// GreaterThan, LessThan, GreaterThanOrEqual, LessThanOrEqual
    /// </summary>
    public bool IsComparisonOp() => IsOperator && Op.IsComparison();
    /// <summary>
    /// Plus, Minus, PlusEquals, MinusEquals
    /// </summary>
    public bool IsTermOp() => IsOperator && Op.IsTerm();
    /// <summary>
    /// Equals, EqualsStrict, NotEquals, NotEqualsStrict
    /// </summary>
    public bool IsEqualityOp() => IsOperator && Op.IsEquality();
    /// <summary>
    /// Multiply, Divide, Modulus
    /// </summary>
    public bool IsFactorOp() => IsOperator && Op.IsFactor();
    
    public bool IsOrOp()           => IsOperator && Op.IsOr();
    public bool IsAndOp()          => IsOperator && Op.IsAnd();
    public bool IsBitwiseShiftOp() => IsOperator && Op.IsBitwiseShift();
    
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

    public bool IsTriviaToken =>
        ((Type & TokenType.Whitespace) == TokenType.Whitespace) ||
        ((Type & TokenType.NewLine) == TokenType.NewLine) ||
        ((Type & TokenType.LineComment) == TokenType.LineComment) ||
        ((Type & TokenType.BlockComment) == TokenType.BlockComment);


    public bool IsNumber =>
        ((Type & TokenType.Int32) == TokenType.Int32) ||
        ((Type & TokenType.Int64) == TokenType.Int64) ||
        ((Type & TokenType.Float) == TokenType.Float) ||
        ((Type & TokenType.Double) == TokenType.Double);

    public bool IsFunctionDeclarationLike => ((Keyword & Keyword.Function) == Keyword.Function) ||
                                             ((Keyword & Keyword.Coroutine) == Keyword.Coroutine) ||
                                             ((Keyword & Keyword.Async) == Keyword.Async);


    public bool IsIdent(string ident) {
        return Type == TokenType.Identifier && Value == ident;
    }

    public bool Is(Keyword type, bool exact = false) {
        return exact ? Keyword.HasAll(type) : Keyword.HasAny(type);
    }

    public bool IsTupleLike() =>
        IsSequence(TokenType.LParen, TokenType.Identifier, TokenType.Comma);

    public bool IsSequence(params TokenType[] types) {
        var tok = this;
        var i   = 0;
        while (i < types.Length) {
            if (tok.IsTriviaToken) {
                tok = tok.Next;
                continue;
            }

            if (tok == null || !tok.Is(types[i])) {
                return false;
            }

            tok = tok.Next;
            i++;
        }

        return true;
    }

}