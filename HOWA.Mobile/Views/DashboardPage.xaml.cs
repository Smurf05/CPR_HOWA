using HOWA.Mobile.ViewModels;

namespace HOWA.Mobile.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.StartAsync(); // loads user + starts OTP polling
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _viewModel.StopPolling(); // cancel background poll when leaving page
        }
    }
}
