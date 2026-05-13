using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Prodotto;

// ═══════════════════════════════════════════════════════════════════════════════
//  PRODOTTO — Aggregate Root
//
//  Migrazione di:
//    GestionaleProdotto.cls  → anagrafica completa del prodotto
//    GiacenzaProdotto.cls    → gestione giacenze esposizione + magazzino
//    Rettifiche.cls          → registro audit delle modifiche (TipoCosa, TipoAzione)
//    clsRegMovimentiLotto    → tracciabilità lotti
//    clsProdScadenza         → scadenzario
//
//  Il VB6 separava Giacenza in due tabelle fisiche (Esposizione, AltreGiacenze).
//  Qui modelliamo i due magazzini come value object GiacenzaMagazzino,
//  con il Prodotto come aggregate root che li gestisce entrambi.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Prodotto farmaceutico — aggregate root del modulo magazzino.
/// Gestisce anagrafica, giacenze (esposizione + magazzino retro),
/// prezzi, scorte, lotti e scadenzario.
/// </summary>
public sealed class Prodotto : SoftDeletableAggregateRoot
{
    // ── Identificatori ───────────────────────────────────────────────────────

    /// <summary>Codice ministeriale a 9 cifre. Campo "Ministeriale" nel VB6.</summary>
    public CodiceProdotto CodiceFarmaco  { get; private set; } = null!;

    /// <summary>Barcode EAN-13. Ricerca tipo CSFCODICEEAN=15.</summary>
    public CodiceEAN?     CodiceEAN      { get; private set; }

    /// <summary>Codice ATC. Ricerca tipo CSFATC=8.</summary>
    public CodiceATC?     CodiceATC      { get; private set; }

    /// <summary>Codice interno ditta produttrice. Ricerca CSFDITTA=6.</summary>
    public string?        CodiceDitta    { get; private set; }

    /// <summary>Codice targatura (es. opsmatico). Campo Targatura in clsRegMovimentiLotto.</summary>
    public string?        Targatura      { get; private set; }

    // ── Descrizione ──────────────────────────────────────────────────────────

    public string  Descrizione          { get; private set; } = string.Empty;
    public string? FormaBiotica         { get; private set; }  // CSFFORMAFARMACEUTICA=24
    public string? Sostanza             { get; private set; }  // CSFSOSTANZA=3
    public string? Patologia            { get; private set; }  // CSFPATOLOGIA=2
    public string? Gruppo               { get; private set; }  // CSFGRUPPO=5
    public string? LineaDitta           { get; private set; }  // CSFLINEADITTA=21
    public string? Nomenclatore         { get; private set; }  // CSFNOMENCLATORE=25

    // ── Classificazione ──────────────────────────────────────────────────────

    public ClasseFarmaco    Classe          { get; private set; } = ClasseFarmaco.C;
    public CategoriaRicetta CategoriaRicetta { get; private set; } = CategoriaRicetta.NessunObbligo;
    public bool IsStupefacente  { get; private set; }   // CSFSTUPEFACENTI=18
    public bool IsVeterinario   { get; private set; }   // CSFVETERINARIO=14
    public bool IsCongelato     { get; private set; }   // CSFPRODOTTICONGELATI=17
    public bool IsTrattato      { get; private set; }   // CSFTRATTATI=19
    public bool IsIntegrativo   { get; private set; }   // ProdottoIntegrativo
    public bool IsPluriPrescrizione { get; private set; } // CSFPLURIPRESCRIZIONE=7

    // ── 20260513 ───────────────────────────────────────────────────────────────
    /// <summary>Alias di Disattiva — retrocompatibilità con i test.</summary>
    public void Elimina(Guid? operatoreId = null) => Disattiva(operatoreId);

    // ── Prezzi ───────────────────────────────────────────────────────────────

    /// <summary>Prezzo al pubblico (attualePubblico in VB6).</summary>
    public Prezzo  PrezzoVendita       { get; private set; } = null!;

    /// <summary>Prezzo di listino (attualeListino in VB6).</summary>
    public Prezzo? PrezzoListino       { get; private set; }

    /// <summary>Prezzo di riferimento SSN (CSFPREZZORIFERIMENTO=22).</summary>
    public Prezzo? PrezzoRiferimentoSSN { get; private set; }

    /// <summary>Prezzo farmacia (costo d'acquisto). TC_PrezzoFarmacia in Rettifiche.</summary>
    public Prezzo? PrezzoAcquisto      { get; private set; }

    // ── Giacenze (ex tabelle Esposizione + AltreGiacenze) ────────────────────

    /// <summary>
    /// Giacenza nel reparto esposizione (banco vendita).
    /// Ex tabella Esposizione: GiacenzaEsposizione, ScortaMinima, ScortaMassima.
    /// TipoMagazzino.TM_Esposizione = 0 in GiacenzaProdotto.cls.
    /// </summary>
    public GiacenzaMagazzino GiacenzaEsposizione { get; private set; } = GiacenzaMagazzino.Zero;

    /// <summary>
    /// Giacenza nel magazzino retro (scorta di riserva).
    /// Ex tabella AltreGiacenze dove cpeTipoAltreGiacenze = 'M'.
    /// TipoMagazzino.TM_Magazzino = 1 in GiacenzaProdotto.cls.
    /// </summary>
    public GiacenzaMagazzino GiacenzaMagazzino   { get; private set; } = GiacenzaMagazzino.Zero;

    /// <summary>Giacenza totale (esposizione + magazzino retro).</summary>
    public int GiacenzaTotale => GiacenzaEsposizione.Giacenza + GiacenzaMagazzino.Giacenza;

    /// <summary>
    /// True se la giacenza dell'esposizione è sotto la scorta minima.
    /// Genera l'evento SottoscortaRilevata.
    /// </summary>
    public bool IsSottoscorta =>
        GiacenzaEsposizione.ScortaMinima > 0 &&
        GiacenzaEsposizione.Giacenza < GiacenzaEsposizione.ScortaMinima;

    /// <summary>True se la gestione scorte automatica è attiva (GestioneScorteAutomatico in VB6).</summary>
    public bool IsGestioneScorteAutomatica { get; private set; }

    // ── Segnalazioni ─────────────────────────────────────────────────────────

    public bool IsSegnalato    { get; private set; }   // CSFSEGNALAZIONE=11
    public bool IsInvendibile  { get; private set; }   // TC_Giacenzainvendibile
    public int  GiacenzaInvendibile { get; private set; }

    // ── Lotti e scadenze ─────────────────────────────────────────────────────

    private readonly List<ScadenzaProdotto> _scadenze = [];
    public IReadOnlyList<ScadenzaProdotto> Scadenze => _scadenze.AsReadOnly();

    // ── Metadata ─────────────────────────────────────────────────────────────

    public bool     IsAttivo           { get; private set; } = true;
    public DateTime DataAggiornamento  { get; private set; } = DateTime.UtcNow;

    // ── Costruzione (factory) ────────────────────────────────────────────────

    private Prodotto() { }

    /// <summary>
    /// Crea un nuovo prodotto farmaceutico.
    /// Corrisponde all'AddNew sul recordset della tabella Prodotti nel VB6.
    /// </summary>
    public static Prodotto Crea(
        CodiceProdotto   codice,
        string           descrizione,
        ClasseFarmaco    classe,
        CategoriaRicetta categoriaRicetta,
        Prezzo           prezzoVendita,
        Guid?            operatoreId = null)
    {
        var p = new Prodotto(operatoreId)
        {
            CodiceFarmaco    = codice,
            Descrizione      = Guard.AgainstTooLong(Guard.AgainstNullOrEmpty(descrizione), 200).ToUpperInvariant(),
            Classe           = classe,
            CategoriaRicetta = categoriaRicetta,
            PrezzoVendita    = prezzoVendita,
        };

        p.Raise(new ProdottoCreato(p.Id, codice, descrizione, operatoreId));
        return p;
    }

    private Prodotto(Guid? operatoreId) : base(operatoreId) { }

    // ── Classificazione ──────────────────────────────────────────────────────

    public void ImpostaClassificazione(
        bool isStupefacente,
        bool isVeterinario,
        bool isCongelato,
        bool isTrattato,
        bool isPluriPrescrizione,
        bool isIntegrativo)
    {
        IsStupefacente    = isStupefacente;
        IsVeterinario     = isVeterinario;
        IsCongelato       = isCongelato;
        IsTrattato        = isTrattato;
        IsPluriPrescrizione = isPluriPrescrizione;
        IsIntegrativo     = isIntegrativo;
        AggiornaDato();
    }

    public void ImpostaCodici(CodiceEAN? ean, CodiceATC? atc, string? ditta, string? targatura)
    {
        CodiceEAN   = ean;
        CodiceATC   = atc;
        CodiceDitta = ditta;
        Targatura   = targatura;
        AggiornaDato();
    }

    public void ImpostaDescrizioni(
        string? formaBiotica, string? sostanza, string? patologia,
        string? gruppo, string? lineaDitta, string? nomenclatore)
    {
        FormaBiotica = formaBiotica;
        Sostanza     = sostanza;
        Patologia    = patologia;
        Gruppo       = gruppo;
        LineaDitta   = lineaDitta;
        Nomenclatore = nomenclatore;
        AggiornaDato();
    }

    // ── Prezzi ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggiorna il prezzo al pubblico.
    /// Corrisponde a TC_PrezzoPubblico in Rettifiche.cls.
    /// </summary>
    public void AggiornaPrezzo(Prezzo nuovoPrezzo, Guid? operatoreId = null)
    {
        Guard.AgainstFalse(IsAttivo, "AggiornaPrezzo", $"Il prodotto {CodiceFarmaco} non è attivo.");

        var vecchio = PrezzoVendita;
        PrezzoVendita = nuovoPrezzo;
        AggiornaDato(operatoreId);

        Raise(new PrezzoProdottoAggiornato(
            Id, CodiceFarmaco, vecchio, nuovoPrezzo,
            TipoAzioneRettifica.Modifica, TipoCosaRettifica.PrezzoPubblico,
            operatoreId));
    }

    public void AggiornaPrezzoListino(Prezzo prezzo, Guid? operatoreId = null)
    {
        var vecchio = PrezzoListino;
        PrezzoListino = prezzo;
        AggiornaDato(operatoreId);
        Raise(new PrezzoProdottoAggiornato(
            Id, CodiceFarmaco, vecchio, prezzo,
            TipoAzioneRettifica.Modifica, TipoCosaRettifica.PrezzoListino, operatoreId));
    }

    public void AggiornaPrezzoAcquisto(Prezzo prezzo, Guid? operatoreId = null)
    {
        PrezzoAcquisto = prezzo;
        AggiornaDato(operatoreId);
        Raise(new PrezzoProdottoAggiornato(
            Id, CodiceFarmaco, null, prezzo,
            TipoAzioneRettifica.Modifica, TipoCosaRettifica.Costo, operatoreId));
    }

    // ── Giacenze ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Applica una variazione alla giacenza del reparto esposizione.
    /// Migrazione di AssegnaGiacenza con TM_Esposizione.
    /// TipoModalitaAggiunta: Sostituzione, Aggiunta, Sottrazione.
    /// </summary>
    public void VariaGiacenzaEsposizione(
        ModalitaVariazioneGiacenza modalita,
        int valore,
        TipoModuloRettifica modulo,
        Guid? operatoreId = null)
    {
        Guard.AgainstFalse(IsAttivo, "VariaGiacenzaEsposizione",
            $"Il prodotto {CodiceFarmaco} non è attivo.");

        var vecchia = GiacenzaEsposizione;
        GiacenzaEsposizione = GiacenzaEsposizione.Applica(modalita, valore);
        AggiornaDato(operatoreId);

        var azione = vecchia.Giacenza == 0 && GiacenzaEsposizione.Giacenza > 0
            ? TipoAzioneRettifica.Aggiunta
            : TipoAzioneRettifica.Modifica;

        Raise(new GiacenzaVariata(
            Id, CodiceFarmaco,
            TipoDepositoMagazzino.Esposizione, modalita,
            vecchia.Giacenza, GiacenzaEsposizione.Giacenza,
            azione, TipoCosaRettifica.GiacenzaEsposizione,
            modulo, operatoreId));

        if (IsSottoscorta && vecchia.Giacenza >= vecchia.ScortaMinima)
            Raise(new SottoscortaRilevata(
                Id, CodiceFarmaco, Descrizione,
                GiacenzaEsposizione.Giacenza,
                GiacenzaEsposizione.ScortaMinima));
    }

    /// <summary>
    /// Applica una variazione alla giacenza del magazzino retro.
    /// Migrazione di AssegnaGiacenza con TM_Magazzino.
    /// </summary>
    public void VariaGiacenzaMagazzino(
        ModalitaVariazioneGiacenza modalita,
        int valore,
        TipoModuloRettifica modulo,
        Guid? operatoreId = null)
    {
        Guard.AgainstFalse(IsAttivo, "VariaGiacenzaMagazzino",
            $"Il prodotto {CodiceFarmaco} non è attivo.");

        var vecchia = GiacenzaMagazzino;
        GiacenzaMagazzino = GiacenzaMagazzino.Applica(modalita, valore);
        AggiornaDato(operatoreId);

        Raise(new GiacenzaVariata(
            Id, CodiceFarmaco,
            TipoDepositoMagazzino.Magazzino, modalita,
            vecchia.Giacenza, GiacenzaMagazzino.Giacenza,
            TipoAzioneRettifica.Modifica, TipoCosaRettifica.GiacenzaMagazzino,
            modulo, operatoreId));
    }

    /// <summary>
    /// Aggiorna scorte minima e massima dell'esposizione.
    /// Migrazione di AssegnaScorte con TM_Esposizione.
    /// </summary>
    public void ImpostaScorteEsposizione(int scortaMin, int scortaMax, Guid? operatoreId = null)
    {
        Guard.AgainstNegative(scortaMin, nameof(scortaMin));
        Guard.AgainstNegative(scortaMax, nameof(scortaMax));
        Guard.AgainstFalse(scortaMax >= scortaMin, "ScorteEsposizione",
            "La scorta massima deve essere >= scorta minima.");

        var vecchia = GiacenzaEsposizione;
        GiacenzaEsposizione = GiacenzaEsposizione.ConScorte(scortaMin, scortaMax);
        AggiornaDato(operatoreId);

        if (scortaMin != vecchia.ScortaMinima)
            Raise(new GiacenzaVariata(
                Id, CodiceFarmaco, TipoDepositoMagazzino.Esposizione,
                ModalitaVariazioneGiacenza.Sostituzione,
                vecchia.ScortaMinima, scortaMin,
                TipoAzioneRettifica.Modifica, TipoCosaRettifica.ScortaMinimaEsposizione,
                TipoModuloRettifica.Magazzino, operatoreId));
    }

    /// <summary>
    /// Aggiorna scorte minima e massima del magazzino retro.
    /// </summary>
    public void ImpostaScorteMagazzino(int scortaMin, int scortaMax, Guid? operatoreId = null)
    {
        Guard.AgainstNegative(scortaMin, nameof(scortaMin));
        Guard.AgainstNegative(scortaMax, nameof(scortaMax));
        Guard.AgainstFalse(scortaMax >= scortaMin, "ScorteMagazzino",
            "La scorta massima deve essere >= scorta minima.");

        GiacenzaMagazzino = GiacenzaMagazzino.ConScorte(scortaMin, scortaMax);
        AggiornaDato(operatoreId);
    }

    /// <summary>
    /// Azzera la giacenza esposizione a zero.
    /// Migrazione di frmAzzeraGiacenze.frm.
    /// </summary>
    public void AzzeraGiacenzaEsposizione(Guid? operatoreId = null)
        => VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 0,
            TipoModuloRettifica.Magazzino, operatoreId);

    public void AttivaGestioneScorteAutomatica(bool attiva, Guid? operatoreId = null)
    {
        // Migrazione di ImpostaFlagVariazioneScorte in GiacenzaProdotto.cls
        IsGestioneScorteAutomatica = attiva;
        AggiornaDato(operatoreId);
    }

    // ── Invendibili ──────────────────────────────────────────────────────────

    public void ImpostaInvendibile(bool invendibile, int giacenzaInvendibile = 0, Guid? operatoreId = null)
    {
        IsInvendibile       = invendibile;
        GiacenzaInvendibile = invendibile ? giacenzaInvendibile : 0;
        AggiornaDato(operatoreId);

        if (invendibile)
            Raise(new ProdottoMarcatoInvendibile(Id, CodiceFarmaco, giacenzaInvendibile, operatoreId));
    }

    // ── Segnalazioni ─────────────────────────────────────────────────────────

    public void ImpostaSegnalazione(bool segnalato, Guid? operatoreId = null)
    {
        IsSegnalato = segnalato;
        AggiornaDato(operatoreId);
    }

    // ── Scadenze ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggiunge un lotto con data di scadenza.
    /// Migrazione di clsProdScadenza + clsRegMovimentiLotto.
    /// </summary>
    public ScadenzaProdotto AggiungLotto(
        CodiceLotto lotto,
        DateOnly    dataScadenza,
        int         quantita,
        Guid?       operatoreId = null)
    {
        Guard.AgainstNonPositive(quantita, nameof(quantita));

        var esiste = _scadenze.FirstOrDefault(s => s.Lotto == lotto);
        if (esiste is not null)
        {
            esiste.AggiungiQuantita(quantita);
            return esiste;
        }

        var scadenza = ScadenzaProdotto.Crea(Id, lotto, dataScadenza, quantita);
        _scadenze.Add(scadenza);

        if (dataScadenza <= DateOnly.FromDateTime(DateTime.Today.AddDays(90)))
            Raise(new ProdottoInScadenza(Id, CodiceFarmaco, Descrizione, lotto, dataScadenza, quantita));

        return scadenza;
    }

    public void RimuoviLotto(CodiceLotto lotto)
    {
        var s = _scadenze.FirstOrDefault(x => x.Lotto == lotto)
            ?? throw new EntityNotFoundException(nameof(ScadenzaProdotto), nameof(lotto), lotto.Valore);
        _scadenze.Remove(s);
    }

    /// <summary>Scadenze entro N giorni, ordinate per data crescente.</summary>
    public IEnumerable<ScadenzaProdotto> ScadenzeEntro(int giorni)
        => _scadenze
            .Where(s => s.DataScadenza <= DateOnly.FromDateTime(DateTime.Today.AddDays(giorni)))
            .OrderBy(s => s.DataScadenza);

    // ── Ciclo di vita ────────────────────────────────────────────────────────

    public void Disattiva(Guid? operatoreId = null)
    {
        Guard.AgainstFalse(IsAttivo, "Disattiva", $"Il prodotto {CodiceFarmaco} è già inattivo.");
        IsAttivo = false;
        MarkAsDeleted(operatoreId);
        Raise(new ProdottoDisattivato(Id, CodiceFarmaco, operatoreId));
    }

    public void Riattiva(Guid? operatoreId = null)
    {
        IsAttivo = true;
        AggiornaDato(operatoreId);
    }

    // ── Computed ─────────────────────────────────────────────────────────────

    /// <summary>True se il prodotto è mutuabile SSN (classe A o H).</summary>
    public bool IsMutuabile => Classe.IsMutuabile;

    // ── Helper interni ────────────────────────────────────────────────────────

    private void AggiornaDato(Guid? operatoreId = null)
    {
        DataAggiornamento = DateTime.UtcNow;
        SetUpdated(operatoreId);
    }
}
