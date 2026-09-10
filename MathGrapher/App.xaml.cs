using MathGrapher.Core.Data;
using System.IO;
using System.Windows;

namespace MathGrapher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                string appDataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MathGrapher");

                Directory.CreateDirectory(appDataDirectory);

                DatabaseHelper.Initialize(
                    Path.Combine(appDataDirectory, "history.db"));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось инициализировать историю графиков.\n\n{ex.Message}",
                    "Ошибка запуска", MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(-1);
                return;
            }

            base.OnStartup(e);
        }
    }
}
