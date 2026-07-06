using HOWA.Mobile.ViewModels;

namespace HOWA.Mobile.Views
{
    [QueryProperty(nameof(OtpId),   "otpId")]
    [QueryProperty(nameof(OtpCode), "otpCode")]
    public partial class OtpPage : ContentPage
    {
        private readonly OtpViewModel _viewModel;

        public OtpPage(OtpViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
        }

        /// <summary>Receives the OtpId from navigation query.</summary>
        public string OtpId
        {
            set
            {
                if (int.TryParse(value, out int id))
                    _viewModel.SetOtpId(id);
            }
        }

        /// <summary>
        /// Receives the pre-filled code from navigation query so the
        /// attendee sees the 6-digit code already entered — they just
        /// tap Confirm.
        /// </summary>
        public string OtpCode
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                    _viewModel.SetCode(value);
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            OtpEntry.Focus();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("///DashboardPage");
        }
    }
}
