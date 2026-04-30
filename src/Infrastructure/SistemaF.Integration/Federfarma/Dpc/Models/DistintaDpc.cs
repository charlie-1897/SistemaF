namespace SistemaF.Integration.Federfarma.Dpc.Models;

// ═══════════════════════════════════════════════════════════════════════════════
//  MODELLI DOMINIO — DPC (Distribuzione Per Conto) Lombardia
//
//  Sorgente VB.NET: CSFWebDpcLombardia.vb
//
//  Struttura dati originale nel VB.NET:
//    - CSFWebDpcLombardia (11 campi Distinta)
//      └─ ColRicette (lista Ricetta, 16 campi)
//           └─ ColProdotti (lista Prodotto, 8 campi)
//
//  Il VB.NET usava stringhe per tutti i valori (inclusi importi e quantità)
//  per compatibilità COM con VB6. In C# usiamo tipi forti.
//
//  Due note sulla doppia lista originale:
//    - cRicette: lista di Ricetta, ognuna con i propri Prodotti (lista annidata)
//    - cProdotti: lista piatta di TUTTI i prodotti di tutte le ricette
//  Questa struttura è mantenuta in DistintaDpc per compatibilità con
//  il codice chiamante che usa entrambe le viste.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Distinta DPC per il mese di competenza.
/// Migrazione dei campi privati _Xxx in CSFWebDpcLombardia.
/// </summary>
public sealed class DistintaDpc
{
    // ── Dati di testata (11 campi, migrazione 1:1 dal VB.NET) ─────────────────

    /// <summary>Es. "03" — meseCompetenza</summary>
    public string MeseCompetenza                  { get; init; } = string.Empty;

    /// <summary>Es. "2024" — annoCompetenza</summary>
    public string AnnoCompetenza                  { get; init; } = string.Empty;

    public decimal TotalePrezzoPubblicoProdotti   { get; init; }
    public decimal ImportoCompensoNettoIva        { get; init; }
    public decimal ImportoTicketRiscossiLordoIva  { get; init; }
    public decimal ImportoTicketRiscossiNettoIva  { get; init; }
    public decimal TotaleImponibile               { get; init; }
    public decimal TotaleIva                      { get; init; }
    public decimal TotaleFatturato                { get; init; }

    /// <summary>Numero ricette nella distinta.</summary>
    public int NumeroRicette { get; init; }

    /// <summary>Numero totale prodotti su tutte le ricette.</summary>
    public int NumeroProdotti { get; init; }

    // ── Ricette con prodotti annidati (ex cRicette in VB.NET) ─────────────────

    /// <summary>
    /// Lista ricette, ognuna con la propria lista Prodotti.
    /// Equivale a RicetteDPC (cRicette) nel VB.NET.
    /// </summary>
    public IReadOnlyList<RicettaDpc> Ricette { get; init; } = [];

    // ── Vista piatta di tutti i prodotti (ex ProdottiDPC / cProdotti) ─────────

    /// <summary>
    /// Lista piatta di tutti i prodotti di tutte le ricette.
    /// Nel VB.NET questa era ProdottiDPC (cProdotti) — duplicata rispetto
    /// agli oggetti in Ricette[].Prodotti per facilitare le ricerche per codice.
    /// </summary>
    public IReadOnlyList<ProdottoDpc> TuttiProdotti { get; init; } = [];

    // ── Computed ──────────────────────────────────────────────────────────────

    public bool IsVuota => Ricette.Count == 0;

    public DateOnly? Competenza =>
        int.TryParse(AnnoCompetenza, out var a) && int.TryParse(MeseCompetenza, out var m)
            ? new DateOnly(a, m, 1) : null;
}

/// <summary>
/// Una ricetta della distinta DPC.
/// Migrazione della classe Ricetta (16 campi) in CSFWebDpcLombardia.vb.
/// </summary>
public sealed class RicettaDpc
{
    // ── 16 campi originali (tutti String nel VB.NET → tipi forti qui) ─────────

    public int    Progressivo    { get; init; }    // _Progressivo
    public string Iup            { get; init; } = string.Empty;  // Identificativo Unico Prescrizione
    public string AslAssistito   { get; init; } = string.Empty;
    public string CodiceRegionale { get; init; } = string.Empty;
    public string CodiceRicetta  { get; init; } = string.Empty;

    /// <summary>Data spedizione — formato stringa dal WS (es. "2024-03-15").</summary>
    public string DataSpedizione { get; init; } = string.Empty;

    /// <summary>Data firma del medico — formato stringa dal WS.</summary>
    public string DataMedico     { get; init; } = string.Empty;

    public decimal OnereFarmacia   { get; init; }
    public decimal OnereGrossista  { get; init; }
    public string  TipoEsenzione   { get; init; } = string.Empty;
    public decimal TicketRiscosso  { get; init; }
    public string  CodiceFiscale   { get; init; } = string.Empty;

    /// <summary>"S" = sì difforme, "" = regolare.</summary>
    public string  Difforme        { get; init; } = string.Empty;

    public string  DataNascita     { get; init; } = string.Empty;

    /// <summary>"M" = maschio, "F" = femmina.</summary>
    public string  Sesso           { get; init; } = string.Empty;

    // ── Prodotti della ricetta (lista annidata) ────────────────────────────────

    public IReadOnlyList<ProdottoDpc> Prodotti { get; init; } = [];

    // ── Helper ────────────────────────────────────────────────────────────────

    public bool IsDifforme => Difforme.Equals("S", StringComparison.OrdinalIgnoreCase);
    public decimal ImportoTotaleRicetta => Prodotti.Sum(p => p.PrezzoPubblico * p.Quantita);
}

/// <summary>
/// Un prodotto di una ricetta DPC.
/// Migrazione della classe Prodotto (8 campi) in CSFWebDpcLombardia.vb.
///
/// I 8 campi originali erano tutti String nel VB.NET per compatibilità COM.
/// </summary>
public sealed class ProdottoDpc
{
    // ── 8 campi originali ─────────────────────────────────────────────────────

    /// <summary>Numero progressivo della ricetta di appartenenza.</summary>
    public int     ProgressivoRicetta    { get; init; }  // _Progressivo

    /// <summary>Codice ministeriale MINSAN (9 cifre).</summary>
    public string  MinSan                { get; init; } = string.Empty;

    public int     Quantita              { get; init; }

    /// <summary>Prezzo al pubblico (IVA inclusa).</summary>
    public decimal PrezzoPubblico        { get; init; }

    /// <summary>Aliquota IVA in percentuale (es. 4, 10, 22).</summary>
    public decimal AliquotaIva           { get; init; }

    /// <summary>Prezzo d'acquisto netto IVA.</summary>
    public decimal PrezzoAcquisto        { get; init; }

    /// <summary>Percentuale di competenza farmacia sul prezzo di rimborso.</summary>
    public decimal PercentualeFarmacia   { get; init; }

    /// <summary>Percentuale di competenza grossista.</summary>
    public decimal PercentualeGrossista  { get; init; }

    // ── Calcolati ─────────────────────────────────────────────────────────────

    public decimal ImportoNetto         => Math.Round(PrezzoAcquisto * Quantita, 4);
    public decimal ImportoIva           => Math.Round(ImportoNetto * AliquotaIva / 100m, 4);
    public decimal ImportoLordo         => Math.Round(ImportoNetto + ImportoIva, 4);
    public decimal CompensaFarmacia     => Math.Round(PrezzoPubblico * PercentualeFarmacia / 100m, 4);
    public decimal CompensaGrossista    => Math.Round(PrezzoPubblico * PercentualeGrossista / 100m, 4);
}
