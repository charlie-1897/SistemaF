namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  AGGREGATE ROOT
//
//  In Domain-Driven Design un Aggregate è un gruppo di oggetti trattati come
//  un'unica unità transazionale. L'AggregateRoot è il punto di accesso esterno:
//  tutto passa attraverso di lui, niente modifica direttamente i figli.
//
//  Nel VB6 di SistemaF questo confine non esisteva: le finestre Form accedevano
//  direttamente ai campi dei moduli DLL, bypassando qualsiasi logica. Il risultato
//  erano stati inconsistenti difficilissimi da tracciare.
//
//  Esempi di Aggregate Root in SistemaF:
//    • Ordine  (radice) → RigaOrdine (figli)
//    • Vendita (radice) → RigaVendita, Pagamento (figli)
//    • Fattura (radice) → RigaFattura (figli)
//    • Prodotto (radice) — entità semplice, figlio unico
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Radice di un Aggregate di dominio.
/// Gestisce la lista degli eventi di dominio pendenti (non ancora pubblicati).
/// </summary>
public abstract class AggregateRoot : Entity
{
    // ── Domain Events ─────────────────────────────────────────────────────────

    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Coda degli eventi di dominio generati ma non ancora pubblicati.
    /// Viene svuotata dall'UnitOfWork dopo SaveChangesAsync().
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents
        => _domainEvents.AsReadOnly();

    /// <summary>
    /// Accoda un evento di dominio.
    /// Da chiamare SOLO all'interno dei metodi dell'AggregateRoot,
    /// ogni volta che avviene una transizione di stato significativa.
    /// </summary>
    protected void Raise(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Svuota la coda degli eventi. Chiamato dall'IDomainEventDispatcher
    /// dopo che tutti gli eventi sono stati pubblicati con successo.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// True se ci sono eventi pendenti da pubblicare.
    /// </summary>
    public bool HasDomainEvents => _domainEvents.Count > 0;

    // ── Versioning (Optimistic Concurrency) ───────────────────────────────────

    /// <summary>
    /// Token per il controllo ottimistico della concorrenza.
    /// EF Core lo usa come RowVersion: se due operatori modificano
    /// lo stesso record contemporaneamente, la seconda operazione fallisce
    /// con DbUpdateConcurrencyException invece di sovrascrivere silenziosamente.
    /// Sostituisce il vecchio pattern "leggi-modifica-riscrivi" del VB6 che
    /// causava perdite di dati in ambienti multi-postazione.
    /// </summary>
    public byte[]? RowVersion { get; private set; }

    // ── Costruzione ───────────────────────────────────────────────────────────

    protected AggregateRoot() { }
    protected AggregateRoot(Guid? operatoreId = null) : base(operatoreId) { }
}

/// <summary>
/// Versione generica di AggregateRoot con Id fortemente tipizzato.
/// </summary>
public abstract class AggregateRoot<TId> : AggregateRoot
    where TId : notnull
{
    public new TId Id
    {
        get => TypedId
            ?? throw new InvalidOperationException("L'Id tipizzato non è stato impostato.");
        private protected set => TypedId = value;
    }

    private TId? TypedId { get; set; }

    protected AggregateRoot() { }
    protected AggregateRoot(TId id, Guid? operatoreId = null) : base(operatoreId)
        => TypedId = id;
}
