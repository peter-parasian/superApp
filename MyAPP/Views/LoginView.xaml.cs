using Sys = System;
using Win = System.Windows;
using Input = System.Windows.Input;
using Sec = System.Security;
using Tasks = System.Threading.Tasks;
using Icons = MahApps.Metro.IconPacks;

namespace MyAPP.Views
{
    public partial class LoginView : Win.Controls.UserControl
    {
        private Sys.Boolean _isPasswordVisible;
        private readonly MyAPP.ViewModels.LoginViewModel _viewModel;

        public LoginView()
        {
            this.InitializeComponent();
            this._isPasswordVisible = false;
            this._viewModel = new MyAPP.ViewModels.LoginViewModel();
        }

        private void TxtEmail_KeyDown(Sys.Object sender, Input.KeyEventArgs e)
        {
            if (e.Key == Input.Key.Enter)
            {
                if (this._isPasswordVisible)
                {
                    this.TxtPasswordVisible.Focus();
                }
                else
                {
                    this.TxtPassword.Focus();
                }
            }
        }

        private void TxtPassword_KeyDown(Sys.Object sender, Input.KeyEventArgs e)
        {
            if (e.Key == Input.Key.Enter)
            {
                this.BtnLogin_Click(sender, e);
            }
        }

        private void TxtPassword_PasswordChanged(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._isPasswordVisible == false)
            {
                this.TxtPasswordVisible.Text = this.TxtPassword.Password;
            }
        }

        private void BtnTogglePassword_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            if (this._isPasswordVisible)
            {
                this.TxtPassword.Password = this.TxtPasswordVisible.Text;
                this.TxtPasswordVisible.Visibility = Win.Visibility.Collapsed;
                this.TxtPassword.Visibility = Win.Visibility.Visible;
                this.IconEye.Kind = Icons.PackIconMaterialKind.EyeOffOutline;
                this._isPasswordVisible = false;
            }
            else
            {
                this.TxtPasswordVisible.Text = this.TxtPassword.Password;
                this.TxtPassword.Visibility = Win.Visibility.Collapsed;
                this.TxtPasswordVisible.Visibility = Win.Visibility.Visible;
                this.IconEye.Kind = Icons.PackIconMaterialKind.EyeOutline;
                this._isPasswordVisible = true;
            }
        }

        private async void BtnLogin_Click(Sys.Object sender, Win.RoutedEventArgs e)
        {
            try
            {
                this.SetInputsEnabled(false);

                Sys.String emailInput = this.TxtEmail.Text;
                Sys.String passwordPlain = this._isPasswordVisible ? this.TxtPasswordVisible.Text : this.TxtPassword.Password;

                if (Sys.String.IsNullOrWhiteSpace(emailInput))
                {
                    Win.MessageBox.Show("Please enter your email.");
                    return;
                }

                if (Sys.String.IsNullOrWhiteSpace(passwordPlain))
                {
                    Win.MessageBox.Show("Please enter your password.");
                    return;
                }

                using var securePassword = new Sec.SecureString();
                foreach (char c in passwordPlain)
                {
                    securePassword.AppendChar(c);
                }
                securePassword.MakeReadOnly();

                (Sys.Boolean ok, Sys.String? accessToken, Sys.String? error) = await this._viewModel.LoginAsync(emailInput, securePassword).ConfigureAwait(true);

                if (ok)
                {
                    // Persist session for 1 month
                    MyAPP.Services.AuthState.SaveToDisk();
                    this.NavigateToDashboard();
                }
                else
                {
                    Win.MessageBox.Show(error ?? "Login failed.");
                }
            }
            catch (Sys.Exception)
            {
                Win.MessageBox.Show("An unexpected error occurred. Please try again.");
            }
            finally
            {
                this.SetInputsEnabled(true);
            }
        }

        private void SetInputsEnabled(Sys.Boolean isEnabled)
        {
            this.BtnLogin.IsEnabled = isEnabled;
            this.TxtEmail.IsEnabled = isEnabled;
            this.TxtPassword.IsEnabled = isEnabled;
            this.TxtPasswordVisible.IsEnabled = isEnabled;
        }

        private void NavigateToDashboard()
        {
            Win.Window? window = Win.Window.GetWindow(this);
            if (window != null && window is MyAPP.MainWindow mainWindow)
            {
                mainWindow.ShowModeSelection();
            }
            else
            {
                Win.MessageBox.Show("Login successful, but navigation failed.");
            }
        }
    }
}