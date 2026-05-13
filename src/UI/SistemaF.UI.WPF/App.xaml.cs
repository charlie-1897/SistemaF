using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SistemaF.Application;
using SistemaF.Infrastructure;
using SistemaF.UI.WPF.ViewModels.Prodotti;
using SistemaF.UI.WPF.ViewModels.Shell;
using SistemaF.UI.WPF.Views.Shell;
using System.Windows;

namespace SistemaF.UI.WPF;

public partial class App : System.Windows.Application
{
    private IHost _host = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/sistemaf-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((ctx, services) =>
            {
                services.AddInfrastructure(ctx.Configuration);
                services.AddApplication();

                services.AddTransient<MainWindowViewModel>();
                services.AddTransient<RicercaProdottoViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var shell = _host.Services.GetRequiredService<MainWindow>();
        shell.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
