using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Interfaces;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Services;

// ══════════════════════════════════════════════════════════════════════════════
//  SESSIONE 5 MVP — Implementazioni reali dei 6 servizi (ex-Stub)
//
//  Proprietà reali verificate dai sorgenti:
//    GiacenzaMagazzino (VO): .Giacenza, .ScortaMinima, .ScortaMassima
//    Prodotto.GiacenzaEsposizione / .GiacenzaMagazzino → tipo GiacenzaMagazzino
//    RigaOrdine.Costo → CostoFornitore con .Imponibile e .Sconto
//    ConfigurazioneEmissioneFornitore.ConfigurazioneId, .FornitoreId
//    PropostaRiga.PropostaId, .ProdottoId
// ══════════════════════════════════════════════════════════════════════════════

// ─────────────────────────────────────────────────────────────────────────────
//  1. UltimiCostiService
// ─────────────────────────────────────────────────────────────────────────────
public sealed class UltimiCostiService(SistemaFDbContext db) : IUltimiCostiService
{
    public async Task<IReadOnlyDictionary<Guid, decimal?>> GetUltimiCostiAsync(
        Guid              prodottoId,
        IEnumerable<Guid> fornitoriIds,
        int               giorniLookback,
        CancellationToken ct = default)
    {
        var dal = DateTime.UtcNow.AddDays(-giorniLookback);
        var ids = fornitoriIds.ToList();

        var costiDb = await db.Ordini
            .Where(o => ids.Contains(o.FornitoreId)
                     && o.DataEmissione >= dal
                     && o.Stato == StatoOrdine.Ricevuto)
            .SelectMany(o => o.Righe
                .Where(r => r.ProdottoId == prodottoId)
                .Select(r => new
                {
                    o.FornitoreId,
                    Costo = (decimal?)r.Costo.Imponibile,
                    o.DataEmissione
                }))
            .GroupBy(x => x.FornitoreId)
            .Select(g => new
            {
                FornitoreId = g.Key,
                Costo = g.OrderByDescending(x => x.DataEmissione)
                         .Select(x => x.Costo)
                         .FirstOrDefault()
            })
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => costiDb.FirstOrDefault(x => x.FornitoreId == id)?.Costo);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  2. ListiniFornitorService
//     PropostaRiga non ha CostoUnitario direttamente (F1..F5 per fornitore).
//     Per ora restituisce 0 — la tabella ProposteRighe è vuota al primo avvio.
// ─────────────────────────────────────────────────────────────────────────────
public sealed class ListiniFornitorService(SistemaFDbContext db) : IListiniFornitorService
{
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetCostiListinoAsync(
        Guid              prodottoId,
        IEnumerable<Guid> fornitoriIds,
        CancellationToken ct = default)
    {
        var ids = fornitoriIds.ToList();

        // Cerca il costo negli ordini ricevuti (fonte più affidabile disponibile)
        var costiDb = await db.Ordini
            .Where(o => ids.Contains(o.FornitoreId) && o.Stato == StatoOrdine.Ricevuto)
            .SelectMany(o => o.Righe
                .Where(r => r.ProdottoId == prodottoId)
                .Select(r => new { o.FornitoreId, r.Costo.Imponibile, o.DataEmissione }))
            .GroupBy(x => x.FornitoreId)
            .Select(g => new
            {
                FornitoreId = g.Key,
                Costo = g.OrderByDescending(x => x.DataEmissione)
                         .Select(x => x.Imponibile)
                         .FirstOrDefault()
            })
            .ToListAsync(ct);

        return ids.ToDictionary(
            id => id,
            id => costiDb.FirstOrDefault(x => x.FornitoreId == id)?.Costo ?? 0m);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  3. ScontiCondizioniService
// ─────────────────────────────────────────────────────────────────────────────
public sealed class ScontiCondizioniService(SistemaFDbContext db) : IScontiCondizioniService
{
    public async Task<IReadOnlyDictionary<Guid, ScontoCondizione>> GetScontiAsync(
        Guid                                   prodottoId,
        string                                 settore,
        string                                 classe,
        string                                 categoriaRicetta,
        IEnumerable<(Guid Id, int Quantita)>   fornitori,
        CancellationToken                      ct = default)
    {
        var fornitoriList = fornitori.ToList();
        var ids = fornitoriList.Select(f => f.Id).ToList();

        var scontiDb = await db.Ordini
            .Where(o => ids.Contains(o.FornitoreId) && o.Stato == StatoOrdine.Ricevuto)
            .SelectMany(o => o.Righe
                .Where(r => r.ProdottoId == prodottoId)
                .Select(r => new { o.FornitoreId, Sconto = r.Costo.Sconto, o.DataEmissione }))
            .GroupBy(x => x.FornitoreId)
            .Select(g => new
            {
                FornitoreId = g.Key,
                Sconto = g.OrderByDescending(x => x.DataEmissione)
                          .Select(x => x.Sconto)
                          .FirstOrDefault()
            })
            .ToListAsync(ct);

        return fornitoriList.ToDictionary(
            f => f.Id,
            f =>
            {
                var sconto = scontiDb.FirstOrDefault(x => x.FornitoreId == f.Id)?.Sconto ?? 0m;
                return new ScontoCondizione(
                    Sconto:              sconto,
                    QuantitaArrotondata: f.Quantita,
                    TipoCalcolo:         TipoCalcoloSconto.Imponibile);
            });
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  4. OfferteService
// ─────────────────────────────────────────────────────────────────────────────
public sealed class OfferteService(SistemaFDbContext db) : IOfferteService
{
    public async Task<IReadOnlyList<OffertaProdotto>> GetOfferteAsync(
        Guid              prodottoId,
        int               quantitaMax,
        IEnumerable<Guid> fornitoriIds,
        CancellationToken ct = default)
    {
        var ids  = fornitoriIds.ToList();
        var oggi = DateOnly.FromDateTime(DateTime.Today);

        return await db.OfferteFornitore
            .Where(o => o.ProdottoId     == prodottoId
                     && ids.Contains(o.FornitoreId)
                     && o.DataInizio     <= oggi
                     && o.DataFine       >= oggi
                     && o.QuantitaMinima <= quantitaMax)
            .Select(o => new OffertaProdotto(
                o.FornitoreId,
                o.CostoOfferta,
                o.ScontoCalcolato,
                o.QuantitaMinima,
                o.QuantitaOmaggio))
            .ToListAsync(ct);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  5. IndiciVenditaService
// ─────────────────────────────────────────────────────────────────────────────
public sealed class IndiciVenditaService(SistemaFDbContext db) : IIndiciVenditaService
{
    public async Task<decimal> GetTendenzialeAsync(Guid pid, DateOnly d, CancellationToken ct)
    {
        var dal = d.AddDays(-30);
        var tot = await db.MovimentiVendita
            .Where(m => m.ProdottoId == pid && m.Data >= dal && m.Data <= d)
            .SumAsync(m => (decimal?)m.Quantita, ct) ?? 0m;
        return Math.Round(tot / 30m, 2);
    }

    public async Task<decimal> GetAnnualeAsync(Guid pid, int anno, CancellationToken ct)
    {
        var dal = new DateOnly(anno, 1, 1);
        var al  = new DateOnly(anno, 12, 31);
        return await db.MovimentiVendita
            .Where(m => m.ProdottoId == pid && m.Data >= dal && m.Data <= al)
            .SumAsync(m => (decimal?)m.Quantita, ct) ?? 0m;
    }

    public async Task<decimal> GetMensileAsync(Guid pid, int anno, int mese, CancellationToken ct)
    {
        var dal = new DateOnly(anno, mese, 1);
        var al  = new DateOnly(anno, mese, DateTime.DaysInMonth(anno, mese));
        return await db.MovimentiVendita
            .Where(m => m.ProdottoId == pid && m.Data >= dal && m.Data <= al)
            .SumAsync(m => (decimal?)m.Quantita, ct) ?? 0m;
    }

    public async Task<decimal> GetPeriodoAsync(Guid pid, DateOnly dal, DateOnly al, CancellationToken ct)
        => await db.MovimentiVendita
            .Where(m => m.ProdottoId == pid && m.Data >= dal && m.Data <= al)
            .SumAsync(m => (decimal?)m.Quantita, ct) ?? 0m;

    public async Task<decimal> GetMediaAritmeticaAsync(Guid pid, DateOnly d, CancellationToken ct)
    {
        var vendite = new List<decimal>();
        for (int i = 0; i < 12; i++)
        {
            var m   = d.AddMonths(-i);
            var dal = new DateOnly(m.Year, m.Month, 1);
            var al  = new DateOnly(m.Year, m.Month, DateTime.DaysInMonth(m.Year, m.Month));
            var tot = await db.MovimentiVendita
                .Where(mv => mv.ProdottoId == pid && mv.Data >= dal && mv.Data <= al)
                .SumAsync(mv => (decimal?)mv.Quantita, ct) ?? 0m;
            vendite.Add(tot);
        }
        return vendite.Count == 0 ? 0m : Math.Round(vendite.Average(), 2);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  6. ArchivioPropostaService
//     GiacenzaMagazzino è un Value Object: usa .Giacenza, .ScortaMinima, .ScortaMassima
//     Prodotto non ha FornitorePreferenzialeId né SettoreInventario come props dirette
// ─────────────────────────────────────────────────────────────────────────────
public sealed class ArchivioPropostaService(SistemaFDbContext db) : IArchivioPropostaService
{
    public async Task<IReadOnlyList<ProdottoArchivio>> GetProdottiDaOrdinareAsync(
        Guid                   configurazioneId,
        Guid                   fornitoreId,
        FiltriProdottoArchivio filtri,
        CancellationToken      ct = default)
    {
        var query = db.Prodotti.AsNoTracking().Where(p => p.IsAttivo);

        // Filtra solo trattati se configurato
        if (filtri.IsTrattati)
            query = query.Where(p => p.IsTrattato);

        // Filtra sotto-scorta: giacenza esposizione sotto scorta minima
        if (filtri.RipristinoScorta is not null)
            query = query.Where(p =>
                p.GiacenzaEsposizione.Giacenza <= p.GiacenzaEsposizione.ScortaMinima ||
                p.GiacenzaMagazzino.Giacenza   <= p.GiacenzaMagazzino.ScortaMinima);

        var prodotti = await query
            .OrderBy(p => p.Descrizione)
            .Take(200)
            .Select(p => new ProdottoArchivio(
                p.Id,
                p.CodiceFarmaco,
                p.Descrizione,
                0,                                       // Quantita proposta (calcolata a runtime)
                p.GiacenzaEsposizione.Giacenza,
                p.GiacenzaEsposizione.ScortaMinima,
                p.GiacenzaEsposizione.ScortaMassima,
                p.GiacenzaMagazzino.Giacenza,
                p.GiacenzaMagazzino.ScortaMinima,
                p.GiacenzaMagazzino.ScortaMassima,
                0m,                                      // UltimoCostoGrossista (da UltimiCostiService)
                0m,                                      // UltimoCostoDitta (da UltimiCostiService)
                "",                                      // SettoreInventario non su Prodotto
                null))                                   // FornitorePreferenzialeId non su Prodotto
            .ToListAsync(ct);

        return prodotti;
    }
}
