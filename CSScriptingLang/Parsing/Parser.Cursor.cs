using CSScriptingLang.Lexing;

namespace CSScriptingLang.Parsing;

public partial class Parser
{
    private Token Token { get; set; }
    private Token Next  => Cursor.Peek();

    private Token Advance()          => Token = Cursor.Read();
    private Token Advance(int n)     => Token = Cursor.Read(n);
    private Token Rewind(int  n = 1) => Token = Cursor.Rewind(n);

    private bool Sequence(params TokenType[] types) {
        for (var i = 0; i < types.Length; i++) {
            var tok = i == 0 ? Token : Cursor.Peek(i - 1);
            
            if (!tok.Is(types[i])) {
                return false;
            }
        }

        return true;
    }
}