using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Framework.QRCode;
using HOWA.Repository.UnitOfWork;
using Microsoft.Maui.Controls;

namespace HOWA.Mobile.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly QrCodeService _qrCodeService;
        private string _fullName;
        private string _status;
        private ImageSource _qrCodeImage;
        private bool _isApproved;

        public DashboardViewModel(IUnitOfWork unitOfWork, QrCodeService qrCodeService)
        {
            _unitOfWork = unitOfWork;
            _qrCodeService = qrCodeService;
            LoadUserCommand = new Command(async () => await LoadUserAsync());
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public ImageSource QrCodeImage
        {
            get => _qrCodeImage;
            set => SetProperty(ref _qrCodeImage, value);
        }

        public bool IsApproved
        {
            get => _isApproved;
            set => SetProperty(ref _isApproved, value);
        }

        public ICommand LoadUserCommand { get; }

        public async Task LoadUserAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                int userId = Microsoft.Maui.Storage.Preferences.Get("CurrentUserId", 0);
                FullName = Microsoft.Maui.Storage.Preferences.Get("CurrentFullName", "Attendee Member");

                if (userId > 0)
                {
                    var attendee = await _unitOfWork.Attendees.GetByUserIdAsync(userId);
                    if (attendee != null)
                    {
                        Status = attendee.Status;
                        IsApproved = string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);

                        if (IsApproved)
                        {
                            var pngBytes = _qrCodeService.GenerateQrCodePng(attendee.QrCodeData);
                            QrCodeImage = ImageSource.FromStream(() => new MemoryStream(pngBytes));
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
