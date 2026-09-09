using MathGrapher.Core.Algorithms;

namespace MathGrapher.Core.Tests;

public class ExpressionParserTests
{
    [Theory]
    [InlineData("2 + 3 * 4", 0.0, 14.0)]
    [InlineData("(2 + 3) * 4", 0.0, 20.0)]
    [InlineData("2 ^ 3 ^ 2", 0.0, 512.0)]
    [InlineData("5 - 8", 0.0, -3.0)]
    [InlineData("2 * -3", 0.0, -6.0)]
    [InlineData("10 / 4", 0.0, 2.5)]
    [InlineData("x ^ 2", 3.0, 9.0)]
    [InlineData("sin(0)", 0.0, 0.0)]
    [InlineData("cos(0)", 0.0, 1.0)]
    [InlineData("tan(pi / 4)", 0.0, 1.0)]
    [InlineData("asin(1)", 0.0, Math.PI / 2)]
    [InlineData("acos(0)", 0.0, Math.PI / 2)]
    [InlineData("atan(1)", 0.0, Math.PI / 4)]
    [InlineData("sinh(0)", 0.0, 0.0)]
    [InlineData("cosh(0)", 0.0, 1.0)]
    [InlineData("tanh(0)", 0.0, 0.0)]
    [InlineData("sqrt(9)", 0.0, 3.0)]
    [InlineData("cbrt(-8)", 0.0, -2.0)]
    [InlineData("abs(-4)", 0.0, 4.0)]
    [InlineData("ln(e)", 0.0, 1.0)]
    [InlineData("log(e)", 0.0, 1.0)]
    [InlineData("log10(100)", 0.0, 2.0)]
    [InlineData("exp(0)", 0.0, 1.0)]
    [InlineData("pi", 0.0, Math.PI)]
    [InlineData("e", 0.0, Math.E)]
    [InlineData("tau", 0.0, Math.Tau)]
    public void Evaluate_ValidExpression_ReturnsExpectedResult(string expression, double x, double expected)
    {
        double result = ExpressionParser.Evaluate(expression, x);

        Assert.Equal(expected, result, precision: 10);
    }

    [Theory]
    [InlineData("unknown(1)")]
    [InlineData("(2 + 3")]
    [InlineData("2 @ 3")]
    [InlineData("sin()")]
    [InlineData("2 3")]
    [InlineData("2 +")]
    public void Evaluate_InvalidExpression_ThrowsArgumentException(string expression)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => ExpressionParser.Evaluate(expression, 0.0));

        Assert.Contains("Некорректное выражение", exception.Message);
    }

    [Fact]
    public void Compile_ValidExpression_EvaluatesForMultipleXValues()
    {
        Func<double, double> function = ExpressionParser.Compile("x ^ 2 + 1");

        Assert.Equal(1.0, function(0.0), precision: 10);
        Assert.Equal(5.0, function(2.0), precision: 10);
        Assert.Equal(10.0, function(-3.0), precision: 10);
    }
}
