using HOWA.Admin.ViewModels;

namespace HOWA.Admin.Views
{
    public partial class ApprovalsPage : ContentPage
    {
        private readonly ApprovalsViewModel _viewModel;

        public ApprovalsPage(ApprovalsViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPendingAsync();
        }
    }
}
