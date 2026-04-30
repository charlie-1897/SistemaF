namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  REPOSITORY INTERFACES
//
//  Nel VB6 l'accesso ai dati era mescolato direttamente nelle Form e nelle DLL:
//    Dim rs As Recordset
//    rs.Open "SELECT * FROM Prodotti WHERE cpCodice = '" & sCodice & "'", CSFDB
//
//  In C# il Repository separa il dominio dalla persistenza.
//  Le interfacce vivono nel Domain (per rispettare la Dependency Rule:
//  il Domain non può dipendere dall'Infrastructure).
//  Le implementazioni concrete vivono in Infrastructure.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Contratto base per tutti i repository di SistemaF.
/// Definisce le operazioni CRUD standard che ogni aggregate root supporta.
/// </summary>
public interface IRepository<TAggregate>
    where TAggregate : AggregateRoot
{
    // ── Lettura ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Recupera un aggregate per Id.
    /// Restituisce null se non trovato (evita eccezioni per ricerche normali).
    /// </summary>
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Recupera un aggregate per Id; lancia EntityNotFoundException se assente.
    /// Usare quando l'assenza è un errore di dominio, non un caso normale.
    /// </summary>
    async Task<TAggregate> GetByIdOrThrowAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        return entity
            ?? throw new EntityNotFoundException(typeof(TAggregate).Name, id);
    }

    /// <summary>
    /// Restituisce true se esiste un aggregate con l'Id specificato.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    // ── Scrittura ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Aggiunge un nuovo aggregate al contesto di persistenza.
    /// Il salvataggio fisico avviene tramite IUnitOfWork.SaveChangesAsync().
    /// </summary>
    Task AddAsync(TAggregate aggregate, CancellationToken ct = default);

    /// <summary>
    /// Marca un aggregate come modificato.
    /// In EF Core è spesso automatico (change tracking); il metodo
    /// è esposto esplicitamente per chiarezza d'intenzione.
    /// </summary>
    void Update(TAggregate aggregate);

    /// <summary>
    /// Rimuove fisicamente un aggregate dal database.
    /// Preferire il soft delete (ISoftDeletable) per entità con storico.
    /// </summary>
    void Remove(TAggregate aggregate);
}

/// <summary>
/// Unit of Work: coordina il salvataggio transazionale di più aggregate.
///
/// Nel VB6 le transazioni erano gestite con:
///   CSFDB.BeginTrans
///   CSFDB.CommitTrans / CSFDB.Rollback
///
/// Qui l'IUnitOfWork garantisce che tutte le modifiche agli aggregate
/// siano salvate atomicamente in un'unica transazione DB, e che
/// gli eventi di dominio vengano pubblicati solo a commit avvenuto.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Salva tutte le modifiche pendenti nel database.
    /// Dopo il salvataggio, pubblica gli eventi di dominio degli aggregate modificati.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Apre esplicitamente una transazione di database.
    /// Usare solo quando servono operazioni multi-step che devono essere atomiche
    /// (es. importazione batch di giacenze).
    /// </summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Conferma la transazione aperta con BeginTransactionAsync.</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>Annulla la transazione aperta con BeginTransactionAsync.</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
