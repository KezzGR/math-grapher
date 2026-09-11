using MathGrapher.Core.Algorithms;

namespace MathGrapher.Core.Tests;

public class IntegratorTests
{
    [Fact]
    public void Trapezoidal_LinearFunction_ReturnsExpectedResult()
    {
        double result = Integrator.Trapezoidal(x => x, 0.0, 1.0, 100);

        Assert.Equal(0.5, result, precision: 10);
    }

    [Fact]
    public void Trapezoidal_QuadraticFunction_ReturnsExpectedResult()
    {
        double result = Integrator.Trapezoidal(x => x * x, 0.0, 1.0, 100);

        double expected = 1.0 / 3.0;

        Assert.InRange(result, expected - 0.0001, expected + 0.0001);
    }

    [Fact]
    public void Trapezoidal_ConstantFunction_ReturnsExpectedResult()
    {
        double result = Integrator.Trapezoidal(_ => 5, -2.0, 3.0, 100);

        Assert.Equal(25, result, precision: 10);
    }

    [Fact]
    public void Trapezoidal_FunctionIsNotFinite_ThrowsInvalidOperationException()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => Integrator.Trapezoidal(x => Math.Sqrt(x), -10.0, 10.0, 100));

        Assert.Contains("not finite", exception.Message);
    }

    [Fact]
    public void Trapezoidal_FunctionIsNotFiniteInsideInterval_ThrowsInvalidOperationException()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => Integrator.Trapezoidal(x => 1 / x, -1.0, 1.0, 100));

        Assert.Contains("not finite", exception.Message);
    }

    [Fact]
    public void Trapezoidal_NegativeFunction_ReturnsNegativeIntegral()
    {
        double result = Integrator.Trapezoidal(_ => -1.0, 0.0, 2.0, 100);

        Assert.Equal(-2.0, result, precision: 10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Trapezoidal_NonPositiveSubdivisionCount_ThrowsArgumentOutOfRangeException(int n)
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Integrator.Trapezoidal(x => x, 0.0, 1.0, n));

        Assert.Equal("n", exception.ParamName);
    }
}
