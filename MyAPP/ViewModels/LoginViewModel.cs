using Sys = System;
using Tasks = System.Threading.Tasks;
using Sec = System.Security;

namespace MyAPP.ViewModels
{
    public sealed class LoginViewModel
    {
        private readonly MyAPP.Services.SupabaseAuthService _authService;
        private Sys.Boolean _isBusy;

        public LoginViewModel()
        {
            this._authService = new MyAPP.Services.SupabaseAuthService();
            this._isBusy = false;
        }

        public Sys.Boolean IsBusy
        {
            get => this._isBusy;
            private set => this._isBusy = value;
        }

        public async Tasks.Task<(Sys.Boolean ok, Sys.String? accessToken, Sys.String? error)> LoginAsync(Sys.String email, Sec.SecureString password)
        {
            if (Sys.String.IsNullOrWhiteSpace(email))
            {
                return (false, null, "Email is required.");
            }

            if (password == null || password.Length == 0)
            {
                return (false, null, "Password is required.");
            }

            this.IsBusy = true;
            try
            {
                MyAPP.Services.SupabaseAuthService.AuthResult result = await this._authService.SignInAsync(email.Trim(), password).ConfigureAwait(false);

                if (result == null)
                {
                    return (false, null, "Critical: Service returned null.");
                }

                if (result.IsSuccess)
                {
                    MyAPP.Services.AuthState.SetToken(result.AccessToken);
                    MyAPP.Services.AuthState.SetUser(result.UserId);
                    return (true, result.AccessToken, null);
                }

                return (false, null, result.ErrorMessage ?? "Unknown authentication error.");
            }
            catch (Sys.Exception)
            {
                return (false, null, "An unexpected error occurred during login.");
            }
            finally
            {
                this.IsBusy = false;
            }
        }
    }
}