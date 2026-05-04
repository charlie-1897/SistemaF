using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  RIGA ORDINE — Entity figlia di Ordine
//
//  Migrazione della tabella DettaglioOrdine nel database WinSF.
//  Nel VB6: DettaglioOrdine.cpeProdotto, Quantita, Costo, Sconto, IVA,
//           QPrenotata, QSospesa, QMancante, QNecessita, QArchivio, ecc.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Singola riga di un ordine emesso.</summary>
public sealed class RigaOrdine : Entity
{
    public Guid             OrdineId        { get; private set; }
    public Guid             ProdottoId      { get; private set; }
    public CodiceProdotto   CodiceFarmaco   { get; private set; } = null!;
    public string           Descrizione     { get; private set; } = string.Empty;
    public int              Quantita        { get; private set; }
    public int              QuantitaOmaggio { get; private set; }
    public CostoFornitore   Costo           { get; private set; } = CostoFornitore.Zero;
    public decimal          PrezzoListino   { get; private set; }
    public int              AliquotaIVA     { get; private set; }

    // Quantità per fonte (da ValutazioneOrdine2 al momento dell'emissione)
    public int QuantitaMancante  { get; private set; }
    public int QuantitaNecessita { get; private set; }
    public int QuantitaPrenotata { get; private set; }
    public int QuantitaSospesa   { get; private set; }
    public int QuantitaArchivio  { get; private set; }

    // Stato ricevimento (aggiornato nel modulo Ricarico)
    public int QuantitaArrivata  { get; private set; }
    public int QuantitaAssicurata { get; private set; }  // QAssicurata: confermata dal fornitore
    public string? CodiceMancante { get; private set; }  // codice alternativo del mancante

    private RigaOrdine() { }

    internal static RigaOrdine DaProposta(
        Guid          ordineId,
        PropostaRiga  riga,
        int           indiceFornitore1Based)
    {
        return new RigaOrdine
        {
            OrdineId         = ordineId,
            ProdottoId       = riga.ProdottoId,
            CodiceFarmaco    = riga.CodiceFarmaco,
            Descrizione      = riga.Descrizione,
            Quantita         = riga.QuantitaPerFornitore(indiceFornitore1Based),
            QuantitaOmaggio  = riga.OmaggioPerFornitore(indiceFornitore1Based),
            Costo            = riga.CostoPerFornitore(indiceFornitore1Based),
            PrezzoListino    = riga.PrezzoListino,
            AliquotaIVA      = riga.AliquotaIVA,
            QuantitaMancante  = riga.QuantitaMancante,
            QuantitaNecessita = riga.QuantitaNecessita,
            QuantitaPrenotata = riga.QuantitaPrenotata,
            QuantitaSospesa   = riga.QuantitaSospesa,
            QuantitaArchivio  = riga.QuantitaArchivio,
        };
    }

    public decimal CostoTotale
        => (Quantita - QuantitaOmaggio) * Costo.Imponibile;

    internal void ImpostaRicevuto(int quantitaArrivata, int quantitaAssicurata, string? codiceMancante)
    {
        Guard.AgainstNegative(quantitaArrivata, nameof(quantitaArrivata));
        QuantitaArrivata   = quantitaArrivata;
        QuantitaAssicurata = quantitaAssicurata;
        CodiceMancante     = codiceMancante;
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  ORDINE — Aggregate Root
//
//  Migrazione della tabella Ordine nel database WinSF.
//  Nel VB6: Ordine.cpOrdine, Ordine.cpeAnabase, Ordine.Stato, Ordine.Data,
//           Ordine.DataEmissione, Ordine.Note, Ordine.TipoOrdine, ecc.
//
//  Le righe dell'ordine corrispondono a DettaglioOrdine.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Ordine farmaceutico confermato ed emesso verso un fornitore.
/// </summary>
public sealed class Ordine : SoftDeletableAggregateRoot
{
    // ── Identificatori ────────────────────────────────────────────────────────

    public NumeroOrdine Numero          { get; private set; } = null!;
    public Guid         PropostaId      { get; private set; }
    public Guid         FornitoreId     { get; private set; }
    public long         CodiceAnabase   { get; private set; }  // cpAnabase nel VB6
    public TipoFornitore TipoFornitore  { get; private set; }
    public string       RagioneSociale  { get; private set; } = string.Empty;

    // ── Stato e date ──────────────────────────────────────────────────────────

    public StatoOrdine Stato            { get; private set; } = StatoOrdine.Emesso;
    public DateTime    DataEmissione    { get; private set; }
    public DateTime?   DataTrasmissione { get; private set; }
    public DateTime?   DataRicezione    { get; private set; }
    public Guid        OperatoreId      { get; private set; }

    // ── Annotazioni ───────────────────────────────────────────────────────────

    public string? Note      { get; private set; }
    public string? NomeEmissione { get; private set; }  // dal PropostaOrdine

    // ── Righe ─────────────────────────────────────────────────────────────────

    private readonly List<RigaOrdine> _righe = [];
    public IReadOnlyList<RigaOrdine>  Righe => _righe.AsReadOnly();

    // ── Costruzione ───────────────────────────────────────────────────────────

    private Ordine() { }

    /// <summary>
    /// Crea un ordine a partire da una proposta completata.
    /// Migrazione del flusso di conferma in frmConferma.frm / frmEmissione.frm.
    /// </summary>
    public static Ordine DaProposta(
        NumeroOrdine   numero,
        PropostaOrdine proposta,
        InfoFornitore  fornitore,
        string         ragioneSociale,
        string?        note = null)
    {
        Guard.AgainstFalse(
            proposta.Stato == PropostaOrdine.StatoProposta.Completata,
            "DaProposta", "Solo una proposta completata può generare un ordine.");

        var o = new Ordine(proposta.OperatoreId)
        {
            Numero         = numero,
            PropostaId     = proposta.Id,
            FornitoreId    = fornitore.FornitoreId,
            CodiceAnabase  = fornitore.CodiceAnabase,
            TipoFornitore  = fornitore.Tipo,
            RagioneSociale = ragioneSociale,
            DataEmissione  = DateTime.UtcNow,
            OperatoreId    = proposta.OperatoreId,
            NomeEmissione  = proposta.NomeEmissione,
            Note           = note,
            Stato          = StatoOrdine.Emesso,
        };

        // Indice fornitore 1-based nella lista della proposta
        var indice = 0;
        for (var fi = 0; fi < proposta.Fornitori.Count; fi++)
            if (proposta.Fornitori[fi].FornitoreId == fornitore.FornitoreId) { indice = fi + 1; break; }
        if (indice == 0) indice = 1;

        foreach (var riga in proposta.RigheDaEmettere.Where(r => r.IsFornitoreAbilitato(indice)
                                                               && r.QuantitaPerFornitore(indice) > 0))
            o._righe.Add(RigaOrdine.DaProposta(o.Id, riga, indice));

        if (o._righe.Count == 0)
            throw new BusinessRuleViolationException("OrdineVuoto",
                $"L'ordine per '{ragioneSociale}' non contiene righe.");

        o.Raise(new OrdineCreato(o.Id, numero, proposta.Id, fornitore.FornitoreId,
            o._righe.Count, o.ImportoTotale, proposta.OperatoreId));

        return o;
    }

    private Ordine(Guid? operatoreId) : base(operatoreId) { }

    // ── Calcolati ──────────────────────────────────────────────────────────────

    public int     TotalePezzi   => _righe.Sum(r => r.Quantita);
    public decimal ImportoTotale => _righe.Sum(r => r.CostoTotale);
    public int     NumeroRighe   => _righe.Count;

    // ── Transizioni di stato ───────────────────────────────────────────────────

    public void Trasmetti(Guid? operatoreId = null)
    {
        Guard.AgainstFalse(Stato == StatoOrdine.Emesso, "Trasmetti",
            $"L'ordine {Numero} non è nello stato Emesso.");
        Stato             = StatoOrdine.Trasmesso;
        DataTrasmissione  = DateTime.UtcNow;
        SetUpdated(operatoreId);
        Raise(new OrdineTrasmesso(Id, Numero, FornitoreId));
    }

    public void RegistraRicezione(DateTime dataRicezione, Guid? operatoreId = null)
    {
        Guard.AgainstFalse(
            Stato is StatoOrdine.Trasmesso or StatoOrdine.Emesso,
            "RegistraRicezione", $"L'ordine {Numero} non è trasmesso.");
        Stato          = StatoOrdine.Ricevuto;
        DataRicezione  = dataRicezione;
        SetUpdated(operatoreId);
        Raise(new OrdineRicevuto(Id, Numero, FornitoreId, dataRicezione));
    }

    public void Annulla(string motivazione, Guid? operatoreId = null)
    {
        Guard.AgainstFalse(
            Stato is StatoOrdine.Emesso or StatoOrdine.Trasmesso,
            "Annulla", $"L'ordine {Numero} non può essere annullato nello stato {Stato}.");
        MarkAsDeleted(operatoreId);
        Raise(new OrdineAnnullato(Id, Numero, motivazione, operatoreId));
    }

    /// <summary>Aggiorna la quantità arrivata su una riga (modulo Ricarico).</summary>
    public void ImpostaRicevuto(Guid prodottoId, int qtaArrivata, int qtaAssicurata,
        string? codiceMancante = null)
    {
        var riga = _righe.FirstOrDefault(r => r.ProdottoId == prodottoId)
            ?? throw new EntityNotFoundException(nameof(RigaOrdine), nameof(prodottoId), prodottoId);
        riga.ImpostaRicevuto(qtaArrivata, qtaAssicurata, codiceMancante);
    }
}
