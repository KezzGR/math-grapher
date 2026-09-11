namespace MathGrapher.Core.Algorithms;

public static class Integrator
{
    public static double Trapezoidal(Func<double, double> f, double a, double b, int n)
    {
        if (n <= 0)
            throw new ArgumentOutOfRangeException(nameof(n), n, "Subdivision count must be positive.");

        double h = (b - a) / n;

        double startValue = f(a);
        double endValue = f(b);

        if (!double.IsFinite(startValue) || !double.IsFinite(endValue))
            throw new InvalidOperationException("The function is not finite over the integration interval.");

        double sum = 0.5 * (startValue + endValue);

        for (int i = 1; i < n; i++)
        {
            double value = f(a + i * h);

            if (!double.IsFinite(value))
                throw new InvalidOperationException("The function is not finite over the integration interval.");

            sum += value;
        }

        return sum * h;
    }
}
