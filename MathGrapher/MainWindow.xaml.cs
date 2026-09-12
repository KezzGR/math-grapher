using System.Windows;
using System.Globalization;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;
using MathGrapher.Core.Algorithms;
using MathGrapher.Core.Data;
using MathGrapher.Core.Models;

namespace MathGrapher;

public partial class MainWindow : Window
{
    private const int MaxPointCount = 50_000;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (s, e) => LoadHistory();
    }

    private void PlotButton_Click(object sender, RoutedEventArgs e)
    {
        PlotGraph(saveToHistory: true);
    }

    private void HistoryDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (HistoryDataGrid.SelectedItem is GraphRecord record)
        {
            FormulaTextBox.Text = record.Expression;
            XMinTextBox.Text = record.XMin.ToString(CultureInfo.InvariantCulture);
            XMaxTextBox.Text = record.XMax.ToString(CultureInfo.InvariantCulture);
            StepTextBox.Text = record.Step.ToString(CultureInfo.InvariantCulture);

            PlotGraph(saveToHistory: false);
        }
    }

    private static bool HasLikelyDiscontinuity(
        Func<double, double> function,
        double leftX,
        double leftY,
        double rightX,
        double rightY)
    {
        bool changesSign = (leftY < 0 && rightY > 0) || (leftY > 0 && rightY < 0);

        if (!changesSign)
            return false;

        double middleX = leftX + (rightX - leftX) / 2;
        double middleY = function(middleX);

        if (!double.IsFinite(middleY))
            return true;

        double minY = Math.Min(leftY, rightY);
        double maxY = Math.Max(leftY, rightY);

        return middleY < minY || middleY > maxY;
    }

    private void PlotGraph(bool saveToHistory)
    {
        string formula = FormulaTextBox.Text.Trim();

        if (string.IsNullOrEmpty(formula))
        {
            ShowError("Enter an expression.");
            return;
        }

        if (!TryParseDouble(XMinTextBox.Text, out double xMin, "X min")) return;
        if (!TryParseDouble(XMaxTextBox.Text, out double xMax, "X max")) return;
        if (!TryParseDouble(StepTextBox.Text, out double step, "Step")) return;

        if (!double.IsFinite(xMin) || !double.IsFinite(xMax) || !double.IsFinite(step))
        {
            ShowError("Bounds and step must be finite numbers.");
            return;
        }

        if (xMin >= xMax)
        {
            ShowError("X minimum must be less than X maximum.");
            return;
        }
        if (step <= 0)
        {
            ShowError("Step must be positive.");
            return;
        }

        double estimatedPointCount = Math.Floor((xMax - xMin) / step) + 1;

        if (!double.IsFinite(estimatedPointCount) || estimatedPointCount > MaxPointCount)
        {
            ShowError($"Too many points to plot. Maximum: {MaxPointCount}. Increase the step.");
            return;
        }

        int pointCount = (int)estimatedPointCount;

        List<DataPoint> points = new(pointCount);
        Func<double, double> function;
        int validPointCount = 0;
        bool hasPreviousPoint = false;
        bool hasDiscontinuity = false;
        double previousX = 0;
        double previousY = 0;

        try
        {
            function = ExpressionParser.Compile(formula);

            for (int i = 0; i < pointCount; i++)
            {
                double x = xMin + step * i;

                if (x > xMax) break;

                double y = function(x);

                if (double.IsFinite(y))
                {
                    if (hasPreviousPoint && HasLikelyDiscontinuity(function, previousX, previousY, x, y))
                    {
                        points.Add(DataPoint.Undefined);
                        hasDiscontinuity = true;
                    }

                    points.Add(new DataPoint(x, y));
                    validPointCount++;

                    previousX = x;
                    previousY = y;
                    hasPreviousPoint = true;
                }
                else
                {
                    points.Add(DataPoint.Undefined);
                    hasPreviousPoint = false;
                    hasDiscontinuity = true;
                }
            }
        }
        catch (ArgumentException ex)
        {
            ShowError(ex.Message);
            return;
        }

        if (validPointCount == 0)
        {
            ShowError("No valid points to plot.\nThe function may be undefined over the selected interval.");
            return;
        }

        PlotModel model = new()
        {
            Title = $"y = {formula}",
            Culture = CultureInfo.InvariantCulture
        };

        LineSeries lineSeries = new()
        {
            Title = formula,
            Color = OxyColors.DodgerBlue,
            StrokeThickness = 2,
            MarkerType = MarkerType.None
        };
        lineSeries.Points.AddRange(points);
        model.Series.Add(lineSeries);

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "X",
            Minimum = xMin,
            Maximum = xMax,
            PositionAtZeroCrossing = true,
            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Black,
            AxislineThickness = 1,
            TitlePosition = 1.0,
            AxisTitleDistance = 10
        });

        model.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Y",
            PositionAtZeroCrossing = true,
            AxislineStyle = LineStyle.Solid,
            AxislineColor = OxyColors.Black,
            AxislineThickness = 1,
            TitlePosition = 1.0,
            AxisTitleDistance = 10
        });

        model.PlotAreaBorderThickness = new OxyThickness(0);
        PlotView.Model = model;

        double? integral = null;
        if (hasDiscontinuity)
        {
            StatusTextBlock.Text =
                $"Ready. Points: {validPointCount}. " +
                "Integral unavailable: discontinuity detected.";
        }
        else
        {
            try
            {
                int n = Math.Max(100, (int)((xMax - xMin) / step));
                integral = Integrator.Trapezoidal(function, xMin, xMax, n);
                StatusTextBlock.Text = $"Ready. Points: {validPointCount}. Integral ≈ {integral.Value.ToString("F4", CultureInfo.InvariantCulture)}";
            }
            catch (InvalidOperationException ex)
            {
                StatusTextBlock.Text = $"Ready. Points: {validPointCount}. Integral unavailable: {ex.Message}";
            }
        }

        if (saveToHistory)
        {
            try
            {
                HistoryRepository.AddRecord(formula, xMin, xMax, step, integral);
                LoadHistory();
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save graph history: {ex.Message}");
            }
        }
    }

    private void LoadHistory()
    {
        try
        {
            var history = HistoryRepository.GetHistory();
            HistoryDataGrid.ItemsSource = history;
        }
        catch (Exception ex)
        {
            ShowError($"Failed to load graph history: {ex.Message}");
        }
    }

    private bool TryParseDouble(string text, out double value, string fieldName)
    {
        if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            ShowError($"Invalid value in the '{fieldName}' field. Enter a number.");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        StatusTextBlock.Text = "Error";
    }
}
