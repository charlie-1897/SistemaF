using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Domain.Entities.Anagrafica;

// ═══════════════════════════════════════════════════════════════════════════════
//  FORNITORE — Aggregate Root
//
//  Migrazione di clsAnagraficaFornitore in CSFOrdCommon.
//
//  Nel VB6 l'anagrafica fornitore era letta dalla tabella AnagraficaFornitori
//  nel database WinSF (Access). Ogni fornitore aveva:
//    - cpAnabase (Long) → chiave primaria legacy
//    - CodiceTipoAnagrafica → "G"=Grossista, "D"=Ditta, "M"=Magazzino, "W"=Web
//    - RagioneSociale, PartitaIVA, CodiceFiscale
//    - Dati sede, deposito, contatto
//    - BudgetStimato (Currency VB6 → decimal C#)
//
//  In C# diventa un AggregateRoot con Value Objects per i dati di contatto.
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class Fornitore : AggregateRoot
{
    // ── Identificatori ────────────────────────────────────────────────────────

    /// <summary>
    /// Codice numerico legacy del VB6 (cpAnabase).
    /// Mantenuto per compatibilità con i dati storici degli ordini.
    /// </summary>
    public long CodiceAnabase { get; private set; }

    // ── Dati anagrafici principali ────────────────────────────────────────────

    public string        RagioneSociale  { get; private set; } = string.Empty;
    public TipoFornitore Tipo            { get; private set; }
    public string?       PartitaIVA      { get; private set; }
    public string?       CodiceFiscale   { get; private set; }
    public string?       Annotazione     { get; private set; }

    // ── Sede principale (ex tAnagraficaFornitore) ─────────────────────────────

    public IndirizzoPosta Sede           { get; private set; } = IndirizzoPosta.Vuoto;
    public ContattoTelefonico Contatti   { get; private set; } = ContattoTelefonico.Vuoto;

    // ── Deposito (ex tAnagraficaFornitoreDeposito) ────────────────────────────

    public string?           NomeDeposito    { get; private set; }
    public IndirizzoPosta?   IndirizzoDeposito { get; private set; }

    // ── Contatto referente (ex tAnagraficaFornitoreContatto) ─────────────────

    public string?              NomeContatto  { get; private set; }
    public ContattoTelefonico?  TelefonoContatto { get; private set; }
    public string?              EmailContatto { get; private set; }

    // ── Parametri commerciali ─────────────────────────────────────────────────

    /// <summary>Budget stimato annuo acquisti (BudgetStimato in VB6).</summary>
    public decimal BudgetStimato          { get; private set; }

    /// <summary>True se è il magazzino interno della farmacia.</summary>
    public bool    IsMagazzino            { get; private set; }

    /// <summary>True se è il fornitore preferenziale predefinito.</summary>
    public bool    IsPreferenzialeDefault { get; private set; }

    /// <summary>True se fa parte del gruppo della farmacia (stessa catena).</summary>
    public bool    IsFornitoreGruppo      { get; private set; }

    /// <summary>
    /// Percentuale di ripartizione ordini quando ci sono più fornitori
    /// con la stessa priorità (EmissioniFornitori.Percentuale nel VB6).
    /// </summary>
    public int     PercentualeRipartizione { get; private set; } = 100;

    public bool    IsAttivo               { get; private set; } = true;

    // ── Costruzione ───────────────────────────────────────────────────────────

    private Fornitore() { }

    public static Fornitore Crea(
        string        ragioneSociale,
        TipoFornitore tipo,
        string?       partitaIva      = null,
        string?       codiceFiscale   = null,
        bool          isMagazzino     = false)
    {
        Guard.AgainstNullOrEmpty(ragioneSociale, nameof(ragioneSociale));

        var f = new Fornitore
        {
            RagioneSociale = ragioneSociale.Trim().ToUpperInvariant(),
            Tipo           = tipo,
            PartitaIVA     = partitaIva?.Trim(),
            CodiceFiscale  = codiceFiscale?.Trim().ToUpperInvariant(),
            IsMagazzino    = isMagazzino,
        };

        f.Raise(new FornitoreCreato(f.Id, ragioneSociale, tipo));
        return f;
    }

    // ── Aggiornamento dati ────────────────────────────────────────────────────

    public void AggiornaDatiAnagrafici(
        string  ragioneSociale,
        string? partitaIva    = null,
        string? codiceFiscale = null,
        string? annotazione   = null)
    {
        Guard.AgainstNullOrEmpty(ragioneSociale, nameof(ragioneSociale));
        RagioneSociale = ragioneSociale.Trim().ToUpperInvariant();
        PartitaIVA     = partitaIva?.Trim();
        CodiceFiscale  = codiceFiscale?.Trim().ToUpperInvariant();
        Annotazione    = annotazione;
        Raise(new FornitoreDatiAggiornati(Id, RagioneSociale));
    }

    public void ImpostaSede(IndirizzoPosta sede, ContattoTelefonico contatti)
    {
        Sede     = sede;
        Contatti = contatti;
    }

    public void ImpostaCodiceAnabase(long codice)
    {
        Guard.AgainstNonPositive(codice, nameof(codice));
        CodiceAnabase = codice;
    }

    public void ImpostaParametriCommerciali(
        decimal budgetStimato,
        int     percentualeRipartizione = 100,
        bool    isPreferenziale         = false,
        bool    isGruppo                = false)
    {
        Guard.AgainstNegative(budgetStimato, nameof(budgetStimato));
        Guard.AgainstOutOfRange(percentualeRipartizione, 0, 100,
            nameof(percentualeRipartizione));
        BudgetStimato             = budgetStimato;
        PercentualeRipartizione   = percentualeRipartizione;
        IsPreferenzialeDefault    = isPreferenziale;
        IsFornitoreGruppo         = isGruppo;
    }

    public void Disattiva()
    {
        Guard.AgainstFalse(IsAttivo, "Disattiva", "Il fornitore è già disattivato.");
        IsAttivo = false;
        Raise(new FornitoreDisattivato(Id, RagioneSociale));
    }

    public void Riattiva() => IsAttivo = true;

    // ── Conversione a InfoFornitore (usato dalla pipeline ordini) ─────────────

    /// <summary>
    /// Crea l'InfoFornitore richiesto da PropostaOrdine.AggiungiFornitori.
    /// </summary>
    public InfoFornitore ToInfoFornitore()
        => new(Id, CodiceAnabase, Tipo,
               IsFornitoreGruppo, PercentualeRipartizione, IsMagazzino);
}

// ═══════════════════════════════════════════════════════════════════════════════
//  VALUE OBJECTS — Anagrafica
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Indirizzo postale (sede o deposito fornitore).
/// Migrazione dei campi Indirizzo/CAP/Localita/Provincia in tAnagraficaFornitore.
/// </summary>
public sealed class IndirizzoPosta : ValueObject
{
    public string Indirizzo { get; }
    public string Cap       { get; }
    public string Localita  { get; }
    public string Provincia { get; }

    private IndirizzoPosta(string indirizzo, string cap, string localita, string provincia)
    {
        Indirizzo = indirizzo;
        Cap       = cap;
        Localita  = localita;
        Provincia = provincia;
    }

    public static IndirizzoPosta Da(
        string indirizzo, string cap, string localita, string provincia)
        => new(
            indirizzo?.Trim() ?? string.Empty,
            cap?.Trim() ?? string.Empty,
            localita?.Trim() ?? string.Empty,
            (provincia?.Trim().ToUpperInvariant() ?? string.Empty)[..Math.Min(2,
                provincia?.Trim().Length ?? 0)]);

    public static IndirizzoPosta Vuoto
        => new(string.Empty, string.Empty, string.Empty, string.Empty);

    public override string ToString()
        => $"{Indirizzo}, {Cap} {Localita} ({Provincia})".Trim(' ', ',');

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Indirizzo.ToUpperInvariant();
        yield return Cap;
        yield return Localita.ToUpperInvariant();
        yield return Provincia;
    }
}

/// <summary>
/// Dati di contatto telefonico.
/// Migrazione dei campi NumeroTelefono/Fax/Email/Cellulare.
/// </summary>
public sealed class ContattoTelefonico : ValueObject
{
    public string? Telefono   { get; }
    public string? Fax        { get; }
    public string? Email      { get; }
    public string? Cellulare  { get; }
    public string? SitoWeb    { get; }

    private ContattoTelefonico(
        string? telefono, string? fax, string? email,
        string? cellulare, string? sitoWeb)
    {
        Telefono  = telefono;
        Fax       = fax;
        Email     = email;
        Cellulare = cellulare;
        SitoWeb   = sitoWeb;
    }

    public static ContattoTelefonico Da(
        string? telefono  = null, string? fax      = null,
        string? email     = null, string? cellulare = null,
        string? sitoWeb   = null)
        => new(telefono?.Trim(), fax?.Trim(), email?.Trim()?.ToLowerInvariant(),
               cellulare?.Trim(), sitoWeb?.Trim());

    public static ContattoTelefonico Vuoto => new(null, null, null, null, null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Telefono;
        yield return Email;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  DOMAIN EVENTS — Fornitore
// ═══════════════════════════════════════════════════════════════════════════════

public record FornitoreCreato(
    Guid          FornitoreId,
    string        RagioneSociale,
    TipoFornitore Tipo) : DomainEvent;

public record FornitoreDatiAggiornati(
    Guid   FornitoreId,
    string RagioneSociale) : DomainEvent;

public record FornitoreDisattivato(
    Guid   FornitoreId,
    string RagioneSociale) : DomainEvent;
