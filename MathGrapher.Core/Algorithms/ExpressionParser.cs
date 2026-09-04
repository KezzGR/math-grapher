using System;
using System.Collections.Generic;
using System.Globalization;

namespace MathGrapher.Core.Algorithms
{
    public static class ExpressionParser
    {
        private static readonly Dictionary<char, int> Precedance = new Dictionary<char, int>
        {
            { '+', 1 },
            { '-', 1 },
            { '*', 2 },
            { '/', 2 },
            { '^', 3 }
        };

        private static readonly Dictionary<string, Func<double, double>> Functions = new Dictionary<string, Func<double, double>>(StringComparer.OrdinalIgnoreCase)
        {
            { "sin", Math.Sin },
            { "cos", Math.Cos },
            { "sqrt", Math.Sqrt },
            { "abs", Math.Abs },
            { "log", Math.Log },
            { "exp", Math.Exp }
        };

        private static readonly Dictionary<string, double> Constants = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            { "pi", Math.PI },
            { "e", Math.E }
        };

        public static double Evaluate(string expression, double x)
        {
            Queue<Token> outputQueue;

            try
            {
                outputQueue = ShuntingYard(expression, x);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Ошибка парсинга выражения: {ex.Message}", ex);
            }

            return EvaluateRPN(outputQueue);
        }

        private static Queue<Token> ShuntingYard(string expression, double x)
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
                    output.Enqueue(Token.Number(x));
                }
                else if (char.IsLetter(c))
                {
                    string name = "";

                    while (i < expression.Length && char.IsLetter(expression[i]))
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
                        throw new Exception($"Неизвестное имя: {name}");
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

                    if (operators.Count == 0) throw new Exception("Несогласованные скобки");

                    operators.Pop();

                    if (operators.Count > 0 && operators.Peek().Type == TokenType.Function)
                    {
                        output.Enqueue(operators.Pop());
                    }
                }
                else if (Precedance.ContainsKey(c))
                {
                    int previousIndex = i - 1;

                    while (previousIndex >= 0 && char.IsWhiteSpace(expression[previousIndex]))
                    {
                        previousIndex--;
                    }

                    if (c == '-' && (previousIndex < 0 || expression[previousIndex] == '(' || Precedance.ContainsKey(expression[previousIndex])))
                    {
                        output.Enqueue(Token.Number(-1.0));
                        operators.Push(Token.Operator('*'));
                    }
                    else
                    {
                        while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator)
                        {
                            char stackOp = (char)operators.Peek().Value; 
                            if (Precedance.TryGetValue(stackOp, out int stackPrec))
                            {
                                int currPrec = Precedance[c];

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
                    throw new Exception($"Недопустимый символ: '{c}'");
                }
            }

            while (operators.Count > 0)
            {
                Token token = operators.Pop();

                if (token.Type == TokenType.LeftParen) throw new Exception("Несогласованные скобки");

                output.Enqueue(token);
            }

            return output;
        }

        private static double EvaluateRPN(Queue<Token> rpnQueue)
        {
            var stack = new Stack<double>();

            while (rpnQueue.Count > 0)
            {
                Token token = rpnQueue.Dequeue();

                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push((double)token.Value);
                        break;

                    case TokenType.Operator:
                        char op = (char)token.Value;
                        double right = stack.Pop();
                        double left = stack.Pop();
                        double result = op switch
                        {
                            '+' => left + right,
                            '-' => left - right,
                            '*' => left * right,
                            '/' => left / right,
                            '^' => Math.Pow(left, right),
                            _ => throw new Exception($"Неизвестный оператор: {op}")
                        };

                        stack.Push(result);
                        break;

                    case TokenType.Function:
                        string funcName = (string)token.Value;
                        double arg = stack.Pop();
                        var func = Functions[funcName];
                        stack.Push(func(arg));
                        break;

                    default:
                        throw new Exception($"Неожиданный токен: {token.Type}");
                }
            }

            if (stack.Count != 1) throw new Exception($"Ошибка вычисления: неверное число операндов");

            return stack.Pop();
        }
    }
}