using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaF.Integration.Federfarma;
using SistemaF.Integration.Federfarma.Dpc.Clients;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Clients;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Integration.Federfarma;

// ═══════════════════════════════════════════════════════════════════════════════
//  DEPENDENCY INJECTION — Federfarma Integration
//
//  Utilizzo in Program.cs:
//    builder.Services.AddFederfarma(builder.Configuration);
//
//  Sezione appsettings.json richiesta:
//    "Federfarma": {
//      "DpcEndpointUrl": "https://webdpc.federfarma.lombardia.it/...",
//      "DpcSoapAction":  "https://webdpc.federfarma.lombardia.it/.../GetFUR",
//      "WebCareEndpointBase": "https://webcare.federfarma.lombardia.it/{nomeAsl}/Service.svc",
//      "WebCareSoapAction": "http://tempuri.org/IService/GetCompetenza",
//      "TimeoutSecondi": 100
//    }
// ═══════════════════════════════════════════════════════════════════════════════

public static class FederfarmaServiceExtensions
{
    public static IServiceCollection AddFederfarma(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // Configurazione
        var config = configuration
            .GetSection("Federfarma")
            .Get<FederfarmaConfiguration>()
            ?? new FederfarmaConfiguration();

        services.AddSingleton(config);

        // Parsers (stateless, singleton)
        services.AddSingleton<DpcXmlParser>();
        services.AddSingleton<WebCareXmlParser>();

        // HttpClient DPC
        services.AddHttpClient<IFederfarmaWebDpcClient, FederfarmaWebDpcClient>(client =>
        {
            client.BaseAddress = new Uri(config.DpcEndpointUrl);
            client.Timeout     = TimeSpan.FromSeconds(config.TimeoutSecondi);
        });

        // HttpClient WebCare (URL dinamico, non ha BaseAddress fisso)
        services.AddHttpClient<IFederfarmaWebCareClient, FederfarmaWebCareClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(config.TimeoutSecondi);
        });

        return services;
    }
}
