using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  PROPOSTA ORDINE — Aggregate Root
//
//  Migrazione della sessione di composizione ordine in EmissioneOrdine.cls.
//
//  Nel VB6 la sessione era:
//    - La classe clsEmissioneOrdine stessa (stato interno)
//    - Le tabelle SQL ValutazioneOrdine1/2 (area di lavoro)
//    - Il campo cpOrdineSessione per tenere traccia della sessione
//
//  Qui la PropostaOrdine è un aggregate che contiene:
//    - La lista di PropostaRiga (ex ValutazioneOrdine2)
//    - I parametri dell'emissione (ex tEmissione, tIndiceDiVendita, tRipristinoScorta)
//    - Il RiepilogoElaborazione
//    - Lo stato della proposta (Draft → Completata → Emessa)
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Sessione di composizione di un ordine.
/// Contiene i prodotti valutati durante la pipeline di emissione
/// e governa le regole di aggiunta/rimozione manuale.
/// </summary>
public sealed class PropostaOrdine : AggregateRoot
{
    // ── Stato ────────────────────────────────────────────────────────────────

    public enum StatoProposta
    {
        Bozza      = 0,   // elaborazione in corso
        Completata = 1,   // pipeline terminata, in attesa di conferma
        Emessa     = 2,   // ordine generato, proposta chiusa
        Annullata  = 3
    }

    public StatoProposta Stato { get; private set; } = StatoProposta.Bozza;

    // ── Metadata ──────────────────────────────────────────────────────────────

    public Guid                   OperatoreId       { get; private set; }
    public Guid                   ConfigurazioneId  { get; private set; }  // cpEmissione
    public string                 NomeEmissione     { get; private set; } = string.Empty;
    public DateTime               DataCreazione     { get; private set; }
    public bool                   Interrotta        { get; private set; }

    // ── Fornitori dell'emissione ──────────────────────────────────────────────

    private readonly List<InfoFornitore> _fornitori = [];
    public IReadOnlyList<InfoFornitore> Fornitori => _fornitori.AsReadOnly();

    // ── Righe (ex ValutazioneOrdine2) ─────────────────────────────────────────

    private readonly List<PropostaRiga> _righe = [];
    public IReadOnlyList<PropostaRiga>  Righe => _righe.AsReadOnly();

    // ── Parametri pipeline ────────────────────────────────────────────────────

    public bool                      IsEmissioneDaArchivio  { get; private set; }
    public bool                      IsEmissioneNecessita   { get; private set; }
    public bool                      IsEmissionePrenotati   { get; private set; }
    public bool                      IsEmissioneSospesi     { get; private set; }
    public ParametriIndiceDiVendita  IndiceDiVendita        { get; private set; } = ParametriIndiceDiVendita.Vuoto;
    public ParametriRipristinoScorta RipristinoScorta       { get; private set; } = ParametriRipristinoScorta.ScortaMinimaEsposizione;
    public bool                      IsRicalcolaNecessita   { get; private set; }
    public bool                      IsOrdineLiberoDitta    { get; private set; }
    public bool                      IsOrdineGenericoDitta  { get; private set; }
    public bool                      IsMagazzinoPresente    { get; private set; }
    public int                       IndiceMagazzino        { get; private set; }  // 1-based

    // ── Riepilogo elaborazione ─────────────────────────────────────────────────

    public RiepilogoElaborazione Riepilogo { get; private set; } = RiepilogoElaborazione.Vuoto;

    // ── Costruzione ───────────────────────────────────────────────────────────

    private PropostaOrdine() { }

    public static PropostaOrdine Crea(
        Guid   operatoreId,
        Guid   configurazioneId,
        string nomeEmissione)
    {
        Guard.AgainstEmptyGuid(operatoreId, nameof(operatoreId));
        Guard.AgainstEmptyGuid(configurazioneId, nameof(configurazioneId));

        var p = new PropostaOrdine
        {
            OperatoreId      = operatoreId,
            ConfigurazioneId = configurazioneId,
            NomeEmissione    = nomeEmissione ?? string.Empty,
            DataCreazione    = DateTime.UtcNow,
        };

        p.IsOrdineGenericoDitta = nomeEmissione == "Ordine generico a Ditta";
        p.Raise(new PropostaOrdineCreata(p.Id, operatoreId, configurazioneId, nomeEmissione));
        return p;
    }

    // ── Configurazione pipeline ────────────────────────────────────────────────

    public void ImpostaFonti(bool archivio, bool necessita, bool prenotati, bool sospesi)
    {
        Guard.AgainstFalse(Stato == StatoProposta.Bozza, "ImpostaFonti", "Proposta non in bozza.");
        IsEmissioneDaArchivio = archivio;
        IsEmissioneNecessita  = necessita;
        IsEmissionePrenotati  = prenotati;
        IsEmissioneSospesi    = sospesi;
    }

    public void AggiungiFornitori(IEnumerable<InfoFornitore> fornitori)
    {
        Guard.AgainstFalse(Stato == StatoProposta.Bozza, "AggiungiFornitori", "Proposta non in bozza.");
        _fornitori.Clear();
        _fornitori.AddRange(fornitori);

        // Individua il magazzino interno (se presente)
        IndiceMagazzino    = 0;
        IsMagazzinoPresente = false;
        for (var i = 0; i < _fornitori.Count; i++)
        {
            if (_fornitori[i].IsMagazzino)
            {
                IsMagazzinoPresente = true;
                IndiceMagazzino     = i + 1;  // 1-based
                break;
            }
        }
    }

    public void ImpostaIndiceDiVendita(ParametriIndiceDiVendita parametri)
        => IndiceDiVendita = parametri;

    public void ImpostaRipristinoScorta(ParametriRipristinoScorta parametri)
        => RipristinoScorta = parametri;

    public void ImpostaFlagOrdineLiberoDitta(bool valore)
        => IsOrdineLiberoDitta = valore;

    public void ImpostaRicalcolaNecessita(bool valore)
        => IsRicalcolaNecessita = valore;

    // ── Gestione righe (usata dalla pipeline) ──────────────────────────────────

    internal PropostaRiga OttieniOCreaRiga(Guid prodottoId, CodiceProdotto codice, string descrizione)
    {
        var esistente = _righe.FirstOrDefault(r => r.ProdottoId == prodottoId);
        if (esistente is not null) return esistente;

        var nuova = PropostaRiga.Crea(Id, prodottoId, codice, descrizione);
        _righe.Add(nuova);
        return nuova;
    }

    internal PropostaRiga? TrovaRiga(Guid prodottoId)
        => _righe.FirstOrDefault(r => r.ProdottoId == prodottoId);

    internal void RimuoviRighe(IEnumerable<Guid> prodottoIds)
    {
        foreach (var pid in prodottoIds)
            _righe.RemoveAll(r => r.ProdottoId == pid);
    }

    internal void AzzeraRighe() => _righe.Clear();

    internal void ImpostaRiepilogo(RiepilogoElaborazione riepilogo)
        => Riepilogo = riepilogo;

    // ── Aggiunta manuale prodotto ──────────────────────────────────────────────

    /// <summary>
    /// Aggiunge o aggiorna un prodotto nella proposta su richiesta esplicita dell'operatore.
    /// Migrazione di AggiungiProdottoInProposta in EmissioneOrdine.cls.
    /// </summary>
    public PropostaRiga AggiungiProdottoManuale(
        Guid          prodottoId,
        CodiceProdotto codice,
        string        descrizione,
        int           quantita,
        FonteAggiunta fonte,
        int           indiceFornitore1Based = 1)
    {
        Guard.AgainstFalse(Stato == StatoProposta.Bozza || Stato == StatoProposta.Completata,
            "AggiungiProdotto", "Impossibile aggiungere prodotti a una proposta emessa o annullata.");
        Guard.AgainstNonPositive(quantita, nameof(quantita));

        var riga = TrovaRiga(prodottoId);

        if (riga is null)
        {
            riga = OttieniOCreaRiga(prodottoId, codice, descrizione);
            riga.AbilitaFornitore(indiceFornitore1Based, true);
            riga.ImpostaQuantitaFornitore(indiceFornitore1Based, quantita);
            riga.AggiungiQuantita(fonte, quantita);
            riga.ImpostaPrioritaOrdinamento(PrioritaOrdinamentoRiga.Aggiunto);
        }
        else if (riga.DaEliminare)
        {
            // Ripristino prodotto precedentemente rimosso
            riga.ImpostaDaEliminare(false);
            riga.ImpostaDaOrdinare(true);
            riga.ImpostaPrioritaOrdinamento(PrioritaOrdinamentoRiga.Aggiunto);
        }
        else
        {
            // Incremento quantità su riga esistente
            riga.AggiungiQuantita(fonte, quantita);
            if (indiceFornitore1Based > 0)
                riga.IncrementaQuantitaFornitore(indiceFornitore1Based, quantita);
            else
                // Distribuisce sulla prima fonte abilitata
                for (var i = 1; i <= PropostaRiga.MaxFornitori; i++)
                    if (riga.IsFornitoreAbilitato(i)) { riga.IncrementaQuantitaFornitore(i, quantita); break; }
        }

        Raise(new ProdottoAggiuntoProposta(Id, prodottoId, codice, fonte, quantita));
        return riga;
    }

    /// <summary>
    /// Rimuove un prodotto dalla proposta (marca DaEliminare).
    /// Il prodotto viene fisicamente tolto solo quando si emette l'ordine.
    /// </summary>
    public void EliminaProdotto(Guid prodottoId)
    {
        var riga = TrovaRiga(prodottoId)
            ?? throw new EntityNotFoundException(nameof(PropostaRiga), nameof(prodottoId), prodottoId);
        riga.ImpostaDaEliminare(true);
        Raise(new ProdottoRimossoProposta(Id, prodottoId, riga.CodiceFarmaco));
    }

    // ── Transizioni di stato ───────────────────────────────────────────────────

    internal void MarcaCompletata(RiepilogoElaborazione riepilogo)
    {
        Guard.AgainstFalse(Stato == StatoProposta.Bozza, "MarcaCompletata",
            "Solo una proposta in bozza può essere completata.");
        Stato = StatoProposta.Completata;
        ImpostaRiepilogo(riepilogo);
        Raise(new PropostaOrdineCompletata(Id, OperatoreId, riepilogo));
    }

    internal void MarcaEmessa(Guid ordineId)
    {
        Guard.AgainstFalse(Stato == StatoProposta.Completata, "MarcaEmessa",
            "Solo una proposta completata può essere emessa.");
        Stato = StatoProposta.Emessa;
        Raise(new PropostaOrdineEmessa(Id, ordineId, OperatoreId));
    }

    public void Annulla()
    {
        Guard.AgainstStates(Stato,
            [StatoProposta.Emessa, StatoProposta.Annullata],
            "Annulla", "Impossibile annullare una proposta già emessa o già annullata.");
        Stato = StatoProposta.Annullata;
        Raise(new PropostaOrdineAnnullata(Id, OperatoreId));
    }

    public void Interrompi()
    {
        Interrotta = true;
        Raise(new ElaborazioneInterrotta(Id, OperatoreId));
    }

    // ── Statistiche ───────────────────────────────────────────────────────────

    public int NumeroProdottiInProposta => _righe.Count(r => !r.DaEliminare && r.DaOrdinare);
    public int NumeroProdottiConFornitore => _righe.Count(r => r.HaFornitoriAbilitati);

    /// <summary>Righe valide da emettere (non eliminate, con quantità > 0).</summary>
    public IEnumerable<PropostaRiga> RigheDaEmettere =>
        _righe.Where(r => r.DaOrdinare && !r.DaEliminare && r.TotaleQuantita > 0);
}
