using Win = System.Windows;

namespace MyAPP
{
    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.ShowLogin();
        }

        public void ShowLogin()
        {
            MyAPP.Views.LoginView loginView = new();
            this.MainContent.Content = loginView;
        }

        public void ShowDashboard()
        {
            MyAPP.Views.DashboardView dashboardView = new();
            this.MainContent.Content = dashboardView;
        }

        public void ShowModeSelection()
        {
            MyAPP.Views.ModeSelectionView modeView = new();
            this.MainContent.Content = modeView;
        }
    }
}