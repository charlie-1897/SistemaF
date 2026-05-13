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

public partial class App : System.Windows.Application
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
                    rollingInterval: RollingInterval.Day))
            .ConfigureAppConfiguration(cfg => cfg
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true))
            .ConfigureServices((ctx, services) =>
            {
                services.AddInfrastructure(ctx.Configuration);
                services.AddApplication();

                // ViewModel WPF
                services.AddTransient<ShellViewModel>();
                services.AddTransient<RicercaProdottoViewModel>();
                services.AddTransient<ShellWindow>();
            })
            .Build();

        await _host.StartAsync();

        var shell = _host.Services.GetRequiredService<ShellWindow>();
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
        base.OnExit(e);
    }
}
