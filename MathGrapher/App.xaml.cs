using MathGrapher.Core.Data;
using System.IO;
using System.Windows;

namespace MathGrapher
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string appDataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MathGrapher");
            Directory.CreateDirectory(appDataDirectory);

            DatabaseHelper.Initialize(
                Path.Combine(appDataDirectory, "history.db"));
        }
    }
}
