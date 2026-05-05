using MediatR;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Interfaces;
using SistemaF.Domain.Specifications;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Application.Prodotti.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
//  QUERIES — Modulo Prodotto
//  Lato lettura (CQRS): restituiscono DTO, non modificano lo stato.
// ═══════════════════════════════════════════════════════════════════════════════

// ── GetProdottoById ───────────────────────────────────────────────────────────

public sealed record GetProdottoByIdQuery(Guid Id) : IRequest<ProdottoDto?>;

public sealed class GetProdottoByIdHandler(IProdottoRepository repo)
    : IRequestHandler<GetProdottoByIdQuery, ProdottoDto?>
{
    public async Task<ProdottoDto?> Handle(GetProdottoByIdQuery q, CancellationToken ct)
    {
        var p = await repo.GetByIdAsync(q.Id, ct);
        return p is null ? null : ProdottoDto.Da(p);
    }
}

// ── GetProdottoByCodiceFarmaco ────────────────────────────────────────────────

public sealed record GetProdottoByCodiceFarmacoQuery(string CodiceFarmaco)
    : IRequest<ProdottoDto?>;

public sealed class GetProdottoByCodiceFarmacoHandler(IProdottoRepository repo)
    : IRequestHandler<GetProdottoByCodiceFarmacoQuery, ProdottoDto?>
{
    public async Task<ProdottoDto?> Handle(
        GetProdottoByCodiceFarmacoQuery q, CancellationToken ct)
    {
        var codice = CodiceProdotto.Da(q.CodiceFarmaco);
        var p = await repo.GetByCodiceFarmacoAsync(codice, ct);
        return p is null ? null : ProdottoDto.Da(p);
    }
}

// ── CercaProdotti ─────────────────────────────────────────────────────────────

public sealed record CercaProdottiQuery(
    string  Termine,
    int     Limit  = 50,
    int     Offset = 0) : IRequest<ProdottiRisultato>;

public sealed record ProdottiRisultato(
    IReadOnlyList<ProdottoDto> Prodotti,
    int TotaleRisultati);

public sealed class CercaProdottiHandler(IProdottoRepository repo)
    : IRequestHandler<CercaProdottiQuery, ProdottiRisultato>
{
    public async Task<ProdottiRisultato> Handle(CercaProdottiQuery q, CancellationToken ct)
    {
        var spec   = new ProdottoCercaSpec(q.Termine).And(new ProdottiAttiviSpec());
        var items  = await repo.CercaAsync(spec, q.Limit, q.Offset, ct);
        var totale = await repo.ContaAsync(spec, ct);
        return new ProdottiRisultato(items.Select(ProdottoDto.Da).ToList(), totale);
    }
}

// ── CercaProdottiPerTipo (sostituisce CSFMINISTERIALE, CSFATC ecc.) ───────────

public sealed record CercaProdottiPerTipoQuery(
    int    TipoRicerca,   // Id di TipoRicercaProdotto (CSFMINISTERIALE=1, CSFATC=8…)
    string Valore,
    int    Limit  = 50,
    int    Offset = 0) : IRequest<ProdottiRisultato>;

public sealed class CercaProdottiPerTipoHandler(IProdottoRepository repo)
    : IRequestHandler<CercaProdottiPerTipoQuery, ProdottiRisultato>
{
    public async Task<ProdottiRisultato> Handle(
        CercaProdottiPerTipoQuery q, CancellationToken ct)
    {
        var tipo = (TipoRicercaProdotto)q.TipoRicerca;
        var spec = new ProdottoRicercaTipoSpec(tipo, q.Valore).And(new ProdottiAttiviSpec());

        var items  = await repo.CercaAsync(spec, q.Limit, q.Offset, ct);
        var totale = await repo.ContaAsync(spec, ct);
        return new ProdottiRisultato(items.Select(ProdottoDto.Da).ToList(), totale);
    }
}

// ── GetSottoscorta ────────────────────────────────────────────────────────────

public sealed record GetSottoscortaQuery : IRequest<IReadOnlyList<ProdottoGiacenzaDto>>;

public sealed class GetSottoscortaHandler(IProdottoRepository repo)
    : IRequestHandler<GetSottoscortaQuery, IReadOnlyList<ProdottoGiacenzaDto>>
{
    public async Task<IReadOnlyList<ProdottoGiacenzaDto>> Handle(
        GetSottoscortaQuery q, CancellationToken ct)
    {
        var lista = await repo.GetSottoscortaAsync(ct);
        return lista.Select(ProdottoGiacenzaDto.Da).ToList();
    }
}

// ── GetInScadenza ─────────────────────────────────────────────────────────────

public sealed record GetInScadenzaQuery(int GiorniSoglia = 90)
    : IRequest<IReadOnlyList<ProdottoScadenzaDto>>;

public sealed class GetInScadenzaHandler(IProdottoRepository repo)
    : IRequestHandler<GetInScadenzaQuery, IReadOnlyList<ProdottoScadenzaDto>>
{
    public async Task<IReadOnlyList<ProdottoScadenzaDto>> Handle(
        GetInScadenzaQuery q, CancellationToken ct)
    {
        var lista = await repo.GetInScadenzaAsync(q.GiorniSoglia, ct);
        return lista
            .SelectMany(p => p.ScadenzeEntro(q.GiorniSoglia)
                .Select(s => ProdottoScadenzaDto.Da(p, s)))
            .OrderBy(s => s.DataScadenza)
            .ToList();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  DTO — proiezioni di sola lettura
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record ProdottoDto(
    Guid     Id,
    string   CodiceFarmaco,
    string?  CodiceEAN,
    string?  CodiceATC,
    string   Descrizione,
    string   Classe,
    bool     IsMutuabile,
    bool     IsStupefacente,
    bool     IsVeterinario,
    bool     IsCongelato,
    decimal  PrezzoVendita,
    int      AliquotaIVA,
    decimal? PrezzoSSN,
    int      GiacenzaEsposizione,
    int      ScortaMinimaEsposizione,
    int      GiacenzaMagazzino,
    bool     IsSottoscorta,
    bool     IsInvendibile,
    bool     IsSegnalato,
    string   CategoriaRicetta,
    DateTime DataAggiornamento)
{
    internal static ProdottoDto Da(Prodotto p) => new(
        p.Id,
        p.CodiceFarmaco.Valore,
        p.CodiceEAN?.Valore,
        p.CodiceATC?.Valore,
        p.Descrizione,
        p.Classe.CodiceBreve,
        p.IsMutuabile,
        p.IsStupefacente,
        p.IsVeterinario,
        p.IsCongelato,
        p.PrezzoVendita.Importo,
        p.PrezzoVendita.AliquotaIVA,
        p.PrezzoRiferimentoSSN?.Importo,
        p.GiacenzaEsposizione.Giacenza,
        p.GiacenzaEsposizione.ScortaMinima,
        p.GiacenzaMagazzino.Giacenza,
        p.IsSottoscorta,
        p.IsInvendibile,
        p.IsSegnalato,
        p.CategoriaRicetta.Nome,
        p.DataAggiornamento);
}

public sealed record ProdottoGiacenzaDto(
    Guid   Id,
    string CodiceFarmaco,
    string Descrizione,
    int    GiacenzaEsposizione,
    int    ScortaMinima,
    int    Mancanti)
{
    internal static ProdottoGiacenzaDto Da(Prodotto p) => new(
        p.Id,
        p.CodiceFarmaco.Valore,
        p.Descrizione,
        p.GiacenzaEsposizione.Giacenza,
        p.GiacenzaEsposizione.ScortaMinima,
        Math.Max(0, p.GiacenzaEsposizione.ScortaMinima - p.GiacenzaEsposizione.Giacenza));
}

public sealed record ProdottoScadenzaDto(
    Guid     ProdottoId,
    string   CodiceFarmaco,
    string   Descrizione,
    string   Lotto,
    DateOnly DataScadenza,
    int      GiorniAllaScadenza,
    int      Quantita,
    bool     IsScaduto)
{
    internal static ProdottoScadenzaDto Da(Prodotto p, ScadenzaProdotto s) => new(
        p.Id,
        p.CodiceFarmaco.Valore,
        p.Descrizione,
        s.Lotto.Valore,
        s.DataScadenza,
        s.GiorniAllaScadenza,
        s.Quantita,
        s.IsScaduto);
}
