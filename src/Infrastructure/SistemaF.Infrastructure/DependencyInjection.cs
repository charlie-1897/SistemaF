using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Interfaces;
using SistemaF.Domain.Entities.Ricerca;
using SistemaF.Infrastructure.Persistence;
using SistemaF.Infrastructure.Repositories;
using SistemaF.Infrastructure.Services;
using SistemaF.Integration.Federfarma;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // Database
        services.AddDbContext<SistemaFDbContext>(opts =>
        {
            var cs = configuration.GetConnectionString("SistemaF")
                ?? throw new InvalidOperationException("Connection string 'SistemaF' mancante.");
            opts.UseSqlServer(cs, sql =>
            {
                sql.MigrationsAssembly(typeof(SistemaFDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                sql.CommandTimeout(30);
            });
#if DEBUG
            opts.EnableDetailedErrors().EnableSensitiveDataLogging();
#endif
        });

        // Unit of Work e repositories
        services.AddScoped<IUnitOfWork,               UnitOfWork>();
        services.AddScoped<IProdottoRepository,       ProdottoRepository>();
        services.AddScoped<IOrdineRepository,         OrdineRepositoryImpl>();
        services.AddScoped<IPropostaOrdineRepository, PropostaOrdineRepositoryImpl>();

        // Domain services
        services.AddScoped<EmissioneOrdineService>();

        // Anagrafica repositories (Sessione 2 MVP)
        services.AddScoped<IFornitoreRepository,                FornitoreRepository>();
        services.AddScoped<IOperatoreRepository,                OperatoreRepository>();
        services.AddScoped<IConfigurazioneEmissioneRepository,  ConfigurazioneEmissioneRepository>();
        services.AddScoped<IFarmaciaRepository,                 FarmaciaRepository>();

        // Stub services (temporanei fino a Wave 2)
        services.AddScoped<IUltimiCostiService,     StubUltimiCostiService>();
        services.AddScoped<IListiniFornitorService,  StubListiniFornitorService>();
        services.AddScoped<IScontiCondizioniService, StubScontiCondizioniService>();
        services.AddScoped<IOfferteService,          StubOfferteService>();
        services.AddScoped<IIndiciVenditaService,    StubIndiciVenditaService>();
        services.AddScoped<IArchivioPropostaService, StubArchivioPropostaService>();

        // Ricerca prodotto (Sessione 3 MVP)
        services.AddScoped<IRicercaProdottoService, RicercaProdottoService>();

        // Federfarma
        services.AddFederfarma(configuration);

        return services;
    }

    public static async Task InitializeDatabaseAsync(
        IServiceProvider services, bool seedDemoData = true)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SistemaFDbContext>();
        await db.Database.MigrateAsync();
        if (seedDemoData)
        {
            await Seed.DataSeeder.SeedAsync(db);
            await Seed.DataSeeder.SeedAnagraficaAsync(db);
        }
    }
}
