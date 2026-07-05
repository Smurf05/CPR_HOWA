using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.Models;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Admin.ViewModels
{
    public class ApprovalsViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private ObservableCollection<Attendee> _pendingList;
        private Attendee _selectedAttendee;
        private string _rfidInput;

        public ApprovalsViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            PendingList = new ObservableCollection<Attendee>();
            LoadPendingCommand = new Microsoft.Maui.Controls.Command(async () => await LoadPendingAsync());
            ApproveCommand = new Microsoft.Maui.Controls.Command<Attendee>(async (attendee) => await ExecuteApproveAsync(attendee));
            RejectCommand = new Microsoft.Maui.Controls.Command<Attendee>(async (attendee) => await ExecuteRejectAsync(attendee));
        }

        public ObservableCollection<Attendee> PendingList
        {
            get => _pendingList;
            set => SetProperty(ref _pendingList, value);
        }

        public Attendee SelectedAttendee
        {
            get => _selectedAttendee;
            set => SetProperty(ref _selectedAttendee, value);
        }

        public string RfidInput
        {
            get => _rfidInput;
            set => SetProperty(ref _rfidInput, value);
        }

        public ICommand LoadPendingCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        public async Task LoadPendingAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                PendingList.Clear();
                var list = await _unitOfWork.Attendees.GetPendingApprovalsAsync();
                foreach (var item in list)
                {
                    PendingList.Add(item);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading approvals: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteApproveAsync(Attendee attendee)
        {
            if (attendee == null) return;

            if (string.IsNullOrWhiteSpace(RfidInput))
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Validation", "Please enter an RFID card UID to link.", "OK");
                return;
            }

            IsBusy = true;
            try
            {
                var success = await _unitOfWork.Attendees.ApproveAsync(attendee.AttendeeId, RfidInput, 1);
                if (success)
                {
                    RfidInput = string.Empty;
                    await LoadPendingAsync();
                }
            }
            catch (System.Exception ex)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Approval Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteRejectAsync(Attendee attendee)
        {
            if (attendee == null) return;

            IsBusy = true;
            try
            {
                var success = await _unitOfWork.Attendees.RejectAsync(attendee.AttendeeId);
                if (success)
                {
                    await LoadPendingAsync();
                }
            }
            catch (System.Exception ex)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Rejection Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
