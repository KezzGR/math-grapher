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
                MessageBox.Show($"Failed to initialize graph history.\n\n{ex.Message}",
                    "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);

                Shutdown(-1);
                return;
            }

            base.OnStartup(e);
        }
    }
}
