using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.Models;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Mobile.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private string _fullName;
        private string _username;
        private string _email;
        private string _contactNo;
        private string _rfidUid;
        private ObservableCollection<AttendanceLog> _history;

        public ProfileViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            History = new ObservableCollection<AttendanceLog>();
            LoadProfileCommand = new Microsoft.Maui.Controls.Command(async () => await LoadProfileAsync());
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
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

        public string RfidUid
        {
            get => _rfidUid;
            set => SetProperty(ref _rfidUid, value);
        }

        public ObservableCollection<AttendanceLog> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        public ICommand LoadProfileCommand { get; }

        public async Task LoadProfileAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                int userId = Microsoft.Maui.Storage.Preferences.Get("CurrentUserId", 0);
                if (userId > 0)
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user != null)
                    {
                        FullName = user.FullName;
                        Username = user.Username;
                        Email = user.Email;
                        ContactNo = user.ContactNo;

                        var attendee = await _unitOfWork.Attendees.GetByUserIdAsync(userId);
                        if (attendee != null)
                        {
                            RfidUid = string.IsNullOrEmpty(attendee.RfidUid) ? "No RFID tag linked" : attendee.RfidUid;

                            History.Clear();
                            var logs = await _unitOfWork.Attendance.GetAttendeeHistoryAsync(attendee.AttendeeId);
                            foreach (var log in logs)
                            {
                                History.Add(log);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Profile Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
