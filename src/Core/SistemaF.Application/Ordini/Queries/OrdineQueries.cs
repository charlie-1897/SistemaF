using MediatR;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Interfaces;

namespace SistemaF.Application.Ordini.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
//  QUERIES — Modulo Ordine
// ═══════════════════════════════════════════════════════════════════════════════

// ── GetOrdineById ──────────────────────────────────────────────────────────────

public sealed record GetOrdineByIdQuery(Guid OrdineId) : IRequest<OrdineDto?>;

public sealed class GetOrdineByIdHandler(IOrdineRepository repo)
    : IRequestHandler<GetOrdineByIdQuery, OrdineDto?>
{
    public async Task<OrdineDto?> Handle(GetOrdineByIdQuery q, CancellationToken ct)
    {
        var o = await repo.GetByIdAsync(q.OrdineId, ct);
        return o is null ? null : OrdineDto.Da(o);
    }
}

// ── GetPropostaAttivaOperatore ─────────────────────────────────────────────────

public sealed record GetPropostaAttivaQuery(Guid OperatoreId) : IRequest<PropostaDto?>;

public sealed class GetPropostaAttivaHandler(IPropostaOrdineRepository repo)
    : IRequestHandler<GetPropostaAttivaQuery, PropostaDto?>
{
    public async Task<PropostaDto?> Handle(GetPropostaAttivaQuery q, CancellationToken ct)
    {
        var p = await repo.GetByOperatoreAttivaAsync(q.OperatoreId, ct);
        return p is null ? null : PropostaDto.Da(p);
    }
}

// ── GetOrdiniByPeriodo ─────────────────────────────────────────────────────────

public sealed record GetOrdiniByPeriodoQuery(
    DateTime   Dal,
    DateTime   Al,
    StatoOrdine? Stato = null) : IRequest<IReadOnlyList<OrdineDto>>;

public sealed class GetOrdiniByPeriodoHandler(IOrdineRepository repo)
    : IRequestHandler<GetOrdiniByPeriodoQuery, IReadOnlyList<OrdineDto>>
{
    public async Task<IReadOnlyList<OrdineDto>> Handle(
        GetOrdiniByPeriodoQuery q, CancellationToken ct)
    {
        var ordini = q.Stato.HasValue
            ? await repo.GetByStatoAsync(q.Stato.Value, ct)
            : await repo.GetByPeriodoAsync(q.Dal, q.Al, ct);

        return ordini
            .Where(o => o.DataEmissione >= q.Dal && o.DataEmissione <= q.Al)
            .Select(OrdineDto.Da)
            .OrderByDescending(o => o.DataEmissione)
            .ToList();
    }
}

// ── GetRiepilogoProposta ───────────────────────────────────────────────────────

public sealed record GetRiepilogoPropostaQuery(Guid PropostaId) : IRequest<RiepilogoPropostaDto?>;

public sealed class GetRiepilogoPropostaHandler(IPropostaOrdineRepository repo)
    : IRequestHandler<GetRiepilogoPropostaQuery, RiepilogoPropostaDto?>
{
    public async Task<RiepilogoPropostaDto?> Handle(GetRiepilogoPropostaQuery q, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(q.PropostaId, ct);
        return p is null ? null : RiepilogoPropostaDto.Da(p);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  DTO
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record OrdineDto(
    Guid         Id,
    string       Numero,
    string       StatoNome,
    Guid         FornitoreId,
    string       RagioneSociale,
    string       TipoFornitore,
    DateTime     DataEmissione,
    DateTime?    DataTrasmissione,
    DateTime?    DataRicezione,
    int          NumeroRighe,
    int          TotalePezzi,
    decimal      ImportoTotale,
    string?      Note,
    string?      NomeEmissione)
{
    internal static OrdineDto Da(Ordine o) => new(
        o.Id, o.Numero.Valore, o.Stato.ToString(),
        o.FornitoreId, o.RagioneSociale, o.TipoFornitore.ToString(),
        o.DataEmissione, o.DataTrasmissione, o.DataRicezione,
        o.NumeroRighe, o.TotalePezzi, o.ImportoTotale,
        o.Note, o.NomeEmissione);
}

public sealed record RigaOrdineDto(
    Guid    ProdottoId,
    string  CodiceFarmaco,
    string  Descrizione,
    int     Quantita,
    int     QuantitaOmaggio,
    decimal CostoUnitario,
    decimal Sconto,
    int     AliquotaIVA,
    decimal CostoTotale,
    int     QuantitaArrivata)
{
    internal static RigaOrdineDto Da(RigaOrdine r) => new(
        r.ProdottoId, r.CodiceFarmaco.Valore, r.Descrizione,
        r.Quantita, r.QuantitaOmaggio,
        r.Costo.Imponibile, r.Costo.Sconto, r.AliquotaIVA,
        r.CostoTotale, r.QuantitaArrivata);
}

public sealed record PropostaDto(
    Guid     Id,
    string   Stato,
    Guid     OperatoreId,
    string   NomeEmissione,
    int      NumeroProdotti,
    int      NumeroConFornitore,
    DateTime DataCreazione)
{
    internal static PropostaDto Da(PropostaOrdine p) => new(
        p.Id, p.Stato.ToString(), p.OperatoreId, p.NomeEmissione,
        p.NumeroProdottiInProposta, p.NumeroProdottiConFornitore,
        p.DataCreazione);
}

public sealed record RiepilogoPropostaDto(
    Guid   PropostaId,
    string NomeEmissione,
    int    NumeroProdottiEsaminati,
    int    NumeroProdottiInOrdine,
    double DurataSecondi,
    string Stato,
    IReadOnlyList<RigaPropostaDto> Righe)
{
    internal static RiepilogoPropostaDto Da(PropostaOrdine p) => new(
        p.Id,
        p.Riepilogo.NomeEmissione,
        (int)p.Riepilogo.NumeroProdottiEsaminati,
        (int)p.Riepilogo.NumeroProdottiInOrdine,
        p.Riepilogo.DurataSecondi,
        p.Stato.ToString(),
        p.Righe.Select(RigaPropostaDto.Da).ToList());
}

public sealed record RigaPropostaDto(
    Guid    ProdottoId,
    string  CodiceFarmaco,
    string  Descrizione,
    int     QuantitaTotale,
    int     QuantitaMancante,
    int     QuantitaNecessita,
    int     QuantitaPrenotata,
    int     QuantitaSospesa,
    int     GiacenzaEsposizione,
    int     GiacenzaMagazzino,
    decimal PrezzoListino,
    string  FornitoreAssegnato,
    decimal CostoUnitario,
    decimal Sconto,
    decimal Margine,
    decimal IndiceVenditaTendenziale,
    bool    IsSegnalato,
    bool    IsInOrdine)
{
    internal static RigaPropostaDto Da(PropostaRiga r)
    {
        // Trova il primo fornitore abilitato per visualizzazione
        var (costo, nomeForn) = (CostoFornitore.Zero, string.Empty);
        for (var i = 1; i <= PropostaRiga.MaxFornitori; i++)
        {
            if (!r.IsFornitoreAbilitato(i)) continue;
            costo    = r.CostoPerFornitore(i);
            nomeForn = $"Fornitore {i}";
            break;
        }

        return new RigaPropostaDto(
            r.ProdottoId, r.CodiceFarmaco.Valore, r.Descrizione,
            r.QuantitaTotale, r.QuantitaMancante, r.QuantitaNecessita,
            r.QuantitaPrenotata, r.QuantitaSospesa,
            r.GiacenzaEsposizione, r.GiacenzaMagazzino,
            r.PrezzoListino,
            nomeForn, costo.Imponibile, costo.Sconto, costo.Margine,
            r.IndiceVenditaTendenziale,
            r.IsSegnalato, r.IsInOrdine);
    }
}
