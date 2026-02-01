using Win = System.Windows;

namespace MyAPP
{
    public partial class App : Win.Application
    {
        protected override void OnStartup(Win.StartupEventArgs e)
        {
            base.OnStartup(e);

            MyAPP.MainWindow mainWindow = new MyAPP.MainWindow();

            if (MyAPP.Services.AuthState.TryLoadFromDisk())
            {
                mainWindow.ShowModeSelection();
            }
            else
            {
                mainWindow.ShowLogin();
            }

            mainWindow.Show();
        }
    }
}