using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  DOMAIN EVENTS — Modulo Ordine
// ═══════════════════════════════════════════════════════════════════════════════

// ── PropostaOrdine events ─────────────────────────────────────────────────────

/// <summary>Sessione di emissione ordine avviata.</summary>
public record PropostaOrdineCreata(
    Guid   PropostaId,
    Guid   OperatoreId,
    Guid   ConfigurazioneId,
    string NomeEmissione) : DomainEvent;

/// <summary>Pipeline di elaborazione completata; proposta pronta per la conferma.</summary>
public record PropostaOrdineCompletata(
    Guid                 PropostaId,
    Guid                 OperatoreId,
    RiepilogoElaborazione Riepilogo) : DomainEvent;

/// <summary>Proposta confermata; ordine generato.</summary>
public record PropostaOrdineEmessa(
    Guid PropostaId,
    Guid OrdineId,
    Guid OperatoreId) : DomainEvent;

/// <summary>Proposta annullata senza emettere l'ordine.</summary>
public record PropostaOrdineAnnullata(
    Guid PropostaId,
    Guid OperatoreId) : DomainEvent;

/// <summary>Elaborazione pipeline interrotta dall'operatore.</summary>
public record ElaborazioneInterrotta(
    Guid PropostaId,
    Guid OperatoreId) : DomainEvent;

/// <summary>Prodotto aggiunto manualmente alla proposta.</summary>
public record ProdottoAggiuntoProposta(
    Guid           PropostaId,
    Guid           ProdottoId,
    CodiceProdotto CodiceFarmaco,
    FonteAggiunta  Fonte,
    int            Quantita) : DomainEvent;

/// <summary>Prodotto rimosso dalla proposta.</summary>
public record ProdottoRimossoProposta(
    Guid           PropostaId,
    Guid           ProdottoId,
    CodiceProdotto CodiceFarmaco) : DomainEvent;

// ── Ordine events ─────────────────────────────────────────────────────────────

/// <summary>Ordine emesso e registrato nel sistema.</summary>
public record OrdineCreato(
    Guid         OrdineId,
    NumeroOrdine Numero,
    Guid         PropostaId,
    Guid         FornitoreId,
    int          NumeroRighe,
    decimal      ImportoTotale,
    Guid         OperatoreId) : DomainEvent;

/// <summary>Ordine trasmesso al fornitore.</summary>
public record OrdineTrasmesso(
    Guid         OrdineId,
    NumeroOrdine Numero,
    Guid         FornitoreId) : DomainEvent;

/// <summary>Merce dell'ordine arrivata (da modulo Ricarico).</summary>
public record OrdineRicevuto(
    Guid         OrdineId,
    NumeroOrdine Numero,
    Guid         FornitoreId,
    DateTime     DataRicezione) : DomainEvent;

/// <summary>Ordine annullato.</summary>
public record OrdineAnnullato(
    Guid         OrdineId,
    NumeroOrdine Numero,
    string       Motivazione,
    Guid?        OperatoreId) : DomainEvent;
