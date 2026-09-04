using MathGrapher.Core.Algorithms;

namespace MathGrapher.Core.Tests;

public class ExpressionParserTests
{
    [Theory]
    [InlineData("2 + 3 * 4", 0.0, 14.0)]
    [InlineData("(2 + 3) * 4", 0.0, 20.0)]
    [InlineData("2 ^ 3 ^ 2", 0.0, 512.0)]
    [InlineData("sin(0)", 0.0, 0.0)]
    [InlineData("x ^ 2", 3.0, 9.0)]
    [InlineData("pi", 0.0, Math.PI)]
    public void Evaluate_ValidExpression_ReturnsExpectedResult(string expression, double x, double expected)
    {
        double result = ExpressionParser.Evaluate(expression, x);

        Assert.Equal(expected, result, precision: 10);
    }
}
