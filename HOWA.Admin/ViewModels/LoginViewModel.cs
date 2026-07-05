using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Framework.Authentication;

namespace HOWA.Admin.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService;
        private string _username;
        private string _password;
        private string _errorMessage;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            LoginCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteLoginAsync());
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }

        private async Task ExecuteLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _authService.AuthenticateAsync(Username, Password);
                if (user != null && user.Role == Shared.Constants.AppConstants.AdminRole)
                {
                    await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Success", $"Welcome back, {user.FullName}!", "OK");
                    await Microsoft.Maui.Controls.Shell.Current.GoToAsync("///DashboardPage");
                }
                else
                {
                    ErrorMessage = "Invalid Admin credentials.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Database error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
