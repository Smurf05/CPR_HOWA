using HOWA.Admin.ViewModels;

namespace HOWA.Admin.Views
{
    public partial class AttendancePage : ContentPage
    {
        private readonly AttendanceViewModel _viewModel;

        public AttendancePage(AttendanceViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadLogsAsync();
        }
    }
}
