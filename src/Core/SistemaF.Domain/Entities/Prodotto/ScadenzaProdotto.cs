using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Prodotto;

// ═══════════════════════════════════════════════════════════════════════════════
//  SCADENZA PRODOTTO — Entity (child of Prodotto aggregate)
//
//  Migrazione di:
//    clsProdScadenza.cls  → anagrafica singola scadenza/lotto
//    clsProdScadenze.cls  → collezione di scadenze (qui: _scadenze in Prodotto)
//    clsRegMovimentiLotto.cls → dati del lotto (qui: inclusi nella ScadenzaProdotto)
//
//  Nel VB6 era una tabella separata "ScadenzeProdotti" con i campi:
//    cpProdotto, Lotto, DataScadenza, Quantita, Targatura
//  Qui è un'entità figlia nell'aggregate Prodotto.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Un lotto di un prodotto farmaceutico con la relativa data di scadenza
/// e la quantità ancora disponibile in magazzino.
/// </summary>
public sealed class ScadenzaProdotto : Entity
{
    public Guid       ProdottoId   { get; private set; }
    public CodiceLotto Lotto       { get; private set; } = null!;
    public DateOnly   DataScadenza { get; private set; }
    public int        Quantita     { get; private set; }

    // ── Stato della scadenza ──────────────────────────────────────────────────

    public bool IsScaduto       => DataScadenza < DateOnly.FromDateTime(DateTime.Today);
    public bool IsInScadenza    => !IsScaduto &&
                                   DataScadenza <= DateOnly.FromDateTime(DateTime.Today.AddDays(90));
    public int  GiorniAllaScadenza =>
        DataScadenza.ToDateTime(TimeOnly.MinValue).Subtract(DateTime.Today).Days;

    // ── Costruzione ───────────────────────────────────────────────────────────

    private ScadenzaProdotto() { }

    internal static ScadenzaProdotto Crea(
        Guid        prodottoId,
        CodiceLotto lotto,
        DateOnly    dataScadenza,
        int         quantita)
    {
        Guard.AgainstEmptyGuid(prodottoId, nameof(prodottoId));
        Guard.AgainstNonPositive(quantita, nameof(quantita));

        return new ScadenzaProdotto
        {
            ProdottoId   = prodottoId,
            Lotto        = lotto,
            DataScadenza = dataScadenza,
            Quantita     = quantita
        };
    }

    internal void AggiungiQuantita(int quantita)
    {
        Guard.AgainstNonPositive(quantita, nameof(quantita));
        Quantita += quantita;
    }

    /// <summary>Decrementa la quantità disponibile del lotto (es. dopo una vendita).</summary>
    internal void ConsumaQuantita(int quantita)
    {
        Guard.AgainstNonPositive(quantita, nameof(quantita));
        if (quantita > Quantita)
            throw new BusinessRuleViolationException("QuantitaLotto",
                $"Quantità richiesta ({quantita}) superiore a quella del lotto {Lotto} ({Quantita}).");
        Quantita -= quantita;
    }
}
