using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Domain.Entities.Ricerca;

// ═══════════════════════════════════════════════════════════════════════════════
//  MODULO RICERCA PRODOTTO — Domain Layer
//
//  Migrazione di CSFRicerca/ClasseRicerca + frmRicerca.frm
//
//  Nel VB6 la ricerca avveniva così:
//    1. L'utente digitava nella textbox della frmRicerca
//    2. CSFRicerca decideva il tipo di ricerca in base al formato della stringa:
//       - 9 cifre esatte → CSFMINISTERIALE (Cod39) = 1
//       - 8 o 13 cifre EAN valide → CSFCODICEEAN = 15
//       - inizia con lettera o ha spazi → CSFDESCRIZIONE = 4
//       - formato "ATC/..." → CSFATC = 8
//    3. Costruiva una query SQL su ProdBase/ProdEsteso
//    4. Restituiva i risultati in un DataGrid
//
//  In C# le strategie diventano un enum e una funzione di autodetect.
//  La query SQL diventa una LINQ query su EF Core.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Tipo di ricerca prodotto.
/// Migrazione diretta delle costanti CSF* in CSFDichiarazioni.bas:
///   CSFMINISTERIALE=1, CSFDESCRIZIONE=4, CSFGRUPPO=5, CSFDITTA=6,
///   CSFATC=8, CSFCATEGORIARICETTA=9, CSFCODICEEAN=15, CSFCODGTIN=27
/// </summary>
public enum TipoRicercaProdotto
{
    /// <summary>Ricerca per codice ministeriale a 9 cifre (Cod39 / MINSAN).</summary>
    CodiceMinistriale = 1,

    /// <summary>Ricerca per patologia/indicazione terapeutica.</summary>
    Patologia = 2,

    /// <summary>Ricerca per descrizione (LIKE 'testo%' su ProdBase.Descrizione).</summary>
    Descrizione = 4,

    /// <summary>Ricerca per gruppo farmaceutico / principio attivo generico.</summary>
    GruppoFarmaceutico = 5,

    /// <summary>Ricerca per codice ditta produttrice.</summary>
    Ditta = 6,

    /// <summary>Ricerca per codice ATC (es. "N02BE01" = paracetamolo).</summary>
    CodiceATC = 8,

    /// <summary>Ricerca per categoria ricetta (A, C, OTC, SOP...).</summary>
    CategoriaRicetta = 9,

    /// <summary>Ricerca per codice EAN-8 o EAN-13.</summary>
    CodiceEAN = 15,

    /// <summary>Ricerca per codice GTIN (EAN-14, GS1).</summary>
    CodiceGTIN = 27,
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Criterio di ricerca prodotto.
/// Contiene la stringa di ricerca e il tipo (rilevato automaticamente o
/// impostato esplicitamente).
/// </summary>
public sealed class CriterioRicerca
{
    public string             Termine          { get; }
    public TipoRicercaProdotto Tipo            { get; }
    public bool               RicercaEstesa    { get; }   // wildcard implicito
    public string?            SettoreInventario { get; }  // null = tutti
    public int                MaxRisultati     { get; }

    private CriterioRicerca(
        string              termine,
        TipoRicercaProdotto tipo,
        bool                ricercaEstesa,
        string?             settore,
        int                 maxRisultati)
    {
        Termine           = termine;
        Tipo              = tipo;
        RicercaEstesa     = ricercaEstesa;
        SettoreInventario = settore;
        MaxRisultati      = maxRisultati;
    }

    /// <summary>
    /// Crea un criterio rilevando automaticamente il tipo dalla stringa,
    /// esattamente come faceva CSFRicerca.AvviaRicerca nel VB6.
    /// </summary>
    public static CriterioRicerca Rileva(
        string  termine,
        bool    ricercaEstesa    = false,
        string? settore          = null,
        int     maxRisultati     = 50)
    {
        Guard.AgainstNullOrEmpty(termine, nameof(termine));
        termine = termine.Trim();

        var tipo = RilevaAutomatico(termine);
        return new CriterioRicerca(termine, tipo, ricercaEstesa, settore,
            Math.Clamp(maxRisultati, 1, 500));
    }

    /// <summary>Crea un criterio con tipo esplicito (es. ricerca ATC forzata).</summary>
    public static CriterioRicerca Con(
        string              termine,
        TipoRicercaProdotto tipo,
        bool                ricercaEstesa = false,
        string?             settore       = null,
        int                 maxRisultati  = 50)
    {
        Guard.AgainstNullOrEmpty(termine, nameof(termine));
        return new CriterioRicerca(termine.Trim(), tipo, ricercaEstesa, settore,
            Math.Clamp(maxRisultati, 1, 500));
    }

    // ── Autodetect del tipo (logica VB6 migrata) ──────────────────────────────

    private static TipoRicercaProdotto RilevaAutomatico(string s)
    {
        // EAN-8 o EAN-13: solo cifre, lunghezza esatta
        if (s.All(char.IsDigit))
        {
            if (s.Length == 9)  return TipoRicercaProdotto.CodiceMinistriale;
            if (s.Length == 8 || s.Length == 13) return TipoRicercaProdotto.CodiceEAN;
            if (s.Length == 14) return TipoRicercaProdotto.CodiceGTIN;
        }

        // Codice "A" + 9 cifre (formato scanner some lettori): "A012345678"
        if (s.Length == 10 && s[0] == 'A' && s[1..].All(char.IsDigit))
            return TipoRicercaProdotto.CodiceMinistriale;

        // ATC: lettera + 2 cifre (es. "N02", "A10BA02")
        if (s.Length >= 3 && char.IsLetter(s[0])
            && char.IsDigit(s[1]) && char.IsDigit(s[2]))
            return TipoRicercaProdotto.CodiceATC;

        // Default: ricerca per descrizione
        return TipoRicercaProdotto.Descrizione;
    }

    public string TerminePerLike =>
        RicercaEstesa
            ? "%" + Termine.Replace(" ", "%") + "%"
            : Termine + "%";
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Risultato di una ricerca prodotto.
/// Corrisponde ai campi mostrati nella griglia della frmRicerca nel VB6:
/// Descrizione, Cod39, Classe, Prezzo, Giacenza.
/// </summary>
public sealed record RisultatoRicerca(
    Guid          ProdottoId,
    string        CodiceFarmaco,      // Cod39 / MINSAN
    string?       CodiceEAN,
    string?       CodiceATC,
    string        Descrizione,
    string        Classe,             // A / C / OTC / SOP
    string        CategoriaRicetta,   // RicettaRipetibile / NessunObbligo / ...
    decimal       PrezzoVendita,
    int           AliquotaIVA,
    int           GiacenzaEsposizione,
    int           GiacenzaMagazzino,
    int           GiacenzaTotale,
    bool          IsStupefacente,
    bool          IsVeterinario,
    bool          IsCongelato,
    bool          IsAttivo)
{
    public bool DisponibileInEsposizione => GiacenzaEsposizione > 0;
    public bool DisponibileInMagazzino  => GiacenzaMagazzino > 0;
    public bool IsDisponibile           => GiacenzaTotale > 0;
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Interfaccia del servizio di ricerca prodotto.
/// Separa la logica di ricerca (Domain) dall'implementazione SQL (Infrastructure).
/// </summary>
public interface IRicercaProdottoService
{
    /// <summary>
    /// Esegue la ricerca con il criterio specificato.
    /// Migrazione del metodo AvviaRicerca in clsRicerca.
    /// </summary>
    Task<IReadOnlyList<RisultatoRicerca>> CercaAsync(
        CriterioRicerca   criterio,
        CancellationToken ct = default);

    /// <summary>
    /// Ricerca rapida per codice esatto (ministeriale o EAN).
    /// Usata quando lo scanner legge un barcode: risposta istantanea.
    /// Migrazione di AvviaRicerca con IsNoForm=true nel VB6.
    /// </summary>
    Task<RisultatoRicerca?> TrovaEsattoAsync(
        string            codice,
        CancellationToken ct = default);
}
