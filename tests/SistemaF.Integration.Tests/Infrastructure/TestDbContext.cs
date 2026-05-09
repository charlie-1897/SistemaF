using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using SistemaF.Application;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Entities.Ricerca;
using SistemaF.Domain.ValueObjects;
using SistemaF.Infrastructure;
using SistemaF.Infrastructure.Persistence;
using SistemaF.Infrastructure.Persistence.Seed;
using SistemaF.Infrastructure.Repositories;
using SistemaF.Infrastructure.Services;

namespace SistemaF.Integration.Tests.Infrastructure;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST INFRASTRUCTURE
//
//  Fornisce un DbContext SQLite in-memory per i test di integrazione.
//  Ogni test riceve un database fresco e isolato.
//
//  SQLite in-memory è scelto perché:
//    - Non richiede SQL Server installato
//    - È ordini di grandezza più veloce dei test su SQL Server
//    - Supporta tutte le migration EF Core
//    - È il pattern raccomandato da Microsoft per i test EF Core
//
//  NOTA: SQLite non supporta alcune features SQL Server-specifiche
//  (es. rowversion, alcune date functions). I test che richiedono
//  SQL Server vero sono marcati con [Trait("Category","SqlServer")].
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Factory per creare un DbContext SQLite in-memory isolato per ogni test.
/// Implementa IDisposable per chiudere la connessione al termine del test.
/// </summary>
public sealed class SistemaFTestDbContext : IDisposable
{
    private readonly SqliteConnection _connection;
    public  SistemaFDbContext         Db      { get; }
    public  IServiceProvider          Services { get; }

    public SistemaFTestDbContext()
    {
        // Connessione SQLite in-memory persistente per la durata del test
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();

        services.AddDbContext<SistemaFDbContext>(opts =>
            opts.UseSqlite(_connection));

        // Application layer
        services.AddApplication();

        // Repository e servizi
        services.AddScoped<IUnitOfWork,               UnitOfWork>();
        services.AddScoped<IProdottoRepository,       ProdottoRepository>();
        services.AddScoped<IOrdineRepository,         OrdineRepositoryImpl>();
        services.AddScoped<IPropostaOrdineRepository, PropostaOrdineRepositoryImpl>();
        services.AddScoped<IFornitoreRepository,      FornitoreRepository>();
        services.AddScoped<IOperatoreRepository,      OperatoreRepository>();
        services.AddScoped<IConfigurazioneEmissioneRepository, ConfigurazioneEmissioneRepository>();
        services.AddScoped<IFarmaciaRepository,       FarmaciaRepository>();
        services.AddScoped<IRicercaProdottoService,   RicercaProdottoService>();

        // Stub services per la pipeline ordini
        services.AddScoped<IUltimiCostiService,     StubUltimiCostiService>();
        services.AddScoped<IListiniFornitorService,  StubListiniFornitorService>();
        services.AddScoped<IScontiCondizioniService, StubScontiCondizioniService>();
        services.AddScoped<IOfferteService,          StubOfferteService>();
        services.AddScoped<IIndiciVenditaService,    StubIndiciVenditaService>();
        services.AddScoped<IArchivioPropostaService, StubArchivioPropostaService>();
        services.AddScoped<EmissioneOrdineService>();

        // Logging silenzioso nei test
        services.AddLogging();

        Services = services.BuildServiceProvider();
        Db       = Services.GetRequiredService<SistemaFDbContext>();

        // Crea lo schema (equivalente a dotnet ef database update)
        Db.Database.EnsureCreated();
    }

    /// <summary>Inserisce dati di test minimi (5 prodotti + 1 fornitore + 1 operatore).</summary>
    public async Task SeedBaseAsync()
    {
        await DataSeeder.SeedAsync(Db);
        await DataSeeder.SeedAnagraficaAsync(Db);
    }

    /// <summary>Risolve un servizio dal container DI del test.</summary>
    public T GetService<T>() where T : notnull
        => Services.GetRequiredService<T>();

    public void Dispose()
    {
        Db.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}

/// <summary>
/// Helper per costruire Prodotto di test in modo fluente.
/// Usato per non ripetere lo stesso boilerplate in ogni test.
/// </summary>
public static class TestFactory
{
    public static Prodotto Prodotto(
        string codice       = "012345678",
        string descrizione  = "PRODOTTO TEST 100MG",
        ClasseFarmaco classe = ClasseFarmaco.C,
        decimal prezzo      = 5.00m,
        int iva             = 10,
        int qtaExp          = 10,
        int qtaMag          = 30)
    {
        var p = Domain.Entities.Prodotto.Prodotto.Crea(
            CodiceProdotto.Da(codice),
            descrizione,
            classe,
            CategoriaRicetta.NessunObbligo,
            Prezzo.Di(prezzo, iva));

        p.VariaGiacenzaEsposizione(
            ModalitaVariazioneGiacenza.Sostituzione, qtaExp,
            TipoModuloRettifica.Magazzino);

        p.ImpostaScorteEsposizione(3, qtaExp + 5);

        p.VariaGiacenzaMagazzino(
            ModalitaVariazioneGiacenza.Sostituzione, qtaMag,
            TipoModuloRettifica.Magazzino);

        p.ClearDomainEvents();
        return p;
    }

    public static Fornitore Grossista(
        string ragioneSociale = "GROSSISTA TEST S.P.A.",
        long codiceAnabase    = 1001)
    {
        var f = Domain.Entities.Anagrafica.Fornitore.Crea(
            ragioneSociale, TipoFornitore.Grossista, "01234567891");
        f.ImpostaCodiceAnabase(codiceAnabase);
        f.ImpostaParametriCommerciali(100_000m, 100);
        f.ClearDomainEvents();
        return f;
    }

    public static Operatore Operatore(
        string login       = "test.operatore",
        string nomeCognome = "Test Operatore")
        => Domain.Entities.Anagrafica.Operatore.Crea(
            login, nomeCognome,
            "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918");

    public static ConfigurazioneEmissione Configurazione(
        string nome = "Test Config")
    {
        var c = Domain.Entities.Anagrafica.ConfigurazioneEmissione.Crea(nome);
        c.ImpostaFonti(true, true, false, false);
        return c;
    }
}
