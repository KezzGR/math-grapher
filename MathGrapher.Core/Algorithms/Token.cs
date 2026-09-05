namespace MathGrapher.Core.Algorithms;

public enum TokenType
{
    Number,
    Variable,
    Operator,
    Function,
    LeftParen,
    RightParen
}

public class Token
{
    public TokenType Type { get; }
    public object? Value { get; }

    private Token(TokenType type, object? value = null)
    {
        Type = type;
        Value = value;
    }

    public static Token Number(double d) => new(TokenType.Number, d);
    public static Token Variable() => new(TokenType.Variable);
    public static Token Operator(char op) => new(TokenType.Operator, op);
    public static Token Function(string name) => new(TokenType.Function, name);
    public static Token LeftParen() => new(TokenType.LeftParen);
    public static Token RightParen() => new(TokenType.RightParen);
}
