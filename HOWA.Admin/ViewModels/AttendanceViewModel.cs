using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.DTOs;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Admin.ViewModels
{
    public class AttendanceViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private ObservableCollection<AttendanceReportDto> _logs;

        public AttendanceViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            Logs = new ObservableCollection<AttendanceReportDto>();
            LoadLogsCommand = new Microsoft.Maui.Controls.Command(async () => await LoadLogsAsync());
        }

        public ObservableCollection<AttendanceReportDto> Logs
        {
            get => _logs;
            set => SetProperty(ref _logs, value);
        }

        public ICommand LoadLogsCommand { get; }

        public async Task LoadLogsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                Logs.Clear();
                var activeEvent = await _unitOfWork.Events.GetActiveOrLatestEventAsync();
                if (activeEvent != null)
                {
                    var reports = await _unitOfWork.Attendance.GetAttendanceReportAsync(activeEvent.EventId, null);
                    foreach (var item in reports)
                    {
                        Logs.Add(item);
                    }
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading logs: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
