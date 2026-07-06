using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Framework.QRCode;
using HOWA.Repository.UnitOfWork;
using Microsoft.Maui.Controls;

namespace HOWA.Mobile.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IUnitOfWork   _unitOfWork;
        private readonly QrCodeService _qrCodeService;

        private string      _fullName;
        private string      _status;
        private ImageSource _qrCodeImage;
        private bool        _isApproved;
        private int         _currentAttendeeId;

        // Polling
        private CancellationTokenSource? _pollCts;

        public DashboardViewModel(IUnitOfWork unitOfWork, QrCodeService qrCodeService)
        {
            _unitOfWork    = unitOfWork;
            _qrCodeService = qrCodeService;
            LoadUserCommand = new Command(async () => await LoadUserAsync());
        }

        public string      FullName    { get => _fullName;    set => SetProperty(ref _fullName, value); }
        public string      Status      { get => _status;      set => SetProperty(ref _status, value); }
        public ImageSource QrCodeImage { get => _qrCodeImage; set => SetProperty(ref _qrCodeImage, value); }
        public bool        IsApproved  { get => _isApproved;  set => SetProperty(ref _isApproved, value); }

        public ICommand LoadUserCommand { get; }

        // ----------------------------------------------------------------
        // Called by the page on Appearing — loads user then starts polling
        // ----------------------------------------------------------------
        public async Task StartAsync()
        {
            await LoadUserAsync();
            if (IsApproved)
                StartPolling();
        }

        // ----------------------------------------------------------------
        // Called by the page on Disappearing — stops polling
        // ----------------------------------------------------------------
        public void StopPolling()
        {
            _pollCts?.Cancel();
            _pollCts = null;
        }

        // ----------------------------------------------------------------
        // Polling loop — checks every 3 s for a new OTP
        // ----------------------------------------------------------------
        private void StartPolling()
        {
            StopPolling();
            _pollCts = new CancellationTokenSource();
            var token = _pollCts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(3000, token);
                        if (token.IsCancellationRequested) break;

                        var pending = await _unitOfWork.Attendance.GetPendingOtpAsync(_currentAttendeeId);
                        if (pending.HasValue)
                        {
                            StopPolling(); // stop before navigating

                            // Navigate on the UI thread
                            await MainThread.InvokeOnMainThreadAsync(async () =>
                            {
                                await Shell.Current.GoToAsync(
                                    $"OtpPage?otpId={pending.Value.OtpId}&otpCode={pending.Value.Code}");
                            });
                            break;
                        }
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[OTP Poll] {ex.Message}");
                    }
                }
            }, token);
        }

        // ----------------------------------------------------------------
        // Load user profile + QR code
        // ----------------------------------------------------------------
        public async Task LoadUserAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                int userId = Microsoft.Maui.Storage.Preferences.Get("CurrentUserId", 0);
                FullName   = Microsoft.Maui.Storage.Preferences.Get("CurrentFullName", "Attendee Member");

                if (userId > 0)
                {
                    var attendee = await _unitOfWork.Attendees.GetByUserIdAsync(userId);
                    if (attendee != null)
                    {
                        _currentAttendeeId = attendee.AttendeeId;
                        Status     = attendee.Status;
                        IsApproved = string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);

                        if (IsApproved)
                        {
                            var pngBytes = _qrCodeService.GenerateQrCodePng(attendee.QrCodeData);
                            QrCodeImage  = ImageSource.FromStream(() => new MemoryStream(pngBytes));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
