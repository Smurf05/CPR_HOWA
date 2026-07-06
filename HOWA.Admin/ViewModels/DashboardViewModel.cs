using System;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.Models;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Admin.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private Event   _activeEvent;
        private int     _totalRegistered;
        private int     _totalPresent;
        private int     _pendingApprovals;
        private decimal _attendanceRate;
        private string  _simulatedScanValue;
        private string  _scanMethod;

        // OTP state — shown after a successful scan
        private bool   _otpVisible;
        private string _otpCode      = string.Empty;
        private string _otpInfo      = string.Empty;
        private int    _pendingOtpId;

        public DashboardViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            LoadStatsCommand  = new Microsoft.Maui.Controls.Command(async () => await LoadStatsAsync());
            SimulateScanCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteSimulateScanAsync());
            DismissOtpCommand = new Microsoft.Maui.Controls.Command(() => { OtpVisible = false; OtpCode = string.Empty; OtpInfo = string.Empty; });
            ScanMethod = "RFID";
        }

        public Event   ActiveEvent       { get => _activeEvent;       set => SetProperty(ref _activeEvent, value); }
        public int     TotalRegistered   { get => _totalRegistered;   set => SetProperty(ref _totalRegistered, value); }
        public int     TotalPresent      { get => _totalPresent;      set => SetProperty(ref _totalPresent, value); }
        public int     PendingApprovals  { get => _pendingApprovals;  set => SetProperty(ref _pendingApprovals, value); }
        public decimal AttendanceRate    { get => _attendanceRate;    set => SetProperty(ref _attendanceRate, value); }
        public string  SimulatedScanValue { get => _simulatedScanValue; set => SetProperty(ref _simulatedScanValue, value); }
        public string  ScanMethod        { get => _scanMethod;        set => SetProperty(ref _scanMethod, value); }

        public bool   OtpVisible { get => _otpVisible; set => SetProperty(ref _otpVisible, value); }
        public string OtpCode    { get => _otpCode;    set => SetProperty(ref _otpCode, value); }
        public string OtpInfo    { get => _otpInfo;    set => SetProperty(ref _otpInfo, value); }

        public ICommand LoadStatsCommand    { get; }
        public ICommand SimulateScanCommand { get; }
        public ICommand DismissOtpCommand   { get; }

        public async Task LoadStatsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                ActiveEvent = await _unitOfWork.Events.GetActiveOrLatestEventAsync();
                if (ActiveEvent != null)
                {
                    var stats = await _unitOfWork.Attendance.GetDashboardStatsAsync(ActiveEvent.EventId);
                    if (stats != null)
                    {
                        TotalRegistered  = stats.TotalRegistered;
                        TotalPresent     = stats.TotalPresent;
                        PendingApprovals = stats.PendingApprovals;
                        AttendanceRate   = stats.AttendanceRate;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
            }
            finally { IsBusy = false; }
        }

        private async Task ExecuteSimulateScanAsync()
        {
            if (string.IsNullOrWhiteSpace(SimulatedScanValue) || ActiveEvent == null)
                return;

            IsBusy = true;
            try
            {
                // Issue an OTP instead of logging directly
                var (otpId, code) = await _unitOfWork.Attendance.IssueOtpAsync(
                    SimulatedScanValue, ActiveEvent.EventId, ScanMethod);

                _pendingOtpId     = otpId;
                OtpCode           = code;
                OtpInfo           = $"Scan detected ({ScanMethod}). Show this code to the attendee. Expires in 5 minutes.";
                OtpVisible        = true;
                SimulatedScanValue = string.Empty;
            }
            catch (Exception ex)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage
                    .DisplayAlert("Scan Refused", ex.Message, "OK");
            }
            finally { IsBusy = false; }
        }
    }
}
