using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SistemaF.Application.Anagrafica.Queries;
using SistemaF.Application.Ordini.Commands;
using SistemaF.Application.Ricerca.Queries;
using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Entities.Ricerca;
using SistemaF.Domain.ValueObjects;
using SistemaF.Integration.Tests.Infrastructure;
using Xunit;

namespace SistemaF.Integration.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST DI INTEGRAZIONE — Sessione 3 MVP
//
//  Questi test usano un database SQLite in-memory reale.
//  Verificano che tutti i layer (Domain → Application → Infrastructure → DB)
//  funzionino insieme correttamente.
//
//  Ogni classe di test crea un DbContext fresco e isolato.
// ═══════════════════════════════════════════════════════════════════════════════

// ══════════════════════════════════════════════════════════════════════════════
//  1. REPOSITORY TESTS — verifica che EF Core salvi e legga correttamente
// ══════════════════════════════════════════════════════════════════════════════

public sealed class ProdottoRepositoryTests : IDisposable
{
    private readonly SistemaFTestDbContext _ctx = new();

    [Fact]
    public async Task Salva_e_rilegge_prodotto_correttamente()
    {
        var p = TestFactory.Prodotto("023569287", "AMOXICILLINA 1G");
        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();

        await repo.AddAsync(p);
        await uow.SaveChangesAsync();

        // Pulisce il tracking di EF Core per simulare una nuova richiesta
        _ctx.Db.ChangeTracker.Clear();

        var letto = await repo.GetByIdAsync(p.Id);
        letto.Should().NotBeNull();
        letto!.CodiceFarmaco.Valore.Should().Be("023569287");
        letto.Descrizione.Should().Be("AMOXICILLINA 1G");
        letto.GiacenzaEsposizione.Giacenza.Should().Be(10);
        letto.GiacenzaMagazzino.Giacenza.Should().Be(30);
    }

    [Fact]
    public async Task Salva_prodotto_con_lotto_e_scadenza()
    {
        var p = TestFactory.Prodotto("034512367", "ASPIRINA 500MG");
        p.AggiungLotto(
            CodiceLotto.Da("LOT2024001"),
            new DateOnly(2026, 12, 31),
            50);

        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(p);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var letto = await repo.GetByIdAsync(p.Id);
        letto!.Scadenze.Should().HaveCount(1);
        letto.Scadenze[0].Lotto.Valore.Should().Be("LOT2024001");
        letto.Scadenze[0].DataScadenza.Should().Be(new DateOnly(2026, 12, 31));
        letto.Scadenze[0].Quantita.Should().Be(50);
    }

    [Fact]
    public async Task GetByCodiceFarmacoAsync_trova_prodotto_esistente()
    {
        var p    = TestFactory.Prodotto("099887766");
        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(p);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var trovato = await repo.GetByCodiceFarmacoAsync(
            CodiceProdotto.Da("099887766"));
        trovato.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDelete_nasconde_prodotto_da_query_standard()
    {
        var p    = TestFactory.Prodotto("011223344", "PRODOTTO DA ELIMINARE");
        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(p);
        await uow.SaveChangesAsync();

        p.Elimina();
        repo.Update(p);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        // La global query filter deve escludere i prodotti cancellati
        var trovato = await repo.GetByIdAsync(p.Id);
        trovato.Should().BeNull();
    }

    [Fact]
    public async Task Rettifica_giacenza_persistita_correttamente()
    {
        var p = TestFactory.Prodotto("055443322", qtaExp: 20, qtaMag: 60);
        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(p);
        await uow.SaveChangesAsync();

        // Scarica 5 pezzi dall'esposizione
        p.VariaGiacenzaEsposizione(
            ModalitaVariazioneGiacenza.Sottrazione, 5,
            TipoModuloRettifica.Vendita);
        repo.Update(p);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var letto = await repo.GetByIdAsync(p.Id);
        letto!.GiacenzaEsposizione.Giacenza.Should().Be(15);
    }

    public void Dispose() => _ctx.Dispose();
}

// ══════════════════════════════════════════════════════════════════════════════
//  2. RICERCA PRODOTTO TESTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class RicercaProdottoTests : IDisposable
{
    private readonly SistemaFTestDbContext _ctx = new();

    public RicercaProdottoTests()
    {
        // Inserisce prodotti noti nel DB prima di ogni test
        var repo = _ctx.GetService<IProdottoRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();

        var prodotti = new[]
        {
            TestFactory.Prodotto("023569287", "AMOXICILLINA EG 1G 12 CPR"),
            TestFactory.Prodotto("026974015", "AUGMENTIN 1G 12 CPR"),
            TestFactory.Prodotto("034512367", "CARDIOASPIRINA 100MG 30 CPR"),
            TestFactory.Prodotto("023145698", "TACHIPIRINA 1000MG 16 CPR"),
            TestFactory.Prodotto("028745632", "BUSCOPAN 10MG 30 CPR"),
        };

        // EAN per due prodotti
        prodotti[0].ImpostaCodici(CodiceEAN.Da("8033070038018"), null, null, null);
        prodotti[2].ImpostaCodici(CodiceEAN.Da("8004395050277"), null, null, null);

        foreach (var p in prodotti)
            repo.AddAsync(p).GetAwaiter().GetResult();

        uow.SaveChangesAsync().GetAwaiter().GetResult();
        _ctx.Db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Ricerca_per_codice_ministeriale_esatto()
    {
        var svc = _ctx.GetService<IRicercaProdottoService>();
        var crit = CriterioRicerca.Con("023569287", TipoRicercaProdotto.CodiceMinistriale);
        var ris  = await svc.CercaAsync(crit);

        ris.Should().HaveCount(1);
        ris[0].Descrizione.Should().Be("AMOXICILLINA EG 1G 12 CPR");
    }

    [Fact]
    public async Task Ricerca_per_descrizione_parziale()
    {
        var svc  = _ctx.GetService<IRicercaProdottoService>();
        var crit = CriterioRicerca.Con("AMOX", TipoRicercaProdotto.Descrizione);
        var ris  = await svc.CercaAsync(crit);

        ris.Should().HaveCount(1);
        ris[0].CodiceFarmaco.Should().Be("023569287");
    }

    [Fact]
    public async Task Ricerca_per_EAN_esatto()
    {
        var svc  = _ctx.GetService<IRicercaProdottoService>();
        var crit = CriterioRicerca.Con("8033070038018", TipoRicercaProdotto.CodiceEAN);
        var ris  = await svc.CercaAsync(crit);

        ris.Should().HaveCount(1);
        ris[0].Descrizione.Should().Be("AMOXICILLINA EG 1G 12 CPR");
    }

    [Fact]
    public async Task Ricerca_ritorna_vuoto_se_non_trovato()
    {
        var svc  = _ctx.GetService<IRicercaProdottoService>();
        var crit = CriterioRicerca.Con("INESISTENTE", TipoRicercaProdotto.Descrizione);
        var ris  = await svc.CercaAsync(crit);
        ris.Should().BeEmpty();
    }

    [Fact]
    public async Task Ricerca_estesa_trova_con_wildcard()
    {
        var svc  = _ctx.GetService<IRicercaProdottoService>();
        var crit = CriterioRicerca.Con("1G CPR", TipoRicercaProdotto.Descrizione,
            ricercaEstesa: true);
        var ris  = await svc.CercaAsync(crit);
        ris.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task TrovaEsattoAsync_ministeriale_funziona()
    {
        var svc = _ctx.GetService<IRicercaProdottoService>();
        var ris = await svc.TrovaEsattoAsync("023569287");
        ris.Should().NotBeNull();
        ris!.Descrizione.Should().Be("AMOXICILLINA EG 1G 12 CPR");
    }

    [Fact]
    public async Task TrovaEsattoAsync_EAN_funziona()
    {
        var svc = _ctx.GetService<IRicercaProdottoService>();
        var ris = await svc.TrovaEsattoAsync("8033070038018");
        ris.Should().NotBeNull();
    }

    [Fact]
    public async Task TrovaEsattoAsync_codice_inesistente_ritorna_null()
    {
        var svc = _ctx.GetService<IRicercaProdottoService>();
        var ris = await svc.TrovaEsattoAsync("999999999");
        ris.Should().BeNull();
    }

    // ── Autodetect tipo ────────────────────────────────────────────────────────

    [Theory]
    [InlineData("023569287", TipoRicercaProdotto.CodiceMinistriale)]  // 9 cifre
    [InlineData("8033070038018", TipoRicercaProdotto.CodiceEAN)]      // 13 cifre
    [InlineData("12345678", TipoRicercaProdotto.CodiceEAN)]           // 8 cifre
    [InlineData("N02BE", TipoRicercaProdotto.CodiceATC)]              // ATC
    [InlineData("ASPIRINA", TipoRicercaProdotto.Descrizione)]         // testo
    [InlineData("CARDIO ASPIRINA", TipoRicercaProdotto.Descrizione)]  // testo con spazio
    public void CriterioRicerca_autodetect_tipo_corretto(
        string termine, TipoRicercaProdotto tipoAtteso)
    {
        var c = CriterioRicerca.Rileva(termine);
        c.Tipo.Should().Be(tipoAtteso);
    }

    [Fact]
    public async Task MediatR_CercaProdotti_query_funziona()
    {
        var mediator = _ctx.GetService<IMediator>();
        var ris = await mediator.Send(
            new CercaProdottiQuery("AMOX", MaxRisultati: 10));
        ris.Should().HaveCount(1);
    }

    [Fact]
    public async Task MediatR_TrovaProdottoDaCodice_funziona()
    {
        var mediator = _ctx.GetService<IMediator>();
        var ris = await mediator.Send(
            new TrovaProdottoDaCodiceQuery("023569287"));
        ris.Should().NotBeNull();
    }

    public void Dispose() => _ctx.Dispose();
}

// ══════════════════════════════════════════════════════════════════════════════
//  3. ANAGRAFICA REPOSITORY TESTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class AnagraficaRepositoryTests : IDisposable
{
    private readonly SistemaFTestDbContext _ctx = new();

    [Fact]
    public async Task Salva_e_rilegge_fornitore()
    {
        var f    = TestFactory.Grossista("COMIFAR TEST", 1001);
        var repo = _ctx.GetService<IFornitoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(f);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var letto = await repo.GetByCodiceAnabaseAsync(1001);
        letto.Should().NotBeNull();
        letto!.RagioneSociale.Should().Be("COMIFAR TEST");
        letto.IsAttivo.Should().BeTrue();
    }

    [Fact]
    public async Task GetAttiviAsync_esclude_disattivati()
    {
        var f1 = TestFactory.Grossista("ATTIVO", 2001);
        var f2 = TestFactory.Grossista("DISATTIVATO", 2002);
        f2.Disattiva();

        var repo = _ctx.GetService<IFornitoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(f1);
        await repo.AddAsync(f2);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var attivi = await repo.GetAttiviAsync();
        attivi.Should().HaveCount(1);
        attivi[0].RagioneSociale.Should().Be("ATTIVO");
    }

    [Fact]
    public async Task Salva_e_rilegge_operatore()
    {
        var o    = TestFactory.Operatore("mario.test", "Mario Test");
        var repo = _ctx.GetService<IOperatoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(o);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        var letto = await repo.GetByLoginAsync("mario.test");
        letto.Should().NotBeNull();
        letto!.NomeCognome.Should().Be("Mario Test");
    }

    [Fact]
    public async Task GetByLoginAsync_login_case_insensitive()
    {
        var o    = TestFactory.Operatore("lucia.bianchi");
        var repo = _ctx.GetService<IOperatoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(o);
        await uow.SaveChangesAsync();
        _ctx.Db.ChangeTracker.Clear();

        // Cerca con case diverso
        var letto = await repo.GetByLoginAsync("LUCIA.BIANCHI");
        letto.Should().NotBeNull();
    }

    [Fact]
    public async Task VerificaCredenziali_query_via_mediator()
    {
        var o    = TestFactory.Operatore("admin.test");
        var repo = _ctx.GetService<IOperatoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(o);
        await uow.SaveChangesAsync();

        var mediator = _ctx.GetService<IMediator>();
        var result   = await mediator.Send(new VerificaCredenziliQuery(
            "admin.test",
            "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Login.Should().Be("admin.test");
    }

    [Fact]
    public async Task VerificaCredenziali_password_errata_fallisce()
    {
        var o    = TestFactory.Operatore("admin.test2");
        var repo = _ctx.GetService<IOperatoreRepository>();
        var uow  = _ctx.GetService<IUnitOfWork>();
        await repo.AddAsync(o);
        await uow.SaveChangesAsync();

        var mediator = _ctx.GetService<IMediator>();
        var result   = await mediator.Send(new VerificaCredenziliQuery(
            "admin.test2", "passwordsbagliata"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PASSWORD_ERRATA");
    }

    public void Dispose() => _ctx.Dispose();
}

// ══════════════════════════════════════════════════════════════════════════════
//  4. PIPELINE ORDINE END-TO-END
// ══════════════════════════════════════════════════════════════════════════════

public sealed class PipelineOrdineIntegrationTests : IDisposable
{
    private readonly SistemaFTestDbContext _ctx = new();
    private readonly Guid _operatoreId;
    private readonly Guid _configurazioneId;
    private readonly Guid _fornitoreId;

    public PipelineOrdineIntegrationTests()
    {
        var fornitore = TestFactory.Grossista();
        var operatore = TestFactory.Operatore();
        var config    = TestFactory.Configurazione();

        var fRepo = _ctx.GetService<IFornitoreRepository>();
        var oRepo = _ctx.GetService<IOperatoreRepository>();
        var cRepo = _ctx.GetService<IConfigurazioneEmissioneRepository>();
        var pRepo = _ctx.GetService<IProdottoRepository>();
        var uow   = _ctx.GetService<IUnitOfWork>();

        fRepo.AddAsync(fornitore).GetAwaiter().GetResult();
        oRepo.AddAsync(operatore).GetAwaiter().GetResult();
        cRepo.AddAsync(config).GetAwaiter().GetResult();

        // 5 prodotti con giacenze
        for (var i = 0; i < 5; i++)
        {
            var p = TestFactory.Prodotto(
                $"0{i:D8}", $"PRODOTTO TEST {i:D2}",
                qtaExp: 2, qtaMag: 10);
            pRepo.AddAsync(p).GetAwaiter().GetResult();
        }

        uow.SaveChangesAsync().GetAwaiter().GetResult();

        _operatoreId     = operatore.Id;
        _configurazioneId = config.Id;
        _fornitoreId     = fornitore.Id;
    }

    [Fact]
    public async Task CreaPropostaOrdine_via_command_handler()
    {
        var mediator = _ctx.GetService<IMediator>();

        var result = await mediator.Send(new CreaPropostaOrdineCommand(
            OperatoreId:     _operatoreId,
            ConfigurazioneId: _configurazioneId,
            NomeEmissione:   "Test Emissione",
            DaArchivio:      true,
            DaNecessita:     true,
            DaPrenotati:     false,
            DaSospesi:       false));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreaPropostaOrdine_due_volte_stesso_operatore_fallisce()
    {
        var mediator = _ctx.GetService<IMediator>();

        await mediator.Send(new CreaPropostaOrdineCommand(
            _operatoreId, _configurazioneId,
            "Prima proposta", true, true, false, false));

        // La seconda deve fallire perché c'è già una proposta attiva
        var result = await mediator.Send(new CreaPropostaOrdineCommand(
            _operatoreId, _configurazioneId,
            "Seconda proposta", true, true, false, false));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("PROPOSTA_GIA_ATTIVA");
    }

    [Fact]
    public async Task AggiungiProdottoAProposta_e_verifica_persistenza()
    {
        var pRepo    = _ctx.GetService<IProdottoRepository>();
        var pRepo2   = _ctx.GetService<IPropostaOrdineRepository>();
        var mediator = _ctx.GetService<IMediator>();

        // Prende un prodotto esistente
        var prodotti = await pRepo.GetAllAsync();
        var prodotto = prodotti.First();

        // Crea proposta
        var creaResult = await mediator.Send(new CreaPropostaOrdineCommand(
            _operatoreId, _configurazioneId,
            "Test Add Prodotto", true, true, false, false));
        creaResult.IsSuccess.Should().BeTrue();

        // Aggiunge il prodotto
        var addResult = await mediator.Send(new AggiungiProdottoProposta(
            PropostaId:     creaResult.Value,
            ProdottoId:     prodotto.Id,
            Quantita:       5,
            Fonte:          FonteAggiunta.Mancanti,
            IndiceFornitore: 1,
            OperatoreId:    _operatoreId));

        addResult.IsSuccess.Should().BeTrue();

        // Verifica che la riga sia stata salvata
        _ctx.Db.ChangeTracker.Clear();
        var proposta = await pRepo2.GetByIdAsync(creaResult.Value);
        proposta.Should().NotBeNull();
        proposta!.Righe.Should().HaveCount(1);
        proposta.Righe[0].QuantitaMancante.Should().Be(5);
    }

    [Fact]
    public async Task Pipeline_completa_crea_proposta_completata()
    {
        var pRepo2   = _ctx.GetService<IPropostaOrdineRepository>();
        var mediator = _ctx.GetService<IMediator>();

        // Crea proposta
        var creaResult = await mediator.Send(new CreaPropostaOrdineCommand(
            _operatoreId, _configurazioneId,
            "Test Pipeline Completa", true, true, false, false));
        creaResult.IsSuccess.Should().BeTrue();

        var propostaId = creaResult.Value;

        // Aggiunge fornitore alla proposta
        var proposta = await pRepo2.GetByIdAsync(propostaId);
        proposta!.AggiungiFornitori([
            new InfoFornitore(_fornitoreId, 1001,
                TipoFornitore.Grossista, false, 100, false)
        ]);

        // Aggiunge 3 prodotti manualmente (simula il recupero da archivio
        // che gli stub di archivio non forniscono)
        var prodotti = await _ctx.GetService<IProdottoRepository>().GetAllAsync();
        for (var i = 0; i < Math.Min(3, prodotti.Count); i++)
        {
            proposta.AggiungiProdottoManuale(
                prodotti[i].Id,
                prodotti[i].CodiceFarmaco,
                prodotti[i].Descrizione,
                quantita: 5,
                FonteAggiunta.Necessita,
                indiceFornitore1Based: 1);
        }

        _ctx.GetService<IPropostaOrdineRepository>().Update(proposta);
        await _ctx.GetService<IUnitOfWork>().SaveChangesAsync();

        // Esegue la pipeline
        var pipelineResult = await mediator.Send(
            new EseguiPipelineEmissioneCommand(propostaId, _operatoreId));

        pipelineResult.IsSuccess.Should().BeTrue();
        pipelineResult.Value!.NumeroProdottiEsaminati.Should().BeGreaterThan(0);
        _ctx.Db.ChangeTracker.Clear();

        var propostaAggiornata = await pRepo2.GetByIdAsync(propostaId);
        propostaAggiornata!.Stato.Should().Be(PropostaOrdine.StatoProposta.Completata);
    }

    public void Dispose() => _ctx.Dispose();
}

// ══════════════════════════════════════════════════════════════════════════════
//  5. SEED DATA TESTS
// ══════════════════════════════════════════════════════════════════════════════

public sealed class SeedDataTests : IDisposable
{
    private readonly SistemaFTestDbContext _ctx = new();

    [Fact]
    public async Task Seed_inserisce_50_prodotti()
    {
        await _ctx.SeedBaseAsync();
        var count = await _ctx.Db.Prodotti.CountAsync();
        count.Should().Be(50);
    }

    [Fact]
    public async Task Seed_inserisce_3_fornitori()
    {
        await _ctx.SeedBaseAsync();
        var count = await _ctx.Db.Fornitori.CountAsync();
        count.Should().Be(3);
    }

    [Fact]
    public async Task Seed_inserisce_operatori()
    {
        await _ctx.SeedBaseAsync();
        var count = await _ctx.Db.Operatori.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task Seed_inserisce_farmacia()
    {
        await _ctx.SeedBaseAsync();
        var farm = _ctx.GetService<IFarmaciaRepository>();
        var f    = await farm.GetFarmaciaCorrente();
        f.Should().NotBeNull();
        f!.CodiceAsl.Should().Be("030313");
    }

    [Fact]
    public async Task Seed_inserisce_configurazioni_emissione()
    {
        await _ctx.SeedBaseAsync();
        var count = await _ctx.Db.ConfigurazioniEmissione.CountAsync();
        count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Seed_idempotente_seconda_esecuzione_non_duplica()
    {
        await _ctx.SeedBaseAsync();
        await _ctx.SeedBaseAsync();   // seconda chiamata
        var count = await _ctx.Db.Prodotti.CountAsync();
        count.Should().Be(50);        // ancora 50, non 100
    }

    public void Dispose() => _ctx.Dispose();
}
