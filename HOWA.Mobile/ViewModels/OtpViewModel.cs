using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Mobile.ViewModels
{
    public class OtpViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;

        private string _otpCode       = string.Empty;
        private int    _otpId;
        private string _resultMessage = string.Empty;
        private string _countdownText = "Expires in 5:00";
        private bool   _isSuccess;
        private bool   _resultVisible;

        private CancellationTokenSource? _countdownCts;

        public OtpViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork   = unitOfWork;
            VerifyCommand = new Microsoft.Maui.Controls.Command(
                async () => await ExecuteVerifyAsync(),
                () => !IsBusy && OtpCode?.Length == 6);
        }

        public string OtpCode
        {
            get => _otpCode;
            set
            {
                if (SetProperty(ref _otpCode, value))
                    ((Microsoft.Maui.Controls.Command)VerifyCommand).ChangeCanExecute();
            }
        }

        public string CountdownText { get => _countdownText; set => SetProperty(ref _countdownText, value); }
        public string ResultMessage { get => _resultMessage; set => SetProperty(ref _resultMessage, value); }
        public bool   IsSuccess     { get => _isSuccess;     set => SetProperty(ref _isSuccess, value); }
        public bool   ResultVisible { get => _resultVisible; set => SetProperty(ref _resultVisible, value); }

        public ICommand VerifyCommand { get; }

        /// <summary>Called by the page — sets the OTP id from navigation param.</summary>
        public void SetOtpId(int otpId)
        {
            _otpId = otpId;
            StartCountdown();
        }

        /// <summary>Called by the page — pre-fills the code field from navigation param.</summary>
        public void SetCode(string code)
        {
            OtpCode = code;
        }

        // ----------------------------------------------------------------
        // 5-minute countdown displayed on the OTP screen
        // ----------------------------------------------------------------
        private void StartCountdown()
        {
            _countdownCts?.Cancel();
            _countdownCts = new CancellationTokenSource();
            var token = _countdownCts.Token;
            var expiry = DateTime.Now.AddMinutes(5);

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    var remaining = expiry - DateTime.Now;
                    if (remaining <= TimeSpan.Zero)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                            CountdownText = "⏰ OTP Expired — please scan again");
                        break;
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                        CountdownText = $"Expires in {remaining.Minutes}:{remaining.Seconds:D2}");

                    await Task.Delay(1000, token);
                }
            }, token);
        }

        // ----------------------------------------------------------------
        // Verify the entered code
        // ----------------------------------------------------------------
        private async Task ExecuteVerifyAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ResultVisible = false;

            try
            {
                var result = await _unitOfWork.Attendance.VerifyOtpAndLogAsync(_otpId, OtpCode.Trim());

                IsSuccess     = result.Success;
                ResultMessage = result.Success
                    ? $"✅ {result.Message}"
                    : $"❌ {result.Message}";
                ResultVisible = true;

                if (result.Success)
                {
                    _countdownCts?.Cancel();
                    OtpCode = string.Empty;
                    await Task.Delay(2000);
                    await Microsoft.Maui.Controls.Shell.Current.GoToAsync("///DashboardPage");
                }
            }
            catch (Exception ex)
            {
                IsSuccess     = false;
                ResultMessage = $"❌ Error: {ex.Message}";
                ResultVisible = true;
            }
            finally
            {
                IsBusy = false;
                ((Microsoft.Maui.Controls.Command)VerifyCommand).ChangeCanExecute();
            }
        }
    }
}
