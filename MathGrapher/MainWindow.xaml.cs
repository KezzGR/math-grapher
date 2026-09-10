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
            ShowError("Введите формулу.");
            return;
        }

        if (!TryParseDouble(XMinTextBox.Text, out double xMin, "XMin")) return;
        if (!TryParseDouble(XMaxTextBox.Text, out double xMax, "XMax")) return;
        if (!TryParseDouble(StepTextBox.Text, out double step, "Шаг")) return;

        if (!double.IsFinite(xMin) || !double.IsFinite(xMax) || !double.IsFinite(step))
        {
            ShowError("Границы и шаг должны быть конечными числами.");
            return;
        }

        if (xMin >= xMax)
        {
            ShowError("XMin должен быть меньше XMax");
            return;
        }
        if (step <= 0)
        {
            ShowError("Шаг должен быть положительным");
            return;
        }

        double estimatedPointCount = Math.Floor((xMax - xMin) / step) + 1;

        if (!double.IsFinite(estimatedPointCount) || estimatedPointCount > MaxPointCount)
        {
            ShowError($"Слишком много точек для построения. Максимум: {MaxPointCount}. Увеличьте шаг.");
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
            ShowError("Нет допустимых точек для построения графика.\nВозможно, функция не определена на всем интервале.");
            return;
        }

        PlotModel model = new() { Title = $"y = {formula}" };

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
                $"Готово. Точек: {validPointCount}. " +
                "Интеграл не вычислен: обнаружен разрыв функции.";
        }
        else
        {
            try
            {
                int n = Math.Max(100, (int)((xMax - xMin) / step));
                integral = Integrator.Trapezoidal(function, xMin, xMax, n);
                StatusTextBlock.Text = $"Готово. Точек: {validPointCount}. Интеграл ≈ {integral:F4}";
            }
            catch (InvalidOperationException ex)
            {
                StatusTextBlock.Text = $"Готово. Точек: {validPointCount}. Интеграл не вычислен: {ex.Message}";
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
                ShowError($"Ошибка сохранения в базу данных: {ex.Message}");
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
            ShowError($"Не удалось загрузить историю: {ex.Message}");
        }
    }

    private bool TryParseDouble(string text, out double value, string fieldName)
    {
        if (!double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            ShowError($"Неккоректное значение в поле '{fieldName}'. Введите число.");
            return false;
        }

        return true;
    }

    private void ShowError(string message)
    {
        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        StatusTextBlock.Text = "Ошибка";
    }
}
