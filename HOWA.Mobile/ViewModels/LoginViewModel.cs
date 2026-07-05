using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Framework.Authentication;

namespace HOWA.Mobile.ViewModels
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
            RegisterNavCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteRegisterNavAsync());
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
        public ICommand RegisterNavCommand { get; }

        private async Task ExecuteLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var user = await _authService.AuthenticateAsync(Username, Password);
                if (user != null && user.Role == Shared.Constants.AppConstants.AttendeeRole)
                {
                    // Pass the UserId to Preferences or global app state to query logs later
                    Microsoft.Maui.Storage.Preferences.Set("CurrentUserId", user.UserId);
                    Microsoft.Maui.Storage.Preferences.Set("CurrentFullName", user.FullName);
                    Microsoft.Maui.Storage.Preferences.Set("CurrentUsername", user.Username);

                    await Microsoft.Maui.Controls.Shell.Current.GoToAsync("///DashboardPage");
                }
                else
                {
                    ErrorMessage = "Invalid credentials or account is not an Attendee.";
                }
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Service error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteRegisterNavAsync()
        {
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync("///RegisterPage");
        }
    }
}
