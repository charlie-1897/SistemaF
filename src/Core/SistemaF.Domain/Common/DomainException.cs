namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  DOMAIN EXCEPTIONS
//
//  Nel VB6 la gestione degli errori era affidata a:
//    On Error GoTo GestioneErrori  (goto degli errori — difficile da tracciare)
//    MsgBox Err.Description        (messaggi all'utente direttamente dal dominio)
//    Resume Next                   (inghiottire silenziosamente gli errori)
//
//  In C# usiamo eccezioni tipizzate che:
//    • Separano errori di dominio (violazioni di regole di business) da
//      errori tecnici (rete, database, null reference)
//    • Vengono catturate nei Command Handler e convertite in messaggi utente
//    • Portano un codice machina-leggibile per internazionalizzazione/logging
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Eccezione base per tutte le violazioni di regole di business nel dominio.
/// Viene lanciata quando un'operazione contraddice le invarianti del modello.
/// NON usare per errori tecnici (usare InvalidOperationException, ArgumentException ecc.).
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Codice identificativo dell'errore, leggibile dal codice.
    /// Convenzione: "ENTITA_ERRORE" in maiuscolo, es. "ORDINE_SENZA_RIGHE".
    /// Usato per logging strutturato e per costruire messaggi localizzati.
    /// </summary>
    public string ErrorCode { get; }

    public DomainException(string message, string? errorCode = null)
        : base(message)
    {
        ErrorCode = errorCode ?? "DOMAIN_ERROR";
    }

    public DomainException(string message, Exception innerException,
                           string? errorCode = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode ?? "DOMAIN_ERROR";
    }
}

/// <summary>
/// Eccezione per violazioni di accesso / autorizzazione.
/// Lanciata quando l'operatore non ha i permessi necessari.
/// Nel VB6 corrispondeva ai controlli su xAutorizzazioni in modSessione.bas.
/// </summary>
public sealed class DomainUnauthorizedException(string message)
    : DomainException(message, "UNAUTHORIZED");

/// <summary>
/// Eccezione per tentativi di accesso a risorse inesistenti.
/// Meglio di null reference: porta il tipo e l'Id dell'entità non trovata.
/// </summary>
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityType, object id)
        : base($"{entityType} con Id '{id}' non trovato.", "NOT_FOUND")
    { }

    public EntityNotFoundException(string entityType, string fieldName, object fieldValue)
        : base($"{entityType} con {fieldName} '{fieldValue}' non trovato.", "NOT_FOUND")
    { }
}

/// <summary>
/// Eccezione per tentativi di creare un'entità che esiste già
/// (violazione di unicità di business, non solo di database).
/// </summary>
public sealed class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityType, string fieldName, object fieldValue)
        : base($"Esiste già un/una {entityType} con {fieldName} '{fieldValue}'.", "DUPLICATE")
    { }
}

/// <summary>
/// Eccezione per transizioni di stato non valide.
/// Usata dagli aggregate che gestiscono una macchina a stati
/// (es. Ordine: InComposizione → Emesso → Ricevuto).
/// </summary>
public sealed class InvalidStatoException : DomainException
{
    public InvalidStatoException(string entityType, object statoAttuale, object statoRichiesto)
        : base(
            $"Impossibile eseguire l'operazione '{statoRichiesto}' su {entityType} " +
            $"in stato '{statoAttuale}'.",
            "INVALID_STATE")
    { }
}

/// <summary>
/// Eccezione per violazioni di soglie e limiti di business.
/// Es.: quantità negativa, prezzo fuori range, giacenza insufficiente.
/// </summary>
public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string regola, string dettaglio)
        : base($"Violazione della regola '{regola}': {dettaglio}.", "RULE_VIOLATION")
    { }
}
