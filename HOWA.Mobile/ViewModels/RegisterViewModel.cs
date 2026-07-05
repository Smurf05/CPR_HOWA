using System;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.Models;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Mobile.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private string _username;
        private string _password;
        private string _firstName;
        private string _lastName;
        private string _email;
        private string _contactNo;
        private string _statusMessage;

        public RegisterViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            RegisterCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteRegisterAsync());
            LoginNavCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteLoginNavAsync());
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

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string ContactNo
        {
            get => _contactNo;
            set => SetProperty(ref _contactNo, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand LoginNavCommand { get; }

        private async Task ExecuteRegisterAsync()
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
            {
                StatusMessage = "Please fill in all required fields.";
                return;
            }

            IsBusy = true;
            StatusMessage = string.Empty;

            try
            {
                var user = new User
                {
                    Username = Username.Trim(),
                    Password = Password,
                    Role = Shared.Constants.AppConstants.AttendeeRole,
                    FirstName = FirstName.Trim(),
                    LastName = LastName.Trim(),
                    Email = Email?.Trim(),
                    ContactNo = ContactNo?.Trim()
                };

                // Generate unique QR code payload for the user
                var qrPayload = $"HOWA_{user.Username}_QR_TOKEN";

                var attendee = new Attendee
                {
                    QrCodeData = qrPayload,
                    Status = Shared.Constants.AppConstants.Status.Pending
                };

                var success = await _unitOfWork.Attendees.RegisterAsync(user, attendee);
                if (success)
                {
                    await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(
                        "Registration Submitted", 
                        "Registration submitted successfully! Please wait for admin approval.", 
                        "OK");
                    
                    // Clear fields
                    Username = Password = FirstName = LastName = Email = ContactNo = string.Empty;
                    await ExecuteLoginNavAsync();
                }
                else
                {
                    StatusMessage = "Registration failed. Username may already exist.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Registration Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteLoginNavAsync()
        {
            await Microsoft.Maui.Controls.Shell.Current.GoToAsync("///LoginPage");
        }
    }
}
