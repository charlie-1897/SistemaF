using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  PROPOSTA RIGA — Entity (figlia di PropostaOrdine)
//
//  Migrazione della tabella ValutazioneOrdine2 in EmissioneOrdine.cls.
//
//  Nel VB6 questa era una tabella SQL temporanea con ~80 colonne,
//  una riga per prodotto, usata come area di lavoro dell'emissione.
//  Ogni UPDATE/SELECT su ValutazioneOrdine2 diventa qui un metodo
//  o una proprietà calcolata.
//
//  Vengono supportati fino a 5 fornitori (F1..F5, QuantitaFornitore1..5)
//  esattamente come nel VB6.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Una riga della proposta ordine: un prodotto con le quantità richieste
/// suddivise per fonte e per fornitore.
/// </summary>
public sealed class PropostaRiga : Entity
{
    public const int MaxFornitori = 5;

    // ── Identificatori ────────────────────────────────────────────────────────

    public Guid             PropostaId   { get; private set; }
    public Guid             ProdottoId   { get; private set; }
    public CodiceProdotto   CodiceFarmaco { get; private set; } = null!;
    public string           Descrizione  { get; private set; } = string.Empty;

    // ── Quantità per fonte (ex campi QuantitaX in ValutazioneOrdine2) ────────

    /// <summary>Quantità da mancanti (eFonteAggiunta.TFA_Mancanti).</summary>
    public int QuantitaMancante  { get; private set; }

    /// <summary>Quantità da necessità/fabbisogno (eFonteAggiunta.TFA_Necessita).</summary>
    public int QuantitaNecessita { get; private set; }

    /// <summary>Quantità prenotata (eFonteAggiunta.TFA_Prenotati).</summary>
    public int QuantitaPrenotata { get; private set; }

    /// <summary>Quantità sospesa (eFonteAggiunta.TFA_Sospesi).</summary>
    public int QuantitaSospesa   { get; private set; }

    /// <summary>Quantità da archivio storico (TT_Archivio).</summary>
    public int QuantitaArchivio  { get; private set; }

    /// <summary>Quantità totale richiesta (somma di tutte le fonti).</summary>
    public int QuantitaTotale    => QuantitaMancante + QuantitaNecessita
                                  + QuantitaPrenotata + QuantitaSospesa
                                  + QuantitaArchivio;

    // ── Assegnazione fornitori (ex F1..F5 + QuantitaFornitore1..5) ────────────

    /// <summary>
    /// Indica se il fornitore i-esimo (0-based) è abilitato a ricevere la riga.
    /// Corrisponde ai flag F1..F5 in ValutazioneOrdine2.
    /// </summary>
    private readonly bool[]    _fornitoreAbilitato = new bool[MaxFornitori];

    /// <summary>Quantità assegnata al fornitore i-esimo (0-based).</summary>
    private readonly int[]     _quantitaFornitore  = new int[MaxFornitori];

    /// <summary>Quantità omaggio assegnata al fornitore i-esimo (0-based).</summary>
    private readonly int[]     _quantitaOmaggio    = new int[MaxFornitori];

    /// <summary>Costo/sconto verso il fornitore i-esimo (0-based).</summary>
    private readonly CostoFornitore[] _costoFornitore = Enumerable
        .Repeat(CostoFornitore.Zero, MaxFornitori).ToArray();

    // Accessori indicizzati (1-based come nel VB6)
    public bool         IsFornitoreAbilitato(int i) => _fornitoreAbilitato[i - 1];
    public int          QuantitaPerFornitore(int i)  => _quantitaFornitore[i - 1];
    public int          OmaggioPerFornitore(int i)   => _quantitaOmaggio[i - 1];
    public CostoFornitore CostoPerFornitore(int i)   => _costoFornitore[i - 1];

    /// <summary>Totale pezzi assegnati a tutti i fornitori (TotaleQuantita).</summary>
    public int TotaleQuantita => _quantitaFornitore.Sum();

    /// <summary>
    /// Differenza tra quantità ordinata e totale richiesta (QuantitaSottoOrdinata).
    /// Negativa = sotto-ordinato; positiva = sovra-ordinato.
    /// </summary>
    public int QuantitaSottoOrdinata => TotaleQuantita - QuantitaTotale;

    // ── Giacenze snapshot (al momento dell'emissione) ─────────────────────────

    public int GiacenzaEsposizione   { get; private set; }
    public int ScortaMinimaEsposizione { get; private set; }
    public int ScortaMassimaEsposizione { get; private set; }
    public int GiacenzaMagazzino     { get; private set; }
    public int ScortaMinimaMagazzino { get; private set; }
    public int ScortaMassimaMagazzino { get; private set; }
    public int GiacenzaTotale        => GiacenzaEsposizione + GiacenzaMagazzino;

    // ── Dati storici prezzi (da RecuperaAltreInformazioni1) ───────────────────

    public decimal PrezzoListino    { get; private set; }   // "Prezzo"
    public decimal PrezzoVendita    { get; private set; }   // PrezzoVendita
    public decimal PrezzoFarmacia   { get; private set; }   // PrezzoFarmacia
    public decimal PrezzoRiferimento { get; private set; }  // SSN
    public int     AliquotaIVA      { get; private set; }
    public string  Classe           { get; private set; } = string.Empty;

    // ── Indici di vendita (da RecuperaAltreInformazioni2) ────────────────────

    public decimal IndiceVenditaTendenziale    { get; private set; }
    public decimal IndiceVenditaMensile        { get; private set; }
    public decimal IndiceVenditaAnnuale        { get; private set; }
    public decimal IndiceVenditaPeriodo        { get; private set; }
    public decimal IndiceVenditaMediaAritmetica { get; private set; }

    // ── Flag classificazione ──────────────────────────────────────────────────

    public bool IsCongelato        { get; private set; }
    public bool IsVeterinario      { get; private set; }
    public bool IsStupefacente     { get; private set; }
    public bool IsSegnalato        { get; private set; }
    public bool IsTrattatoDitta    { get; private set; }  // acquistato da ditta ultimi 24 mesi
    public bool IsSistemaAutomatico { get; private set; } // gestito da magazzino automatico
    public bool IsOtcSop           { get; private set; }
    public bool IsGenerico         { get; private set; }  // cpeCodiceGruppo in {1031,1032}
    public bool IsInOrdine         { get; private set; }  // già in ordine precedente
    public bool IsPreferenziale    { get; private set; }
    public bool DaOrdinare         { get; private set; } = true;
    public bool DaEliminare        { get; private set; }
    public string SettoreInventario { get; private set; } = string.Empty;

    // ── Priorità ──────────────────────────────────────────────────────────────

    public PrioritaOrdinamentoRiga Priorita { get; private set; }

    // ── Fornitore preferenziale ───────────────────────────────────────────────

    public Guid?  FornitorePreferenzialeId { get; private set; }
    public long   CodiceFornitorePreferenziale { get; private set; }

    // ── Costruzione ───────────────────────────────────────────────────────────

    private PropostaRiga() { }

    public static PropostaRiga Crea(
        Guid          propostaId,
        Guid          prodottoId,
        CodiceProdotto codice,
        string        descrizione)
    {
        Guard.AgainstEmptyGuid(propostaId, nameof(propostaId));
        Guard.AgainstEmptyGuid(prodottoId, nameof(prodottoId));

        return new PropostaRiga
        {
            PropostaId   = propostaId,
            ProdottoId   = prodottoId,
            CodiceFarmaco = codice,
            Descrizione  = descrizione.ToUpperInvariant(),
        };
    }

    // ── Aggiornamento quantità (usato dalla pipeline) ─────────────────────────

    public void AggiungiQuantita(FonteAggiunta fonte, int quantita)
    {
        Guard.AgainstNonPositive(quantita, nameof(quantita));
        switch (fonte)
        {
            case FonteAggiunta.Mancanti:     QuantitaMancante  += quantita; break;
            case FonteAggiunta.Necessita:
            case FonteAggiunta.Parcheggiati: QuantitaNecessita += quantita; break;
            case FonteAggiunta.Prenotati:    QuantitaPrenotata += quantita; break;
            case FonteAggiunta.Sospesi:      QuantitaSospesa   += quantita; break;
            default: throw new DomainException($"Fonte aggiunta sconosciuta: {fonte}.");
        }
    }

    internal void ImpostaQuantitaArchivio(int quantita)
        => QuantitaArchivio = Math.Max(0, quantita);

    // ── Assegnazione fornitori ────────────────────────────────────────────────

    public void AbilitaFornitore(int indice1Based, bool valore)
    {
        ValidaIndice(indice1Based);
        _fornitoreAbilitato[indice1Based - 1] = valore;
    }

    public void ImpostaQuantitaFornitore(int indice1Based, int quantita)
    {
        ValidaIndice(indice1Based);
        _quantitaFornitore[indice1Based - 1] = Math.Max(0, quantita);
    }

    internal void IncrementaQuantitaFornitore(int indice1Based, int delta)
    {
        ValidaIndice(indice1Based);
        _quantitaFornitore[indice1Based - 1] = Math.Max(0, _quantitaFornitore[indice1Based - 1] + delta);
    }

    public void ImpostaOmaggioFornitore(int indice1Based, int quantita)
    {
        ValidaIndice(indice1Based);
        _quantitaOmaggio[indice1Based - 1] = Math.Max(0, quantita);
    }

    public void ImpostaCostoFornitore(int indice1Based, CostoFornitore costo)
    {
        ValidaIndice(indice1Based);
        _costoFornitore[indice1Based - 1] = costo;
    }

    internal void AzzeraAssegnazioniTuttiFornitori()
    {
        for (var i = 0; i < MaxFornitori; i++)
        {
            _fornitoreAbilitato[i] = false;
            _quantitaFornitore[i]  = 0;
        }
    }

    public void AbilitaSoloFornitore(int indice1Based)
    {
        for (var i = 1; i <= MaxFornitori; i++)
            AbilitaFornitore(i, i == indice1Based);
    }

    /// <summary>True se almeno un fornitore è abilitato.</summary>
    public bool HaFornitoriAbilitati => _fornitoreAbilitato.Any(f => f);

    /// <summary>Numero di fornitori con quantità > 0.</summary>
    public int NumeroFornitoriConQuantita =>
        Enumerable.Range(0, MaxFornitori).Count(i => _quantitaFornitore[i] > 0);

    // ── Snapshot giacenze ─────────────────────────────────────────────────────

    internal void ImpostaGiacenze(
        int gExp, int scMinExp, int scMaxExp,
        int gMag, int scMinMag, int scMaxMag)
    {
        GiacenzaEsposizione     = gExp;
        ScortaMinimaEsposizione = scMinExp;
        ScortaMassimaEsposizione = scMaxExp;
        GiacenzaMagazzino       = gMag;
        ScortaMinimaMagazzino   = scMinMag;
        ScortaMassimaMagazzino  = scMaxMag;
    }

    // ── Prezzi e classificazione ──────────────────────────────────────────────

    public void ImpostaPrezzi(decimal listino, decimal vendita, decimal farmacia,
        decimal riferimento, int iva, string classe)
    {
        PrezzoListino     = listino;
        PrezzoVendita     = vendita;
        PrezzoFarmacia    = farmacia;
        PrezzoRiferimento = riferimento;
        AliquotaIVA       = iva;
        Classe            = classe ?? string.Empty;
    }

    internal void ImpostaIndiciVendita(decimal tend, decimal mens, decimal ann,
        decimal periodo, decimal media)
    {
        IndiceVenditaTendenziale     = tend;
        IndiceVenditaMensile         = mens;
        IndiceVenditaAnnuale         = ann;
        IndiceVenditaPeriodo         = periodo;
        IndiceVenditaMediaAritmetica = media;
    }

    internal void ImpostaClassificazione(
        bool congelato, bool veterinario, bool stupefacente, bool segnalato,
        bool trattatoDitta, bool sistemaAuto, bool otcSop, bool generico,
        bool inOrdine, bool preferenziale, string settore)
    {
        IsCongelato         = congelato;
        IsVeterinario       = veterinario;
        IsStupefacente      = stupefacente;
        IsSegnalato         = segnalato;
        IsTrattatoDitta     = trattatoDitta;
        IsSistemaAutomatico = sistemaAuto;
        IsOtcSop            = otcSop;
        IsGenerico          = generico;
        IsInOrdine          = inOrdine;
        IsPreferenziale     = preferenziale;
        SettoreInventario   = settore ?? string.Empty;
    }

    internal void ImpostaPrioritaOrdinamento(PrioritaOrdinamentoRiga priorita)
        => Priorita = priorita;

    internal void ImpostaFornitorePreferenziale(Guid? id, long codice)
    {
        FornitorePreferenzialeId     = id;
        CodiceFornitorePreferenziale = codice;
    }

    internal void ImpostaDaOrdinare(bool valore) => DaOrdinare = valore;
    internal void ImpostaDaEliminare(bool valore)
    {
        DaEliminare = valore;
        if (valore) DaOrdinare = false;
    }

    // ── Helper ────────────────────────────────────────────────────────────────


    private static void ValidaIndice(int i)
    {
        if (i < 1 || i > MaxFornitori)
            throw new DomainException($"Indice fornitore {i} fuori range [1..{MaxFornitori}].");
    }




    /// <summary>Calcola il costo totale delle righe nette (quantità - omaggio) × costo.</summary>
    public decimal CalcolaCostoTotale()
    {
        var totale = 0m;
        for (var i = 0; i < MaxFornitori; i++)
        {
            if (!_fornitoreAbilitato[i]) continue;
            var qtaNetta = _quantitaFornitore[i] - _quantitaOmaggio[i];
            totale += qtaNetta * _costoFornitore[i].Imponibile;
        }
        return totale;
    }
}
