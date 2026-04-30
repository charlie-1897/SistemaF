namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  SOFT DELETE  +  ENTITÀ AUDITABILI
//
//  Nel vecchio Access/.mdb del VB6 i record erano spesso eliminati fisicamente,
//  rendendo impossibile la ricostruzione dello storico.
//  In alcuni moduli (es. ClienteService) esisteva un flag "Annullato" (Boolean)
//  per simulare l'eliminazione logica.
//
//  Qui standardizziamo il pattern:
//    ISoftDeletable → interfaccia da implementare nelle entità che lo richiedono
//    SoftDeletableAggregateRoot → mixin pronto per le radici degli aggregate
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interfaccia per entità con cancellazione logica (soft delete).
/// Il record rimane nel database ma viene filtrato automaticamente da EF Core
/// tramite un global query filter configurato in SistemaFDbContext.
/// </summary>
public interface ISoftDeletable
{
    bool      IsDeleted { get; }
    DateTime? DeletedAt { get; }
    Guid?     DeletedBy { get; }
}

/// <summary>
/// AggregateRoot con supporto nativo a soft delete.
/// Usare per entità che non devono mai essere cancellate fisicamente
/// (storico ordini, fatture, movimenti di magazzino).
/// </summary>
public abstract class SoftDeletableAggregateRoot : AggregateRoot, ISoftDeletable
{
    public bool      IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid?     DeletedBy { get; private set; }

    /// <summary>
    /// Marca l'entità come cancellata logicamente.
    /// Da chiamare tramite un metodo di dominio specifico, mai direttamente
    /// dal repository o dal servizio applicativo.
    /// </summary>
    protected void MarkAsDeleted(Guid? operatoreId = null)
    {
        if (IsDeleted)
            throw new DomainException(
                $"{GetType().Name}[{Id}] è già stato eliminato.", "ALREADY_DELETED");

        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = operatoreId;
        SetUpdated(operatoreId);
    }

    protected SoftDeletableAggregateRoot() { }
    protected SoftDeletableAggregateRoot(Guid? operatoreId = null) : base(operatoreId) { }
}
