using SistemaF.Infrastructure.Services;
using SistemaF.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Interfaces;

namespace SistemaF.Infrastructure.Repositories;

// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
//  STUB SERVICES \u2014 Implementazioni temporanee per la Wave 1 MVP
//
//  Questi stub permettono alla pipeline EmissioneOrdineService di girare
//  con dati realistici senza che CSFOrdCommon e CSFOff siano ancora migrati.
//
//  Verranno sostituiti con implementazioni reali in Wave 2:
//    IUltimiCostiService    \u2192 CSFOrdCommon.clsCampiStoriciProdotto
//    IListiniFornitorService \u2192 CSFOff.CListinoFornitore
//    IScontiCondizioniService \u2192 CSFOrdCommon.clsScontiCondizioni
//    IOfferteService        \u2192 CSFOff.COfferte57
//    IIndiciVenditaService  \u2192 CSFOrdCommon.clsCalcola (IndiceVendita_*)
//    IArchivioPropostaService \u2192 CSFOrdCommon + CSFMag
// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550

public sealed class StubUltimiCostiService : IUltimiCostiService
{
    // Restituisce un costo simulato ~60% del prezzo al pubblico
    public Task<IReadOnlyDictionary<Guid, decimal?>> GetUltimiCostiAsync(
        Guid prodottoId, IEnumerable<Guid> fornitoriIds,
        int giorniLookback, CancellationToken ct = default)
    {
        var result = fornitoriIds
            .Select((id, i) => (id, costo: i == 0 ? (decimal?)4.20m : null))
            .ToDictionary(x => x.id, x => x.costo);
        return Task.FromResult<IReadOnlyDictionary<Guid, decimal?>>(result);
    }
}

public sealed class StubListiniFornitorService : IListiniFornitorService
{
    public Task<IReadOnlyDictionary<Guid, decimal>> GetCostiListinoAsync(
        Guid prodottoId, IEnumerable<Guid> fornitoriIds, CancellationToken ct = default)
    {
        var result = fornitoriIds
            .Select((id, i) => (id, costo: 4.50m - i * 0.10m))
            .ToDictionary(x => x.id, x => Math.Max(3.00m, x.costo));
        return Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(result);
    }
}

public sealed class StubScontiCondizioniService : IScontiCondizioniService
{
    public Task<IReadOnlyDictionary<Guid, ScontoCondizione>> GetScontiAsync(
        Guid prodottoId, string settore, string classe, string categoriaRicetta,
        IEnumerable<(Guid Id, int Quantita)> fornitori, CancellationToken ct = default)
    {
        // Sconto del 3% per il grossista principale
        var result = fornitori
            .Select((f, i) => (f.Id, sc: new ScontoCondizione(
                Sconto:             i == 0 ? 3m : 0m,
                QuantitaArrotondata: f.Quantita,
                TipoCalcolo:        TipoCalcoloSconto.Imponibile)))
            .ToDictionary(x => x.Id, x => x.sc);
        return Task.FromResult<IReadOnlyDictionary<Guid, ScontoCondizione>>(result);
    }
}

public sealed class StubOfferteService : IOfferteService
{
    public Task<IReadOnlyList<OffertaProdotto>> GetOfferteAsync(
        Guid prodottoId, int quantitaMax, IEnumerable<Guid> fornitoriIds,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OffertaProdotto>>([]);
}

public sealed class StubIndiciVenditaService : IIndiciVenditaService
{
    // Indici simulati: ~2.5 pezzi/giorno = tipico prodotto medio farmacia
    public Task<decimal> GetTendenzialeAsync(Guid pid, DateOnly d, CancellationToken ct)
        => Task.FromResult(2.5m);
    public Task<decimal> GetAnnualeAsync(Guid pid, int anno, CancellationToken ct)
        => Task.FromResult(912m);
    public Task<decimal> GetMensileAsync(Guid pid, int anno, int mese, CancellationToken ct)
        => Task.FromResult(76m);
    public Task<decimal> GetPeriodoAsync(Guid pid, DateOnly dal, DateOnly al, CancellationToken ct)
        => Task.FromResult((decimal)(al.DayNumber - dal.DayNumber) * 2.5m);
    public Task<decimal> GetMediaAritmeticaAsync(Guid pid, DateOnly d, CancellationToken ct)
        => Task.FromResult(2.3m);
}

public sealed class StubArchivioPropostaService : IArchivioPropostaService
{
    public Task<IReadOnlyList<ProdottoArchivio>> GetProdottiDaOrdinareAsync(
        Guid configurazioneId, Guid fornitoreId,
        FiltriProdottoArchivio filtri, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ProdottoArchivio>>([]);
}

// \u2500\u2500 Repository implementations aggiornati (Ordine nuovo modulo) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

public sealed class OrdineRepositoryImpl(SistemaFDbContext db)
    : IOrdineRepository
{
    public Task<Ordine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Ordini.Include("_righe").FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.Ordini.AnyAsync(o => o.Id == id, ct);

    public async Task AddAsync(Ordine o, CancellationToken ct = default)
        => await db.Ordini.AddAsync(o, ct);

    public void Update(Ordine o) => db.Ordini.Update(o);
    public void Remove(Ordine o) => db.Ordini.Remove(o);

    public Task<Ordine?> GetByNumeroAsync(NumeroOrdine numero, CancellationToken ct)
        => db.Ordini.Include("_righe")
             .FirstOrDefaultAsync(o => o.Numero.Valore == numero.Valore, ct);

    public async Task<IReadOnlyList<Ordine>> GetByFornitoreAsync(
        Guid fornitoreId, CancellationToken ct)
        => await db.Ordini.Where(o => o.FornitoreId == fornitoreId)
             .OrderByDescending(o => o.DataEmissione).ToListAsync(ct);

    public async Task<IReadOnlyList<Ordine>> GetByPeriodoAsync(
        DateTime da, DateTime a, CancellationToken ct)
        => await db.Ordini
             .Where(o => o.DataEmissione >= da && o.DataEmissione <= a)
             .OrderByDescending(o => o.DataEmissione).ToListAsync(ct);

    public async Task<IReadOnlyList<Ordine>> GetByStatoAsync(
        StatoOrdine stato, CancellationToken ct)
        => await db.Ordini.Where(o => o.Stato == stato)
             .OrderByDescending(o => o.DataEmissione).ToListAsync(ct);

    public async Task<NumeroOrdine> GeneraNumeroProgressivoAsync(
        int anno, CancellationToken ct)
    {
        var prefisso = $"{anno}";
        var max = await db.Ordini
            .Where(o => o.Numero.Valore.StartsWith(prefisso))
            .MaxAsync(o => (string?)o.Numero.Valore, ct);
        var prog = max is null ? 1L : long.Parse(max[4..]) + 1;
        return NumeroOrdine.Da(anno, prog);
    }

    public Task<bool> EsisteOrdinePerFornitoreAsync(
        Guid fornitoreId, StatoOrdine stato, CancellationToken ct)
        => db.Ordini.AnyAsync(
             o => o.FornitoreId == fornitoreId && o.Stato == stato, ct);
}

public sealed class PropostaOrdineRepositoryImpl(SistemaFDbContext db)
    : IPropostaOrdineRepository
{
    public Task<PropostaOrdine?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ProposteOrdine.Include("_righe").FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.ProposteOrdine.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(PropostaOrdine p, CancellationToken ct = default)
        => await db.ProposteOrdine.AddAsync(p, ct);

    public void Update(PropostaOrdine p) => db.ProposteOrdine.Update(p);
    public void Remove(PropostaOrdine p) => db.ProposteOrdine.Remove(p);

    public Task<PropostaOrdine?> GetByOperatoreAttivaAsync(
        Guid operatoreId, CancellationToken ct)
        => db.ProposteOrdine.Include("_righe")
             .FirstOrDefaultAsync(p => p.OperatoreId == operatoreId
                 && (p.Stato == PropostaOrdine.StatoProposta.Bozza
                  || p.Stato == PropostaOrdine.StatoProposta.Completata), ct);

    public Task<PropostaOrdine?> GetByConfigurazioneAsync(
        Guid configId, CancellationToken ct)
        => db.ProposteOrdine.Include("_righe")
             .FirstOrDefaultAsync(p => p.ConfigurazioneId == configId, ct);

    public async Task<IReadOnlyList<PropostaOrdine>> GetByOperatoreAsync(
        Guid operatoreId, CancellationToken ct)
        => await db.ProposteOrdine
             .Where(p => p.OperatoreId == operatoreId)
             .OrderByDescending(p => p.DataCreazione).ToListAsync(ct);
}
