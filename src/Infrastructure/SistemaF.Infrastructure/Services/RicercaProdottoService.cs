using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Entities.Ricerca;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Services;

// ═══════════════════════════════════════════════════════════════════════════════
//  RICERCA PRODOTTO SERVICE — Infrastructure
//
//  Implementazione EF Core di IRicercaProdottoService.
//  Sostituisce le query SQL dirette su ProdBase/ProdEsteso del VB6.
//
//  Le 7 strategie di ricerca VB6 (costanti CSF*) mappano su metodi LINQ:
//    CSFMINISTERIALE (1) → WHERE CodiceFarmaco = @termine (match esatto)
//    CSFDESCRIZIONE  (4) → WHERE Descrizione LIKE @termine%
//    CSFGRUPPO       (5) → WHERE Gruppo LIKE @termine%
//    CSFATC          (8) → WHERE CodiceATC LIKE @termine%
//    CSFCODICEEAN   (15) → WHERE CodiceEAN = @termine (match esatto)
//    CSFCODGTIN     (27) → WHERE CodiceEAN = @termine13 (GTIN→EAN strip)
//    CSFDITTA        (6) → WHERE CodiceDitta = @termine
//    (default)           → CSFDESCRIZIONE
//
//  Il fulltext avanzato del VB6 (ricerca estesa con "*") diventa qui
//  LIKE '%termine%' con EF Core.
// ═══════════════════════════════════════════════════════════════════════════════

internal sealed class RicercaProdottoService(SistemaFDbContext db)
    : IRicercaProdottoService
{
    public async Task<IReadOnlyList<RisultatoRicerca>> CercaAsync(
        CriterioRicerca criterio, CancellationToken ct = default)
    {
        var query = db.Prodotti
            .AsNoTracking()
            .Where(p => p.IsAttivo);

        // Filtro settore (es. solo farmaci "A", solo parafarmaci, ecc.)
        if (criterio.SettoreInventario is not null)
            query = query.Where(p =>
                EF.Functions.Like(p.SettoreInventario ?? "", criterio.SettoreInventario));

        // Applica la strategia di ricerca corretta
        query = criterio.Tipo switch
        {
            TipoRicercaProdotto.CodiceMinistriale =>
                query.Where(p => p.CodiceFarmaco.Valore == criterio.Termine),

            TipoRicercaProdotto.CodiceEAN =>
                query.Where(p => p.CodiceEAN != null
                              && p.CodiceEAN.Valore == criterio.Termine),

            TipoRicercaProdotto.CodiceGTIN =>
                // GTIN-14: stripa leading '0' per ottenere EAN-13
                query.Where(p => p.CodiceEAN != null
                              && p.CodiceEAN.Valore == criterio.Termine.TrimStart('0')),

            TipoRicercaProdotto.CodiceATC =>
                query.Where(p => p.CodiceATC != null
                              && EF.Functions.Like(p.CodiceATC, criterio.Termine + "%")),

            TipoRicercaProdotto.GruppoFarmaceutico =>
                query.Where(p => p.Gruppo != null
                              && EF.Functions.Like(p.Gruppo, criterio.TerminePerLike)),

            TipoRicercaProdotto.Ditta =>
                query.Where(p => p.CodiceDitta != null
                              && EF.Functions.Like(p.CodiceDitta, criterio.TerminePerLike)),

            TipoRicercaProdotto.CategoriaRicetta =>
                query.Where(p => p.Classe.ToString() == criterio.Termine),

            // Descrizione è il default (CSFDESCRIZIONE = 4)
            _ =>
                query.Where(p => EF.Functions.Like(
                    p.Descrizione, criterio.TerminePerLike))
        };

        // Ordinamento: prima disponibili, poi per descrizione (come nel VB6)
        var prodotti = await query
            .OrderByDescending(p => p.GiacenzaEsposizione.Giacenza > 0)
            .ThenBy(p => p.Descrizione)
            .Take(criterio.MaxRisultati)
            .ToListAsync(ct);

        return prodotti.Select(ToRisultato).ToList();
    }

    public async Task<RisultatoRicerca?> TrovaEsattoAsync(
        string codice, CancellationToken ct = default)
    {
        codice = codice.Trim();

        Prodotto? prodotto = null;

        // Codice ministeriale esatto (9 cifre)
        if (codice.All(char.IsDigit) && codice.Length == 9)
            prodotto = await db.Prodotti.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodiceFarmaco.Valore == codice
                                       && p.IsAttivo, ct);

        // EAN-8 o EAN-13
        if (prodotto is null && codice.All(char.IsDigit)
            && (codice.Length == 8 || codice.Length == 13))
            prodotto = await db.Prodotti.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodiceEAN != null
                                       && p.CodiceEAN.Valore == codice
                                       && p.IsAttivo, ct);

        // Formato "A" + 9 cifre (scanner legacy)
        if (prodotto is null && codice.Length == 10 && codice[0] == 'A'
            && codice[1..].All(char.IsDigit))
            prodotto = await db.Prodotti.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CodiceFarmaco.Valore == codice[1..]
                                       && p.IsAttivo, ct);

        return prodotto is null ? null : ToRisultato(prodotto);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static RisultatoRicerca ToRisultato(Prodotto p) => new(
        ProdottoId:          p.Id,
        CodiceFarmaco:       p.CodiceFarmaco.Valore,
        CodiceEAN:           p.CodiceEAN?.Valore,
        CodiceATC:           p.CodiceATC,
        Descrizione:         p.Descrizione,
        Classe:              p.Classe.ToString(),
        CategoriaRicetta:    p.CategoriaRicetta.ToString(),
        PrezzoVendita:       p.PrezzoVendita.Valore,
        AliquotaIVA:         p.PrezzoVendita.AliquotaIVA,
        GiacenzaEsposizione: p.GiacenzaEsposizione.Giacenza,
        GiacenzaMagazzino:   p.GiacenzaMagazzino.Giacenza,
        GiacenzaTotale:      p.GiacenzaEsposizione.Giacenza + p.GiacenzaMagazzino.Giacenza,
        IsStupefacente:      p.IsStupefacente,
        IsVeterinario:       p.IsVeterinario,
        IsCongelato:         p.IsCongelato,
        IsAttivo:            p.IsAttivo);
}
