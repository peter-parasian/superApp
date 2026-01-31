using Win = System.Windows;
using Controls = System.Windows.Controls;

namespace MyAPP.Views
{
    public partial class ModeSelectionView : Controls.UserControl
    {
        public ModeSelectionView()
        {
            this.InitializeComponent();
        }

        private void BtnMode1_Click(System.Object sender, Win.RoutedEventArgs e)
        {
            // Navigasi ke Dashboard View yang sudah ada (Mode 1)
            Win.Window? window = Win.Window.GetWindow(this);
            if (window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowDashboard();
            }
        }
    }
}