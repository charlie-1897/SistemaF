using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Prodotto;

// ═══════════════════════════════════════════════════════════════════════════════
//  DOMAIN EVENTS — Prodotto
//
//  Ogni evento corrisponde a una transizione di stato significativa.
//  Nel VB6 queste notifiche erano implicite (variabili globali, MsgBox)
//  o totalmente assenti. Qui sono esplicite, strutturate e testabili.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Prodotto aggiunto al catalogo.</summary>
public record ProdottoCreato(
    Guid           ProdottoId,
    CodiceProdotto Codice,
    string         Descrizione,
    Guid?          OperatoreId) : DomainEvent;

/// <summary>Prezzo di un prodotto aggiornato (pubblico, listino o costo).</summary>
public record PrezzoProdottoAggiornato(
    Guid                 ProdottoId,
    CodiceProdotto       Codice,
    Prezzo?              PrezzoVecchio,
    Prezzo               PrezzoNuovo,
    TipoAzioneRettifica  Azione,
    TipoCosaRettifica    TipoCosa,
    Guid?                OperatoreId) : DomainEvent;

/// <summary>
/// Giacenza di un prodotto variata (esposizione o magazzino retro).
/// Sostituisce la chiamata a AggiungiRettifica.Aggiungi() in GiacenzaProdotto.cls.
/// </summary>
public record GiacenzaVariata(
    Guid                        ProdottoId,
    CodiceProdotto              Codice,
    TipoDepositoMagazzino       Deposito,
    ModalitaVariazioneGiacenza  Modalita,
    int                         QuantitaVecchia,
    int                         QuantitaNuova,
    TipoAzioneRettifica         Azione,
    TipoCosaRettifica           TipoCosa,
    TipoModuloRettifica         Modulo,
    Guid?                       OperatoreId) : DomainEvent;

/// <summary>
/// Giacenza esposizione scesa sotto la scorta minima.
/// Nel VB6 era segnalato visivamente nella schermata mancanti.
/// </summary>
public record SottoscortaRilevata(
    Guid           ProdottoId,
    CodiceProdotto Codice,
    string         Descrizione,
    int            GiacenzaAttuale,
    int            ScortaMinima) : DomainEvent;

/// <summary>Prodotto marcato come invendibile.</summary>
public record ProdottoMarcatoInvendibile(
    Guid           ProdottoId,
    CodiceProdotto Codice,
    int            GiacenzaInvendibile,
    Guid?          OperatoreId) : DomainEvent;

/// <summary>Prodotto con lotto in scadenza nei prossimi 90 giorni.</summary>
public record ProdottoInScadenza(
    Guid           ProdottoId,
    CodiceProdotto Codice,
    string         Descrizione,
    CodiceLotto    Lotto,
    DateOnly       DataScadenza,
    int            Quantita) : DomainEvent;

/// <summary>Prodotto disattivato dal catalogo.</summary>
public record ProdottoDisattivato(
    Guid           ProdottoId,
    CodiceProdotto Codice,
    Guid?          OperatoreId) : DomainEvent;
