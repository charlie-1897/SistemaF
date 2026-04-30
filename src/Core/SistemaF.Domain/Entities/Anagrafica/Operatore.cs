using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Domain.Entities.Anagrafica;

// ═══════════════════════════════════════════════════════════════════════════════
//  OPERATORE — Entity
//
//  Migrazione della tabella Operatore in WinSF.mdb.
//
//  Nel VB6 i campi principali erano:
//    CodiceOperatore (Long autoincrement)
//    NomeOperatore   (login — univoco)
//    NomeCognome     (nome visualizzato)
//    Password        (MD5 o plaintext legacy)
//    Autorizzazioni  (stringa di bit, es. "111000110...")
//    Badge           (codice badge opzionale)
//
//  Le autorizzazioni erano una stringa di 50 caratteri ('0'/'1')
//  dove ogni posizione corrispondeva a una funzione del sistema.
//  In C# usiamo un insieme di enum tipizzati più leggibili.
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class Operatore : Entity
{
    // ── Identità ──────────────────────────────────────────────────────────────

    /// <summary>Login univoco (NomeOperatore nel VB6).</summary>
    public string Login        { get; private set; } = string.Empty;

    /// <summary>Nome e cognome visualizzato (NomeCognome nel VB6).</summary>
    public string NomeCognome  { get; private set; } = string.Empty;

    /// <summary>Hash della password (PasswordHash, mai in chiaro).</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>Badge reader (campo Badge nel VB6).</summary>
    public string? Badge       { get; private set; }

    public bool    IsAttivo    { get; private set; } = true;
    public bool    IsAmministratore { get; private set; }

    /// <summary>
    /// Stringa autorizzazioni legacy VB6 (50 char '0'/'1').
    /// Mantenuta per compatibilità durante la migrazione.
    /// Verrà sostituita da AutorizzazioniModuli nella Wave 3.
    /// </summary>
    public string AutorizzazioniLegacy { get; private set; } = new('0', 50);

    // ── Costruzione ───────────────────────────────────────────────────────────

    private Operatore() { }

    public static Operatore Crea(
        string login,
        string nomeCognome,
        string passwordHash,
        bool   isAmministratore = false)
    {
        Guard.AgainstNullOrEmpty(login,       nameof(login));
        Guard.AgainstNullOrEmpty(nomeCognome, nameof(nomeCognome));
        Guard.AgainstNullOrEmpty(passwordHash, nameof(passwordHash));

        return new Operatore
        {
            Login             = login.Trim().ToLowerInvariant(),
            NomeCognome       = nomeCognome.Trim(),
            PasswordHash      = passwordHash,
            IsAmministratore  = isAmministratore,
        };
    }

    // ── Operazioni ────────────────────────────────────────────────────────────

    public void CambiaPassword(string nuovoHash)
    {
        Guard.AgainstNullOrEmpty(nuovoHash, nameof(nuovoHash));
        PasswordHash = nuovoHash;
    }

    public void ImpostaBadge(string? badge)
        => Badge = badge?.Trim();

    public void ImpostaAutorizzazioniLegacy(string autorizzazioni)
    {
        if (autorizzazioni.Length != 50)
            throw new DomainException(
                "La stringa autorizzazioni deve essere esattamente 50 caratteri.",
                "AUTORIZZAZIONI_INVALIDE");
        AutorizzazioniLegacy = autorizzazioni;
    }

    public bool HasAutorizzazione(int posizione0Based)
    {
        if (posizione0Based < 0 || posizione0Based >= 50) return false;
        return AutorizzazioniLegacy[posizione0Based] == '1';
    }

    public void Disattiva() => IsAttivo = false;
    public void Riattiva()  => IsAttivo = true;

    public bool VerificaPassword(string hash) =>
        PasswordHash.Equals(hash, StringComparison.Ordinal);
}

// ═══════════════════════════════════════════════════════════════════════════════
//  FARMACIA — Entity (singleton: una sola farmacia per installazione)
//
//  Migrazione dei parametri della farmacia da CParametri in CSFOrdCommon.
//  Nel VB6 i dati della farmacia erano nell'INI e in alcune tabelle DB.
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class Farmacia : Entity
{
    public string   Nome              { get; private set; } = string.Empty;
    public string?  RagioneSociale    { get; private set; }
    public string?  PartitaIVA        { get; private set; }
    public string?  CodiceFiscale     { get; private set; }
    public string?  CodiceFarmaciaAsl { get; private set; }  // per Federfarma DPC/WebCare
    public string?  CodiceAsl         { get; private set; }  // ASL di appartenenza
    public string?  NomeAsl           { get; private set; }  // per endpoint WebCare
    public string?  RegioneFarmacia   { get; private set; }
    public IndirizzoPosta  Sede       { get; private set; } = IndirizzoPosta.Vuoto;
    public ContattoTelefonico Contatti { get; private set; } = ContattoTelefonico.Vuoto;
    public string?  TitolareFarmacia  { get; private set; }
    public bool     HaMagazzino       { get; private set; }

    private Farmacia() { }

    public static Farmacia Crea(string nome, string? ragioneSociale = null)
    {
        Guard.AgainstNullOrEmpty(nome, nameof(nome));
        return new Farmacia
        {
            Nome           = nome.Trim(),
            RagioneSociale = ragioneSociale?.Trim(),
        };
    }

    public void AggiornaDati(
        string? ragioneSociale, string? partitaIva,
        string? codiceFiscale, string? titolare)
    {
        RagioneSociale  = ragioneSociale?.Trim();
        PartitaIVA      = partitaIva?.Trim();
        CodiceFiscale   = codiceFiscale?.Trim().ToUpperInvariant();
        TitolareFarmacia = titolare?.Trim();
    }

    public void ImpostaCodificheFederfarma(
        string? codiceAsl, string? codiceFarmaciaAsl, string? nomeAsl)
    {
        CodiceAsl         = codiceAsl?.Trim();
        CodiceFarmaciaAsl = codiceFarmaciaAsl?.Trim();
        NomeAsl           = nomeAsl?.Trim();
    }

    public void ImpostaSede(IndirizzoPosta sede, ContattoTelefonico contatti)
    {
        Sede     = sede;
        Contatti = contatti;
    }

    public void ImpostaHaMagazzino(bool valore)
        => HaMagazzino = valore;
}

// ═══════════════════════════════════════════════════════════════════════════════
//  CONFIGURAZIONE EMISSIONE — Entity
//
//  Migrazione di tParametriEmissione in EmissioneOrdine.cls / CSFOrdCommon.
//
//  Nel VB6 ogni "emissione" era un profilo salvato con nome
//  (es. "Ordine settimanale grossista", "Ordine mensile ditta X").
//  La ConfigurazioneId è la chiave che la PropostaOrdine porta con sé.
//
//  I parametri qui corrispondono ai campi del recordset EmissioniOrdini
//  nel database WinSF.
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class ConfigurazioneEmissione : Entity
{
    // ── Identità ──────────────────────────────────────────────────────────────

    public string  Nome          { get; private set; } = string.Empty;
    public string? Descrizione   { get; private set; }
    public bool    IsAttiva      { get; private set; } = true;

    // ── Fonti ordine (ex checkboxes nella UI VB6) ─────────────────────────────

    public bool    DaArchivio    { get; private set; } = true;
    public bool    DaNecessita   { get; private set; } = true;
    public bool    DaPrenotati   { get; private set; }
    public bool    DaSospesi     { get; private set; }

    // ── Tipo ordine ────────────────────────────────────────────────────────────

    /// <summary>
    /// True = ordine libero a ditta (senza vincoli di scorte).
    /// Corrisponde a cParametri.Emissione.OrdineLiberoDitta nel VB6.
    /// </summary>
    public bool    OrdineLiberoDitta  { get; private set; }

    /// <summary>Ricalcola la necessità anche durante la pipeline.</summary>
    public bool    RicalcolaNecessita { get; private set; }

    // ── Indice di vendita ─────────────────────────────────────────────────────

    public TipoIndiceVendita  TipoIndiceVendita  { get; private set; }
    public int                GiorniCopertura    { get; private set; }
    public decimal            IndiceVenditaMin   { get; private set; }
    public decimal            IndiceVenditaMax   { get; private set; }
    public bool               SottraiGiacenze    { get; private set; }

    // ── Ripristino scorte ──────────────────────────────────────────────────────

    public TipoRipristinoScorta TipoRipristinoScorta      { get; private set; }
    public bool                 ConsideraEntroLeScorte    { get; private set; }
    public bool                 ConsideraScortaDiSicurezza { get; private set; }

    // ── Ordinamento risultati ──────────────────────────────────────────────────

    /// <summary>
    /// Tipo di ordinamento prodotti nella griglia.
    /// Corrisponde a cParametri.Emissione.Ordinamento nel VB6 (0-4).
    /// </summary>
    public int     TipoOrdinamento   { get; private set; }

    // ── Costruzione ───────────────────────────────────────────────────────────

    private ConfigurazioneEmissione() { }

    public static ConfigurazioneEmissione Crea(
        string nome,
        string? descrizione = null)
    {
        Guard.AgainstNullOrEmpty(nome, nameof(nome));
        return new ConfigurazioneEmissione
        {
            Nome        = nome.Trim(),
            Descrizione = descrizione?.Trim(),
        };
    }

    // ── Profili predefiniti (factory methods) ─────────────────────────────────

    /// <summary>
    /// Configurazione standard per ordine settimanale al grossista.
    /// Il profilo più comune in farmacia.
    /// </summary>
    public static ConfigurazioneEmissione OrdineSettimanaleGrossista()
    {
        var c = Crea("Ordine settimanale grossista",
            "Ripristino scorte da archivio + necessità, grossista preferenziale");
        c.DaArchivio          = true;
        c.DaNecessita         = true;
        c.TipoIndiceVendita   = TipoIndiceVendita.Tendenziale;
        c.GiorniCopertura     = 7;
        c.TipoRipristinoScorta = TipoRipristinoScorta.ScortaMinimaEsposizione;
        return c;
    }

    /// <summary>Ordine mensile a ditta produttrice (senza vincoli scorte).</summary>
    public static ConfigurazioneEmissione OrdineMensileADitta()
    {
        var c = Crea("Ordine mensile a ditta",
            "Ordine libero, senza calcolo scorte, solo prodotti trattati dalla ditta");
        c.DaArchivio       = true;
        c.OrdineLiberoDitta = true;
        c.TipoIndiceVendita = TipoIndiceVendita.MesePrecedente;
        c.GiorniCopertura  = 30;
        return c;
    }

    // ── Aggiornamento ─────────────────────────────────────────────────────────

    public void ImpostaFonti(
        bool daArchivio, bool daNecessita,
        bool daPrenotati, bool daSospesi)
    {
        DaArchivio  = daArchivio;
        DaNecessita = daNecessita;
        DaPrenotati = daPrenotati;
        DaSospesi   = daSospesi;
    }

    public void ImpostaIndiceDiVendita(
        TipoIndiceVendita tipo, int giorniCopertura,
        decimal min = 0m, decimal max = 0m, bool sottraiGiacenze = false)
    {
        TipoIndiceVendita = tipo;
        GiorniCopertura   = Math.Max(0, giorniCopertura);
        IndiceVenditaMin  = Math.Max(0m, min);
        IndiceVenditaMax  = Math.Max(0m, max);
        SottraiGiacenze   = sottraiGiacenze;
    }

    public void ImpostaRipristinoScorte(
        TipoRipristinoScorta tipo,
        bool consideraEntro, bool consideraSicurezza)
    {
        TipoRipristinoScorta        = tipo;
        ConsideraEntroLeScorte      = consideraEntro;
        ConsideraScortaDiSicurezza  = consideraSicurezza;
    }

    public void ImpostaFlagOrdine(
        bool liberoDitta, bool ricalcolaNecessita)
    {
        OrdineLiberoDitta  = liberoDitta;
        RicalcolaNecessita = ricalcolaNecessita;
    }

    public void Disattiva() => IsAttiva = false;

    // ── Conversione a parametri pipeline ─────────────────────────────────────

    public ParametriIndiceDiVendita ToParametriIndice()
        => new(TipoIndiceVendita, null, null,
               IndiceVenditaMin, IndiceVenditaMax,
               GiorniCopertura, true, SottraiGiacenze, RicalcolaNecessita);

    public ParametriRipristinoScorta ToParametriRipristino()
        => new(TipoRipristinoScorta,
               ConsideraEntroLeScorte, ConsideraScortaDiSicurezza);
}

// ═══════════════════════════════════════════════════════════════════════════════
//  CONFIGURAZIONE EMISSIONE FORNITORE — Entity figlia
//
//  Associazione N:M tra ConfigurazioneEmissione e Fornitore.
//  Migrazione della tabella EmissioniFornitori nel VB6.
//  Determina quali fornitori partecipano a un'emissione e in che ordine.
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class ConfigurazioneEmissioneFornitore : Entity
{
    public Guid   ConfigurazioneId      { get; private set; }
    public Guid   FornitoreId           { get; private set; }

    /// <summary>Ordine di priorità nella pipeline (1 = primo).</summary>
    public int    OrdineIndice          { get; private set; }

    /// <summary>Percentuale di ripartizione ordini tra fornitori.</summary>
    public int    PercentualeRipartizione { get; private set; } = 100;

    public bool   IsAbilitato           { get; private set; } = true;

    private ConfigurazioneEmissioneFornitore() { }

    public static ConfigurazioneEmissioneFornitore Crea(
        Guid configurazioneId, Guid fornitoreId,
        int ordine = 1, int percentuale = 100)
    {
        Guard.AgainstEmptyGuid(configurazioneId, nameof(configurazioneId));
        Guard.AgainstEmptyGuid(fornitoreId,      nameof(fornitoreId));
        return new ConfigurazioneEmissioneFornitore
        {
            ConfigurazioneId      = configurazioneId,
            FornitoreId           = fornitoreId,
            OrdineIndice          = Math.Max(1, ordine),
            PercentualeRipartizione = Math.Clamp(percentuale, 0, 100),
        };
    }

    public void Disabilita() => IsAbilitato = false;
    public void Abilita()    => IsAbilitato = true;
}

// ═══════════════════════════════════════════════════════════════════════════════
//  INTERFACCE REPOSITORY
// ═══════════════════════════════════════════════════════════════════════════════

public interface IFornitoreRepository : IRepository<Fornitore>
{
    Task<Fornitore?> GetByCodiceAnabaseAsync(long codice, CancellationToken ct = default);
    Task<IReadOnlyList<Fornitore>> GetAttiviAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Fornitore>> GetByTipoAsync(TipoFornitore tipo, CancellationToken ct = default);
    Task<Fornitore?> GetMagazzinoInternoAsync(CancellationToken ct = default);
}

public interface IOperatoreRepository : IRepository<Operatore>
{
    Task<Operatore?> GetByLoginAsync(string login, CancellationToken ct = default);
    Task<Operatore?> GetByBadgeAsync(string badge, CancellationToken ct = default);
    Task<IReadOnlyList<Operatore>> GetAttiviAsync(CancellationToken ct = default);
}

public interface IConfigurazioneEmissioneRepository : IRepository<ConfigurazioneEmissione>
{
    Task<IReadOnlyList<ConfigurazioneEmissione>> GetAttiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Fornitore>> GetFornitoriAsync(
        Guid configurazioneId, CancellationToken ct = default);
}

public interface IFarmaciaRepository : IRepository<Farmacia>
{
    Task<Farmacia?> GetFarmaciaCorrente(CancellationToken ct = default);
}
