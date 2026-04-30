namespace SistemaF.Integration.Federfarma.WebCare.Models;

// ═══════════════════════════════════════════════════════════════════════════════
//  MODELLI DOMINIO — WebCare (FUR) Lombardia
//
//  Sorgente VB.NET: CSFWebCareLombardia.vb
//  WSDL: WebServiceFUR.wsdl
//
//  Gerarchia (migrazione 1:1 della struttura VB.NET + WSDL):
//    CompetenzaWebCare                  (top level)
//    ├─ RiepiloghiIva[]                 (riepiloghi IVA a livello competenza)
//    └─ Contabilizzazioni[]
//         ├─ RiepiloghiIva[]            (riepiloghi IVA a livello contabilizzazione)
//         └─ Documenti[]
//              ├─ RiepiloghiIva[]       (riepiloghi IVA a livello documento)
//              └─ Movimenti[]
//                   ├─ RiepiloghiIva[]  (riepiloghi IVA a livello movimento)
//                   └─ Righe[]          (righe prodotto del movimento)
//
//  Tutti i campi monetari nel WSDL sono decimal; nel VB.NET erano String.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Risposta alla chiamata GetCompetenza.
/// Migrazione della classe CSFWebCareLombardia + Competenza WSDL.
/// </summary>
public sealed class CompetenzaWebCare
{
    public string  CodiceAsl              { get; init; } = string.Empty;
    public string  CodiceFarmacia         { get; init; } = string.Empty;
    public string  RagioneSocialeFarmacia { get; init; } = string.Empty;
    public string  AnnoCompetenza         { get; init; } = string.Empty;
    public string  MeseCompetenza         { get; init; } = string.Empty;
    public int     NumeroMovimenti        { get; init; }
    public decimal ImportoNetto           { get; init; }
    public decimal ImportoIva             { get; init; }
    public decimal ImportoLordo           { get; init; }

    public IReadOnlyList<RiepilogoIvaWebCare>  RiepiloghiIva     { get; init; } = [];
    public IReadOnlyList<ContabilizzazioneWebCare> Contabilizzazioni { get; init; } = [];

    // ── Computed ──────────────────────────────────────────────────────────────

    public bool IsVuota => Contabilizzazioni.Count == 0;

    public DateOnly? Competenza =>
        int.TryParse(AnnoCompetenza, out var a) && int.TryParse(MeseCompetenza, out var m)
            ? new DateOnly(a, m, 1) : null;

    public int TotaleMovimenti => Contabilizzazioni.Sum(c => c.NumeroMovimenti);
}

/// <summary>
/// Riepilogo IVA per fascia di aliquota.
/// Usato a 4 livelli: Competenza, Contabilizzazione, Documento, Movimento.
/// Migrazione della classe RiepilogoIva in CSFWebCareLombardia.vb + WSDL.
/// </summary>
public sealed class RiepilogoIvaWebCare
{
    /// <summary>Categoria merceologica (es. "A", "B", "C").</summary>
    public string  Categoria       { get; init; } = string.Empty;
    public decimal AliquotaIva     { get; init; }
    public decimal Imponibile      { get; init; }
    public decimal ImportoIva      { get; init; }
    /// <summary>Importo parziale (lordo per aliquota).</summary>
    public decimal ImportoParziale { get; init; }
}

/// <summary>
/// Contabilizzazione mensile (corrisponde a una liquidazione ASL).
/// Migrazione della classe Contabilizzazione in CSFWebCareLombardia.vb + WSDL.
/// </summary>
public sealed class ContabilizzazioneWebCare
{
    public int     NumeroContabilizzazione { get; init; }
    public string  DataContabilizzazione   { get; init; } = string.Empty;
    public int     NumeroMovimenti         { get; init; }
    public decimal ImportoNetto            { get; init; }
    public decimal ImportoIva              { get; init; }
    public decimal ImportoLordo            { get; init; }

    public IReadOnlyList<RiepilogoIvaWebCare>  RiepiloghiIva { get; init; } = [];
    public IReadOnlyList<DocumentoWebCare>     Documenti     { get; init; } = [];
}

/// <summary>
/// Tipo documento DPC (migrazione dell'enum TipoDocumento dal WSDL).
/// </summary>
public enum TipoDocumentoDpc
{
    Fattura,             // "fattura"
    RiepilogoContabile   // "riepilogoContabile"
}

/// <summary>
/// Documento (fattura o riepilogo) all'interno di una contabilizzazione.
/// Migrazione della classe Documento in CSFWebCareLombardia.vb + WSDL.
/// </summary>
public sealed class DocumentoWebCare
{
    public TipoDocumentoDpc Tipo            { get; init; }
    public string           NumeroDocumento { get; init; } = string.Empty;
    public string           DataDocumento   { get; init; } = string.Empty;
    public int              NumeroMovimenti { get; init; }
    public decimal          ImportoNetto    { get; init; }
    public decimal          ImportoIva      { get; init; }
    public decimal          ImportoLordo    { get; init; }

    public IReadOnlyList<RiepilogoIvaWebCare> RiepiloghiIva { get; init; } = [];
    public IReadOnlyList<MovimentoWebCare>    Movimenti     { get; init; } = [];
}

/// <summary>
/// Singolo movimento (dispensazione) all'interno di un documento.
/// Migrazione della classe Movimento in CSFWebCareLombardia.vb + WSDL.
/// </summary>
public sealed class MovimentoWebCare
{
    public string  IdMovimento    { get; init; } = string.Empty;
    public string  Categoria      { get; init; } = string.Empty;
    public string  CodiceFiscale  { get; init; } = string.Empty;
    public string  DataNascita    { get; init; } = string.Empty;

    /// <summary>"M" o "F".</summary>
    public string  Sesso          { get; init; } = string.Empty;
    public string  DataMovimento  { get; init; } = string.Empty;
    public int     QtaTotale      { get; init; }
    public decimal ImportoNetto   { get; init; }
    public decimal ImportoIva     { get; init; }
    public decimal ImportoLordo   { get; init; }

    public IReadOnlyList<RiepilogoIvaWebCare>  RiepiloghiIva { get; init; } = [];
    public IReadOnlyList<RigaMovimentoWebCare> Righe         { get; init; } = [];
}

/// <summary>
/// Riga prodotto di un movimento.
/// Migrazione della classe RigaMovimento in CSFWebCareLombardia.vb + WSDL.
/// </summary>
public sealed class RigaMovimentoWebCare
{
    /// <summary>Codice parafarmaco (se applicabile).</summary>
    public string  CodiceParaf              { get; init; } = string.Empty;

    /// <summary>Codice nomenclatore tariffario.</summary>
    public string  CodiceNomenclatore       { get; init; } = string.Empty;

    public string  DescrizioneNomenclatore  { get; init; } = string.Empty;

    /// <summary>Codice EAN del prodotto.</summary>
    public string  CodiceEan                { get; init; } = string.Empty;

    public int     Quantita                 { get; init; }
    public decimal ImportoNetto             { get; init; }
    public decimal ImportoIva               { get; init; }
    public decimal ImportoLordo             { get; init; }
    public decimal AliquotaIva              { get; init; }

    /// <summary>Tipo riga (es. "farmaco", "dispositivo", "parafarmaco").</summary>
    public string  Tipo                     { get; init; } = string.Empty;
}
