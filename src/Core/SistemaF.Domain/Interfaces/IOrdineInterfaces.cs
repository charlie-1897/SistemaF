using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Domain.Interfaces;

// ═══════════════════════════════════════════════════════════════════════════════
//  INTERFACCE — Modulo Ordine
// ═══════════════════════════════════════════════════════════════════════════════

// ── Repository ────────────────────────────────────────────────────────────────

public interface IOrdineRepository : IRepository<Ordine>
{
    Task<Ordine?> GetByNumeroAsync(NumeroOrdine numero, CancellationToken ct = default);
    Task<IReadOnlyList<Ordine>> GetByFornitoreAsync(Guid fornitoreId, CancellationToken ct = default);
    Task<IReadOnlyList<Ordine>> GetByPeriodoAsync(DateTime da, DateTime a, CancellationToken ct = default);
    Task<IReadOnlyList<Ordine>> GetByStatoAsync(StatoOrdine stato, CancellationToken ct = default);
    Task<NumeroOrdine> GeneraNumeroProgressivoAsync(int anno, CancellationToken ct = default);
    Task<bool> EsisteOrdinePerFornitoreAsync(Guid fornitoreId, StatoOrdine stato, CancellationToken ct = default);
}

public interface IPropostaOrdineRepository : IRepository<PropostaOrdine>
{
    Task<PropostaOrdine?> GetByOperatoreAttivaAsync(Guid operatoreId, CancellationToken ct = default);
    Task<PropostaOrdine?> GetByConfigurazioneAsync(Guid configurazioneId, CancellationToken ct = default);
    Task<IReadOnlyList<PropostaOrdine>> GetByOperatoreAsync(Guid operatoreId, CancellationToken ct = default);
}

// ── Servizi di dominio (dipendenze esterne che la pipeline di emissione richiede) ──

/// <summary>
/// Servizio per recuperare gli ultimi costi d'acquisto da un fornitore.
/// Migrazione di RecuperaUltimiCosti in EmissioneOrdine.cls.
/// Query su DettaglioOrdine/Ordine entro N giorni.
/// </summary>
public interface IUltimiCostiService
{
    /// <summary>
    /// Per ogni fornitore nella lista restituisce l'ultimo costo registrato
    /// per il prodotto specificato entro gli ultimi <paramref name="giorniLookback"/> giorni.
    /// Restituisce null se non trovato.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal?>> GetUltimiCostiAsync(
        Guid                 prodottoId,
        IEnumerable<Guid>    fornitoriIds,
        int                  giorniLookback,
        CancellationToken    ct = default);
}

/// <summary>
/// Servizio per recuperare i costi di listino fornitore.
/// Migrazione di RecuperaCostiListinoFornitore (usa CSFOfferte.cOfferte).
/// </summary>
public interface IListiniFornitorService
{
    Task<IReadOnlyDictionary<Guid, decimal>> GetCostiListinoAsync(
        Guid              prodottoId,
        IEnumerable<Guid> fornitoriIds,
        CancellationToken ct = default);
}

/// <summary>
/// Servizio per recuperare sconti e condizioni d'acquisto per fornitore.
/// Migrazione di RecuperaScontiCondizioniFornitore (usa CSFOrdCommon.clsScontiCondizioni).
/// </summary>
public interface IScontiCondizioniService
{
    Task<IReadOnlyDictionary<Guid, ScontoCondizione>> GetScontiAsync(
        Guid              prodottoId,
        string            settoreInventario,
        string            classe,
        string            categoriaRicetta,
        IEnumerable<(Guid Id, int Quantita)> fornitori,
        CancellationToken ct = default);
}

/// <summary>Risultato di un calcolo sconti/condizioni per un fornitore.</summary>
public sealed record ScontoCondizione(
    decimal          Sconto,
    int              QuantitaArrotondata,
    TipoCalcoloSconto TipoCalcolo);

/// <summary>
/// Servizio per recuperare le offerte attive per un prodotto.
/// Migrazione di cIOfferte/RecuperaDati in VerificaAssegnazioni.
/// </summary>
public interface IOfferteService
{
    Task<IReadOnlyList<OffertaProdotto>> GetOfferteAsync(
        Guid              prodottoId,
        int               quantitaMaxValutare,
        IEnumerable<Guid> fornitoriIds,
        CancellationToken ct = default);
}

/// <summary>Un'offerta promozionale per un prodotto.</summary>
public sealed record OffertaProdotto(
    Guid    FornitoreId,
    decimal Costo,
    decimal ScontoCalcolato,
    int     QuantitaMinima,
    int     QuantitaOmaggio);

/// <summary>
/// Servizio per calcolare gli indici di vendita di un prodotto.
/// Migrazione di clsCalcola.IndiceVendita_* in EmissioneOrdine.cls.
/// </summary>
public interface IIndiciVenditaService
{
    Task<decimal> GetTendenzialeAsync(Guid prodottoId, DateOnly dataRiferimento, CancellationToken ct = default);
    Task<decimal> GetAnnualeAsync(Guid prodottoId, int anno, CancellationToken ct = default);
    Task<decimal> GetMensileAsync(Guid prodottoId, int anno, int mese, CancellationToken ct = default);
    Task<decimal> GetPeriodoAsync(Guid prodottoId, DateOnly dal, DateOnly al, CancellationToken ct = default);
    Task<decimal> GetMediaAritmeticaAsync(Guid prodottoId, DateOnly dataRiferimento, CancellationToken ct = default);
}

/// <summary>
/// Servizio per recuperare i prodotti dall'archivio (ProdBase/Esposizione).
/// Migrazione di RecuperaProdotti_ProdBase in EmissioneOrdine.cls.
/// </summary>
public interface IArchivioPropostaService
{
    Task<IReadOnlyList<ProdottoArchivio>> GetProdottiDaOrdinareAsync(
        Guid                        configurazioneId,
        Guid                        fornitoreId,
        FiltriProdottoArchivio      filtri,
        CancellationToken           ct = default);
}

/// <summary>Un prodotto estratto dall'archivio per la proposta.</summary>
public sealed record ProdottoArchivio(
    Guid          ProdottoId,
    CodiceProdotto CodiceFarmaco,
    string        Descrizione,
    int           Quantita,
    int           GiacenzaEsposizione,
    int           ScortaMinimaEsposizione,
    int           ScortaMassimaEsposizione,
    int           GiacenzaMagazzino,
    int           ScortaMinimaMagazzino,
    int           ScortaMassimaMagazzino,
    decimal       UltimoCostoGrossista,
    decimal       UltimoCostoDitta,
    string        SettoreInventario,
    Guid?         FornitorePreferenzialeId);

/// <summary>Filtri per l'estrazione dall'archivio prodotti.</summary>
public sealed record FiltriProdottoArchivio(
    bool                      IsTrattati,
    bool                      IsOrdineLiberoDitta,
    ParametriIndiceDiVendita? IndiceDiVendita,
    ParametriRipristinoScorta? RipristinoScorta);
