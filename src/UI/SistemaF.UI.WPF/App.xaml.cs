using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SistemaF.Application;
using SistemaF.Infrastructure;
using System.Windows;

namespace SistemaF.UI.WPF;

/// <summary>
/// Entry point WPF. Sostituisce Main.frm + InizializzaAmbiente del VB6.
/// Usa Microsoft.Extensions.Hosting per DI, Configuration e Logging.
/// </summary>
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
                // Application layer (MediatR + FluentValidation)
                services.AddApplication();

                // Infrastructure (EF Core, repositories, stub services, Federfarma)
                services.AddInfrastructure(ctx.Configuration);

                // WPF ViewModels
                services.AddTransient<ViewModels.Prodotti.RicercaProdottoViewModel>();

                // Finestre WPF (registrate come Transient)
                // services.AddTransient<Views.Shell.MainWindow>();
            })
            .Build();

        // Applica migrations e seed al primo avvio (solo in Development)
        if (_host.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
            await InfrastructureDependencyInjection.InitializeDatabaseAsync(
                _host.Services, seedDemoData: true);

        await _host.StartAsync();

        // TODO Sessione 4: aprire MainWindow con navigation
        // var window = _host.Services.GetRequiredService<Views.Shell.MainWindow>();
        // window.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
