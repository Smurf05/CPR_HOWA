using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using HOWA.Domain.DTOs;
using HOWA.Repository.UnitOfWork;

namespace HOWA.Admin.ViewModels
{
    public class ReportsViewModel : BaseViewModel
    {
        private readonly IUnitOfWork _unitOfWork;
        private ObservableCollection<AttendanceReportDto> _reportList;
        private string _selectedStatus;

        public ReportsViewModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            ReportList = new ObservableCollection<AttendanceReportDto>();
            LoadReportCommand = new Microsoft.Maui.Controls.Command(async () => await LoadReportAsync());
            ExportCsvCommand = new Microsoft.Maui.Controls.Command(async () => await ExecuteExportCsvAsync());
        }

        public ObservableCollection<AttendanceReportDto> ReportList
        {
            get => _reportList;
            set => SetProperty(ref _reportList, value);
        }

        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        public ICommand LoadReportCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public async Task LoadReportAsync()
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                ReportList.Clear();
                var activeEvent = await _unitOfWork.Events.GetActiveOrLatestEventAsync();
                if (activeEvent != null)
                {
                    var data = await _unitOfWork.Attendance.GetAttendanceReportAsync(activeEvent.EventId, SelectedStatus);
                    foreach (var item in data)
                    {
                        ReportList.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error exporting report: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteExportCsvAsync()
        {
            if (ReportList.Count == 0)
                return;

            IsBusy = true;
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("LogId,AttendeeId,FullName,Role,EventName,EventDate,ScannedAt,Method,Status");

                foreach (var row in ReportList)
                {
                    sb.AppendLine($"{row.LogId},{row.AttendeeId},\"{row.FullName}\",{row.UserRole},\"{row.EventName}\",{row.EventDate:yyyy-MM-dd HH:mm},{row.ScannedAt:yyyy-MM-dd HH:mm:ss},{row.AttendanceMethod},{row.AttendanceStatus}");
                }

                var fileName = $"HOWA_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
                
                await File.WriteAllTextAsync(filePath, sb.ToString());
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Exported", $"Successfully exported to temporary path: {filePath}", "OK");
            }
            catch (Exception ex)
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Export Failed", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
