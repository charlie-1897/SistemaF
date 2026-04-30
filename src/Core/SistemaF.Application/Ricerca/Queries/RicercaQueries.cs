using FluentValidation;
using MediatR;
using SistemaF.Domain.Entities.Ricerca;

namespace SistemaF.Application.Ricerca.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
//  QUERIES — Ricerca Prodotto
// ═══════════════════════════════════════════════════════════════════════════════

// ── Ricerca per stringa (autodetect tipo) ─────────────────────────────────────

/// <summary>
/// Ricerca prodotti dalla stringa digitata dall'utente.
/// Il tipo di ricerca (codice, EAN, descrizione, ATC) viene rilevato
/// automaticamente, esattamente come in CSFRicerca.AvviaRicerca.
/// </summary>
public sealed record CercaProdottiQuery(
    string  Termine,
    bool    RicercaEstesa    = false,
    string? SettoreInventario = null,
    int     MaxRisultati     = 50)
    : IRequest<IReadOnlyList<RisultatoRicerca>>;

public sealed class CercaProdottiValidator : AbstractValidator<CercaProdottiQuery>
{
    public CercaProdottiValidator()
    {
        RuleFor(q => q.Termine)
            .NotEmpty()
            .MinimumLength(2)
            .WithMessage("Digitare almeno 2 caratteri per avviare la ricerca.")
            .MaximumLength(100);
        RuleFor(q => q.MaxRisultati).InclusiveBetween(1, 500);
    }
}

public sealed class CercaProdottiHandler(IRicercaProdottoService ricerca)
    : IRequestHandler<CercaProdottiQuery, IReadOnlyList<RisultatoRicerca>>
{
    public Task<IReadOnlyList<RisultatoRicerca>> Handle(
        CercaProdottiQuery q, CancellationToken ct)
    {
        var criterio = CriterioRicerca.Rileva(
            q.Termine, q.RicercaEstesa, q.SettoreInventario, q.MaxRisultati);
        return ricerca.CercaAsync(criterio, ct);
    }
}

// ── Ricerca per tipo esplicito ────────────────────────────────────────────────

public sealed record CercaProdottiPerTipoQuery(
    string              Termine,
    TipoRicercaProdotto Tipo,
    bool                RicercaEstesa = false,
    int                 MaxRisultati  = 50)
    : IRequest<IReadOnlyList<RisultatoRicerca>>;

public sealed class CercaProdottiPerTipoHandler(IRicercaProdottoService ricerca)
    : IRequestHandler<CercaProdottiPerTipoQuery, IReadOnlyList<RisultatoRicerca>>
{
    public Task<IReadOnlyList<RisultatoRicerca>> Handle(
        CercaProdottiPerTipoQuery q, CancellationToken ct)
    {
        var criterio = CriterioRicerca.Con(
            q.Termine, q.Tipo, q.RicercaEstesa, maxRisultati: q.MaxRisultati);
        return ricerca.CercaAsync(criterio, ct);
    }
}

// ── Ricerca esatta da scanner barcode ─────────────────────────────────────────

/// <summary>
/// Ricerca istantanea per codice scanner (ministeriale o EAN).
/// Restituisce un solo prodotto o null se non trovato.
/// </summary>
public sealed record TrovaProdottoDaCodiceQuery(string Codice)
    : IRequest<RisultatoRicerca?>;

public sealed class TrovaProdottoDaCodiceValidator
    : AbstractValidator<TrovaProdottoDaCodiceQuery>
{
    public TrovaProdottoDaCodiceValidator()
    {
        RuleFor(q => q.Codice)
            .NotEmpty()
            .Matches(@"^[A-Za-z0-9]{6,14}$")
            .WithMessage("Codice non valido: deve essere tra 6 e 14 caratteri alfanumerici.");
    }
}

public sealed class TrovaProdottoDaCodiceHandler(IRicercaProdottoService ricerca)
    : IRequestHandler<TrovaProdottoDaCodiceQuery, RisultatoRicerca?>
{
    public Task<RisultatoRicerca?> Handle(
        TrovaProdottoDaCodiceQuery q, CancellationToken ct)
        => ricerca.TrovaEsattoAsync(q.Codice, ct);
}
