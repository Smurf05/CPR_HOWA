namespace HOWA.Admin;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Hide tab bar on login page
        Navigating += OnNavigating;
    }

    private void OnNavigating(object sender, ShellNavigatingEventArgs e)
    {
        var route = e.Target?.Location?.ToString() ?? string.Empty;
        bool isLoginPage = route.Contains("LoginPage");
        Shell.SetTabBarIsVisible(Current, !isLoginPage);
    }
}
