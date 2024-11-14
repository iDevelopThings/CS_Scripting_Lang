using System.Reflection;
using CSScriptingLang.Interpreter.Execution.Expressions;
using CSScriptingLang.Interpreter.Execution.Statements;
using CSScriptingLang.Lexing;
using CSScriptingLang.Parsing.AST;
using CSScriptingLang.Utils.ReflectionUtils;

namespace CSScriptingLang.Parsing;

public enum PrecedenceValue
{
    Invalid = 0,
    Assign  = 1,
    Ternary,
    ConditionalOr,
    ConditionalAnd,
    Equality,
    Relational,
    BitOr,
    BitXor,
    BitAnd,
    BitShift,
    Addition,
    Multiplication,
    Prefix,
    Postfix
}

public interface IPrefixParser<out T> where T : Expression
{
    T Parse();
}

public interface IInfixParser<out T> where T : Expression
{
    int Precedence { get; }

    T Parse(Expression left);
}

public interface IStatementParser<out T> where T : Statement
{
    T Parse();
}

public delegate bool ParserMatcherDelegate();

public interface IParserMatcher
{
    bool Matches();
}

// T = IPrefixParser, IInfixParser, IStatementParser
public class ParserGroupCollection<T> where T : class
{
    private readonly SubParser _parser;
    public ParserGroupCollection(SubParser parser) {
        _parser = parser;
    }

    public Dictionary<TokenType, T>                     ParsersByTokenType { get; } = new();
    public Dictionary<OperatorType, T>                  ParsersByOperator  { get; } = new();
    public List<KeyValuePair<ParserMatcherDelegate, T>> Parsers            { get; } = new();

    public void AddParser(TokenType tokenType, T parser) {
        ParsersByTokenType[tokenType] = parser;
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }
    }
    public void AddParser(OperatorType operatorType, T parser) {
        ParsersByOperator[operatorType] = parser;
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }
    }
    public void AddParser<TParserType>(TokenType tokenType, params object[] args) where TParserType : T, new() {
        var parser = (T) Activator.CreateInstance(typeof(TParserType), args);

        ParsersByTokenType[tokenType] = parser;
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }
    }
    public void AddParser<TParserType>(OperatorType operatorType, params object[] args) where TParserType : T, new() {
        var parser = (T) Activator.CreateInstance(typeof(TParserType), args);

        ParsersByOperator[operatorType] = parser;
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }
    }

    public void AddParser<TParserType>(params object[] parserArgs) where TParserType : T, new() {
        if (!typeof(TParserType).IsAssignableTo(typeof(IParserMatcher))) {
            throw new Exception("Parser type must implement IParserMatcher");
        }

        var parser = (T) Activator.CreateInstance(typeof(TParserType), parserArgs);
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }

        if (parser is IParserMatcher matcher) {
            Parsers.Add(new KeyValuePair<ParserMatcherDelegate, T>(matcher.Matches, parser));
        }
    }
    public void AddParser<TParserType>(ParserMatcherDelegate matcher, params object[] parserArgs) where TParserType : T, new() {
        var parser = (T) Activator.CreateInstance(typeof(TParserType), parserArgs);
        if (parser is SubParser subParser) {
            subParser.SetParent(_parser);
        }
        Parsers.Add(new KeyValuePair<ParserMatcherDelegate, T>(matcher, parser));
    }

    public bool TryGetParser(SubParser mainParser, out T parser) {
        if (ParsersByTokenType.TryGetValue(mainParser.Token.Type, out parser)) {
            return true;
        }

        foreach (var (matcher, p) in Parsers) {
            if (!matcher())
                continue;

            parser = p;
            return true;
        }

        if (ParsersByOperator.TryGetValue(mainParser.Token.Op, out parser)) {
            return true;
        }


        return false;
    }

}

public abstract class SubParserType : SubParser
{
    protected SubParserType() { }
    protected SubParserType(Parser parent) {
        SetParent(parent);
    }
}

public abstract class SubParserType<T, TOutputType> : SubParserType
    where T : SubParserType<T, TOutputType>, new()
    where TOutputType : BaseNode
{
    public static T Instance { get; set; }

    protected SubParserType() { }

    public SubParserType(Parser parent) : base(parent) { }

    public static T Create(Parser parent, bool reset = false) {
        if (reset)
            Instance = null;

        if (Instance != null)
            return Instance;
        Instance = new T();
        Instance.SetParent(parent);

        return Instance;
    }

    public virtual TOutputType Parse()     => throw new NotImplementedException();
    public static  TOutputType ParseNode() => Instance?.Parse();

    public virtual TOutputType Parse(Expression     left) => throw new NotImplementedException();
    public static  TOutputType ParseNode(Expression left) => Instance?.Parse(left);
}

public abstract class DependantSubParserType<T, TOutputType, TDependant> : SubParserType<T, TOutputType>
    where T : DependantSubParserType<T, TOutputType, TDependant>, new()
    where TOutputType : BaseNode
    where TDependant : BaseNode
{
    protected DependantSubParserType() { }
    public DependantSubParserType(Parser parent) : base(parent) { }

    public virtual TOutputType Parse(TDependant     dependant) => throw new NotImplementedException();
    public static  TOutputType ParseNode(TDependant dependant) => Instance?.Parse(dependant);
}

public class ParserCollection
{
    public SubParser Parser { get; }

    public ParserGroupCollection<IPrefixParser<Expression>>   PrefixParsers;
    public ParserGroupCollection<IInfixParser<Expression>>    InfixParsers;
    public ParserGroupCollection<IStatementParser<Statement>> StatementParsers;

    public ParserCollection(SubParser parser) {
        Parser = parser;

        PrefixParsers    = new ParserGroupCollection<IPrefixParser<Expression>>(parser);
        InfixParsers     = new ParserGroupCollection<IInfixParser<Expression>>(parser);
        StatementParsers = new ParserGroupCollection<IStatementParser<Statement>>(parser);

    }

    public static Dictionary<Type, SubParser> AllSubParsers { get; } = new();

    public void LoadAllParsers(Parser parser, bool reset = false) {
        var allTypes = ReflectionStore.AllTypesOf(typeof(SubParserType))
           .Where(t => !t.IsAbstract && !t.IsGenericType)
           .ToList();

        allTypes.ForEach(subParser => {
            var createMethod = subParser.GetMethod("Create", BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Static);
            var inst         = (SubParser) createMethod!.Invoke(null, [parser, reset]);

            AllSubParsers[inst!.GetType()] = inst;
        });
    }

    public int GetPrecedence() {
        return TryGetInfixParser(Parser, out var infixParser)
            ? infixParser.Precedence
            : 0;
    }

    public void AddPrefixParser(TokenType tokenType, IPrefixParser<Expression> parser)
        => PrefixParsers.AddParser(tokenType, parser);
    public void AddPrefixParser(OperatorType operatorType, IPrefixParser<Expression> parser)
        => PrefixParsers.AddParser(operatorType, parser);

    public void AddPrefixParser<TParserType>(TokenType tokenType, params object[] args)
        where TParserType : IPrefixParser<Expression>, new()
        => PrefixParsers.AddParser<TParserType>(tokenType, args);
    public void AddPrefixParser<TParserType>(OperatorType operatorType, params object[] args)
        where TParserType : IPrefixParser<Expression>, new()
        => PrefixParsers.AddParser<TParserType>(operatorType, args);

    public void AddPrefixParser<TParserType>(params object[] parserArgs)
        where TParserType : IPrefixParser<Expression>, new()
        => PrefixParsers.AddParser<TParserType>(parserArgs);

    public void AddPrefixParser<TParserType>(ParserMatcherDelegate matcher, params object[] parserArgs)
        where TParserType : IPrefixParser<Expression>, new()
        => PrefixParsers.AddParser<TParserType>(matcher, parserArgs);

    public bool TryGetPrefixParser(SubParser mainParser, out IPrefixParser<Expression> parser) => PrefixParsers.TryGetParser(mainParser, out parser);


    public void AddInfixParser(TokenType tokenType, IInfixParser<Expression> parser)
        => InfixParsers.AddParser(tokenType, parser);
    public void AddInfixParser(OperatorType operatorType, IInfixParser<Expression> parser)
        => InfixParsers.AddParser(operatorType, parser);

    public void AddInfixParser<TParserType>(TokenType tokenType, params object[] args)
        where TParserType : IInfixParser<Expression>, new()
        => InfixParsers.AddParser<TParserType>(tokenType, args);
    public void AddInfixParser<TParserType>(OperatorType operatorType, params object[] args)
        where TParserType : IInfixParser<Expression>, new()
        => InfixParsers.AddParser<TParserType>(operatorType, args);

    public void AddInfixParser<TParserType>(params object[] parserArgs)
        where TParserType : IInfixParser<Expression>, new()
        => InfixParsers.AddParser<TParserType>(parserArgs);

    public void AddInfixParser<TParserType>(ParserMatcherDelegate matcher, params object[] parserArgs)
        where TParserType : IInfixParser<Expression>, new()
        => InfixParsers.AddParser<TParserType>(matcher, parserArgs);

    public bool TryGetInfixParser(SubParser mainParser, out IInfixParser<Expression> parser)
        => InfixParsers.TryGetParser(mainParser, out parser);


    public void AddStatementParser(TokenType tokenType, IStatementParser<Statement> parser)
        => StatementParsers.AddParser(tokenType, parser);
    public void AddStatementParser(OperatorType operatorType, IStatementParser<Statement> parser)
        => StatementParsers.AddParser(operatorType, parser);

    public void AddStatementParser<TParserType>(TokenType tokenType, params object[] args)
        where TParserType : IStatementParser<Statement>, new()
        => StatementParsers.AddParser<TParserType>(tokenType, args);
    public void AddStatementParser<TParserType>(OperatorType operatorType, params object[] args)
        where TParserType : IStatementParser<Statement>, new()
        => StatementParsers.AddParser<TParserType>(operatorType, args);

    public void AddStatementParser<TParserType>(params object[] parserArgs)
        where TParserType : IStatementParser<Statement>, new()
        => StatementParsers.AddParser<TParserType>(parserArgs);

    public void AddStatementParser<TParserType>(ParserMatcherDelegate matcher, params object[] parserArgs)
        where TParserType : IStatementParser<Statement>, new()
        => StatementParsers.AddParser<TParserType>(matcher, parserArgs);
    public bool TryGetStatementParser(SubParser mainParser, out IStatementParser<Statement> parser) => StatementParsers.TryGetParser(mainParser, out parser);

}