using System.Globalization;

namespace MathGrapher.Core.Algorithms;

public static class ExpressionParser
{
    private static readonly Dictionary<char, int> Precedence = new()
    {
        { '+', 1 },
        { '-', 1 },
        { '*', 2 },
        { '/', 2 },
        { '^', 3 }
    };

    private static readonly Dictionary<string, Func<double, double>> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "sin", Math.Sin },
        { "cos", Math.Cos },
        { "tan", Math.Tan },
        { "asin", Math.Asin },
        { "acos", Math.Acos },
        { "atan", Math.Atan },
        { "sinh", Math.Sinh },
        { "cosh", Math.Cosh },
        { "tanh", Math.Tanh },
        { "sqrt", Math.Sqrt },
        { "cbrt", Math.Cbrt },
        { "abs", Math.Abs },
        { "ln", Math.Log },
        { "log", Math.Log },
        { "log10", Math.Log10 },
        { "exp", Math.Exp }
    };

    private static readonly Dictionary<string, double> Constants = new(StringComparer.OrdinalIgnoreCase)
    {
        { "pi", Math.PI },
        { "e", Math.E },
        { "tau", Math.Tau }
    };

    public static double Evaluate(string expression, double x)
    {
        Func<double, double> function = Compile(expression);

        return function(x);
    }

    public static Func<double, double> Compile(string expression)
    {
        Token[] tokens;

        try
        {
            tokens = [.. ShuntingYard(expression)];
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid expression: {ex.Message}", nameof(expression), ex);
        }

        return x =>
        {
            try
            {
                return EvaluateRPN(tokens, x);
            }
            catch (InvalidOperationException ex)
            {
                throw new ArgumentException("Invalid expression: not enough operands.", nameof(expression), ex);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid expression: {ex.Message}", nameof(expression), ex);
            }
        };
    }

    private static Queue<Token> ShuntingYard(string expression)
    {
        var output = new Queue<Token>();
        var operators = new Stack<Token>();

        for (int i = 0; i < expression.Length; i++)
        {
            char c = expression[i];

            if (char.IsWhiteSpace(c)) continue;

            if (char.IsDigit(c) || c == '.')
            {
                string numStr = "";

                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                {
                    numStr += expression[i];
                    i++;
                }
                i--;

                double num = double.Parse(numStr, CultureInfo.InvariantCulture);
                output.Enqueue(Token.Number(num));
            }
            else if (c == 'x')
            {
                output.Enqueue(Token.Variable());
            }
            else if (char.IsLetter(c))
            {
                string name = "";

                while (i < expression.Length && char.IsLetterOrDigit(expression[i]))
                {
                    name += expression[i];
                    i++;
                }
                i--;

                if (Constants.TryGetValue(name, out double constVal))
                {
                    output.Enqueue(Token.Number(constVal));
                }
                else if (Functions.ContainsKey(name))
                {
                    operators.Push(Token.Function(name));
                }
                else
                {
                    throw new Exception($"Unknown identifier: {name}");
                }
            }
            else if (c == '(')
            {
                operators.Push(Token.LeftParen());
            }
            else if (c == ')')
            {
                while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen)
                {
                    output.Enqueue(operators.Pop());
                }

                if (operators.Count == 0) throw new Exception("Mismatched parentheses");

                operators.Pop();

                if (operators.Count > 0 && operators.Peek().Type == TokenType.Function)
                {
                    output.Enqueue(operators.Pop());
                }
            }
            else if (Precedence.ContainsKey(c))
            {
                int previousIndex = i - 1;

                while (previousIndex >= 0 && char.IsWhiteSpace(expression[previousIndex]))
                {
                    previousIndex--;
                }

                if (c == '-' && (previousIndex < 0 || expression[previousIndex] == '(' || Precedence.ContainsKey(expression[previousIndex])))
                {
                    output.Enqueue(Token.Number(-1.0));
                    operators.Push(Token.Operator('*'));
                }
                else
                {
                    while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator)
                    {
                        char stackOp = (char)operators.Peek().Value!;
                        if (Precedence.TryGetValue(stackOp, out int stackPrec))
                        {
                            int currPrec = Precedence[c];

                            if ((c != '^' && stackPrec >= currPrec) || (c == '^' && stackPrec > currPrec))
                            {
                                output.Enqueue(operators.Pop());
                            }
                            else break;
                        }
                        else break;

                    }
                    operators.Push(Token.Operator(c));
                }
            }
            else
            {
                throw new Exception($"Invalid character: '{c}'");
            }
        }

        while (operators.Count > 0)
        {
            Token token = operators.Pop();

            if (token.Type == TokenType.LeftParen) throw new Exception("Mismatched parentheses");

            output.Enqueue(token);
        }

        return output;
    }

    private static double EvaluateRPN(IEnumerable<Token> rpnTokens, double x)
    {
        var stack = new Stack<double>();

        foreach (Token token in rpnTokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    stack.Push((double)token.Value!);
                    break;

                case TokenType.Variable:
                    stack.Push(x);
                    break;

                case TokenType.Operator:
                    char op = (char)token.Value!;
                    double right = stack.Pop();
                    double left = stack.Pop();
                    double result = op switch
                    {
                        '+' => left + right,
                        '-' => left - right,
                        '*' => left * right,
                        '/' => left / right,
                        '^' => Math.Pow(left, right),
                        _ => throw new Exception($"Unknown operator: {op}")
                    };

                    stack.Push(result);
                    break;

                case TokenType.Function:
                    string funcName = (string)token.Value!;
                    double arg = stack.Pop();
                    var func = Functions[funcName];
                    stack.Push(func(arg));
                    break;

                default:
                    throw new Exception($"Unexpected token: {token.Type}");
            }
        }

        if (stack.Count != 1) throw new Exception("Evaluation error: invalid number of operands");

        return stack.Pop();
    }
}
