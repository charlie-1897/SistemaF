using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SistemaF.Infrastructure;
using SistemaF.UI.WPF.ViewModels.Prodotti;
using SistemaF.UI.WPF.ViewModels.Shell;
using SistemaF.UI.WPF.Views.Shell;
using System.Windows;

namespace SistemaF.UI.WPF;

public partial class App : Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .UseSerilog((ctx, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/sistemaf-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30))
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                cfg.AddJsonFile(
                    $"appsettings.{ctx.HostingEnvironment.EnvironmentName}.json",
                    optional: true, reloadOnChange: true);
                cfg.AddEnvironmentVariables("SISTEMAF_");
            })
            .ConfigureServices((ctx, services) =>
            {
                services.AddApplication();
                services.AddInfrastructure(ctx.Configuration);

                // ViewModels
                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<RicercaProdottoViewModel>();

                // Finestre
                services.AddTransient<MainWindow>();
            })
            .Build();

        if (_host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
            await InfrastructureDependencyInjection.InitializeDatabaseAsync(
                _host.Services, seedDemoData: true);

        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>Punto di accesso globale ai servizi (per XAML code-behind).</summary>
    public static T GetService<T>() where T : notnull
        => ((App)Current)._host.Services.GetRequiredService<T>();
}
