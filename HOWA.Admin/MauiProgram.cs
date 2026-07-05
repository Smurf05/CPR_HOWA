using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using HOWA.Shared.Constants;
using HOWA.Shared.Configuration;
using HOWA.Repository.UnitOfWork;
using HOWA.Framework.Authentication;
using HOWA.Framework.Chatbot;
using HOWA.Framework.Database;
using HOWA.Framework.RFID;
using HOWA.Admin.ViewModels;
using HOWA.Admin.Views;

namespace HOWA.Admin;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Load appsettings.json from Raw assets (MAUI FileSystem API)
		System.IO.Stream? settingsStream = null;
		try { settingsStream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult(); }
		catch { /* file missing — use defaults */ }

		IConfiguration configuration = AppConfiguration.Build(settingsStream);
		settingsStream?.Dispose();
		builder.Services.AddSingleton(configuration);

		// Resolve connection string (falls back to DbConstants if file is missing)
		string connectionString = AppConfiguration.GetConnectionString(configuration);

		// Register Database Infrastructure
		builder.Services.AddSingleton(sp => new DbInitializer(connectionString));
		builder.Services.AddSingleton(sp => new DbSeeder(connectionString));
		builder.Services.AddSingleton(sp => new DbConnectionFactory(connectionString));

		// Register Services
		builder.Services.AddSingleton<IUnitOfWork>(sp => new UnitOfWork(connectionString));
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<ChatbotService>();
		builder.Services.AddSingleton<RfidService>();

		// Register ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<AttendanceViewModel>();
		builder.Services.AddTransient<ApprovalsViewModel>();
		builder.Services.AddTransient<ReportsViewModel>();
		builder.Services.AddTransient<ChatbotViewModel>();

		// Register Views
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<AttendancePage>();
		builder.Services.AddTransient<ApprovalsPage>();
		builder.Services.AddTransient<ReportsPage>();

		var app = builder.Build();

		// Initialize DB schema + seed data before app starts
		Task.Run(async () =>
		{
			try
			{
				var initializer = app.Services.GetRequiredService<DbInitializer>();
				await initializer.InitializeAsync();

				var seeder = app.Services.GetRequiredService<DbSeeder>();
				await seeder.SeedAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[DB Init] Failed: {ex.Message}");
			}
		}).GetAwaiter().GetResult();

		return app;
	}
}

