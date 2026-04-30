namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  DOMAIN EVENTS
//  Nel VB6 la comunicazione tra moduli avveniva tramite variabili globali
//  (Public in modSessione.bas) e chiamate dirette tra DLL COM.
//  In C# usiamo Domain Events: ogni entità pubblica eventi che descrivono
//  ciò che è accaduto. Chi è interessato si iscrive tramite MediatR.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Marker interface per tutti gli eventi di dominio.
/// Un evento di dominio descrive qualcosa che è già accaduto nel sistema
/// (passato, immutabile) e che altri moduli potrebbero voler sapere.
/// Esempi: OrdineEmesso, VenditaCompletata, GiacenzaSottoscorta.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Momento esatto in cui l'evento si è verificato (UTC).</summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Identificatore univoco dell'evento.
    /// Utile per idempotenza nei gestori e per audit trail.
    /// </summary>
    Guid EventId { get; }
}

/// <summary>
/// Classe base per tutti gli eventi di dominio.
/// Fornisce automaticamente OccurredAt e EventId.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public Guid     EventId    { get; } = Guid.NewGuid();
}

/// <summary>
/// Interfaccia per il dispatcher degli eventi di dominio.
/// L'implementazione concreta vive in Infrastructure (usa MediatR).
/// Permette al Domain di rimanere puro senza dipendenze su MediatR.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Pubblica tutti gli eventi pendenti di un aggregate e li svuota.
    /// Da chiamare nell'UnitOfWork, dopo SaveChangesAsync().
    /// </summary>
    Task DispatchAndClearAsync(IEnumerable<AggregateRoot> aggregates,
                               CancellationToken ct = default);
}
