using HOWA.Mobile.Views;

namespace HOWA.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes that are navigated to programmatically (not in XAML shell)
        Routing.RegisterRoute("OtpPage", typeof(OtpPage));
    }
}
