using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  VALUE OBJECTS — Modulo Ordine
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Numero progressivo di un ordine emesso.
/// Nel VB6 era il campo cpOrdine (Long autoincrement in Access).
/// Qui usiamo un formato human-readable: YYYYNNNNN.
/// </summary>
public sealed class NumeroOrdine : SingleValueObject<string>
{
    private NumeroOrdine(string v) : base(v) { }

    public static NumeroOrdine Da(int anno, long progressivo)
    {
        Guard.AgainstOutOfRange(anno, 2000, 2099, nameof(anno));
        Guard.AgainstNonPositive(progressivo, nameof(progressivo));
        return new NumeroOrdine($"{anno}{progressivo:D5}");
    }

    public static NumeroOrdine Da(string valore)
    {
        Guard.AgainstNullOrEmpty(valore);
        if (valore.Length != 9 || !valore.All(char.IsDigit))
            throw new DomainException($"Numero ordine non valido: '{valore}'.", "NUMERO_ORDINE_INVALIDO");
        return new NumeroOrdine(valore);
    }

    public int  Anno        => int.Parse(Valore[..4]);
    public long Progressivo => long.Parse(Valore[4..]);
}

/// <summary>
/// Costo d'acquisto netto per un prodotto da un fornitore.
/// Migrazione dei campi CostoFornitore1..5 in ValutazioneOrdine2.
/// Tiene insieme costo imponibile, sconto e extra-sconto
/// così come vengono calcolati in VerificaAssegnazioni.
/// </summary>
public sealed class CostoFornitore : ValueObject
{
    public decimal  Imponibile   { get; }   // CostoFornitoreN — costo netto imponibile
    public decimal  Sconto       { get; }   // ScontoFornitoreN — % sconto primo livello
    public decimal  ExtraSconto  { get; }   // ExtraScontoFornitoreN — % sconto secondo livello
    public decimal  ScontoLordo  { get; }   // ScontoLordoFornitoreN — sconto composto
    public decimal  Margine      { get; }   // MargineFornitoreN — % margine sul vendita

    private CostoFornitore(decimal imp, decimal sc, decimal exSc, decimal scL, decimal mg)
    {
        Imponibile  = imp;
        Sconto      = sc;
        ExtraSconto = exSc;
        ScontoLordo = scL;
        Margine     = mg;
    }

    public static CostoFornitore Da(decimal imponibile, decimal sconto = 0m,
        decimal extraSconto = 0m, decimal scontoLordo = 0m, decimal margine = 0m)
    {
        Guard.AgainstNegative(imponibile, nameof(imponibile));
        Guard.AgainstOutOfRange(sconto,      0m, 100m, nameof(sconto));
        Guard.AgainstOutOfRange(extraSconto, 0m, 100m, nameof(extraSconto));
        return new CostoFornitore(imponibile, sconto, extraSconto, scontoLordo, margine);
    }

    public static CostoFornitore Zero => Da(0m);

    /// <summary>Costo reale dopo extra-sconto obiettivo fatturato (CostoRealeFornitoreN).</summary>
    public decimal CostoReale(decimal extraScontoObiettivoFatturato = 0m)
        => extraScontoObiettivoFatturato > 0
            ? Imponibile - Imponibile / 100m * extraScontoObiettivoFatturato
            : Imponibile;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Imponibile;
        yield return Sconto;
        yield return ExtraSconto;
    }
}

/// <summary>
/// Parametri per il calcolo della quantità tramite indici di vendita.
/// Migrazione di tIndiceDiVendita in EmissioneOrdine.cls.
/// </summary>
public sealed record ParametriIndiceDiVendita(
    TipoIndiceVendita Tipo,
    DateOnly?         DalPeriodo,
    DateOnly?         AlPeriodo,
    decimal           ValoreMinimo,
    decimal           ValoreMassimo,
    int               GiorniCopertura,
    bool              IsInclude,
    bool              IsSottraiGiacenze,
    bool              IsRicalcolaNecessita)
{
    public static ParametriIndiceDiVendita Vuoto => new(
        TipoIndiceVendita.Tendenziale, null, null, 0m, 0m, 0, true, false, false);
}

/// <summary>
/// Parametri per il ripristino scorte nella pipeline di emissione.
/// Migrazione di tRipristinoScorta in EmissioneOrdine.cls.
/// </summary>
public sealed record ParametriRipristinoScorta(
    TipoRipristinoScorta Tipo,
    bool                 IsConsideraProdottiEntroLeScorte,
    bool                 IsConsideraScortaDiSicurezza)
{
    public static ParametriRipristinoScorta ScortaMinimaEsposizione =>
        new(TipoRipristinoScorta.ScortaMinimaEsposizione, false, false);
}

/// <summary>
/// Riepilogo di un'elaborazione di emissione ordine.
/// Migrazione di tRiepilogoElaborazione in EmissioneOrdine.cls.
/// </summary>
public sealed record RiepilogoElaborazione(
    string   NomeEmissione,
    DateTime DataOraInizio,
    DateTime DataOraFine,
    long     NumeroProdottiGlobali,
    long     NumeroProdottiEsaminati,
    long     NumeroProdottiInOrdine)
{
    public double DurataSecondi => (DataOraFine - DataOraInizio).TotalSeconds;

    public static RiepilogoElaborazione Vuoto => new(
        string.Empty, DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);
}

/// <summary>
/// Informazioni fornitore nell'ambito di un ordine.
/// Migrazione di tFornitore in EmissioneOrdine.cls
/// e della Collection colFornitoriInfo in VerificaAssegnazioni.
/// </summary>
public sealed record InfoFornitore(
    Guid          FornitoreId,
    long          CodiceAnabase,         // cpAnabase
    TipoFornitore Tipo,                  // cpTipoAnagrafica
    bool          IsFornitoreGruppo,     // farmacia dello stesso gruppo
    int           PercentualeRipartizione, // Percentuale in EmissioniFornitori
    bool          IsMagazzino)           // è il magazzino interno
{
    public bool IsGrossista => Tipo == TipoFornitore.Grossista;
    public bool IsDitta     => Tipo == TipoFornitore.Ditta;
}
