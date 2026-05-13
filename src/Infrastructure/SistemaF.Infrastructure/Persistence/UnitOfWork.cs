using MediatR;
using Microsoft.EntityFrameworkCore.Storage;
using SistemaF.Domain.Common;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure;

// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
//  UNIT OF WORK
//
//  Nel VB6 le transazioni erano gestite con:
//    CSFDB.BeginTrans
//    ...modifiche...
//    CSFDB.CommitTrans / CSFDB.Rollback
//
//  Qui l'UnitOfWork:
//    1. Salva atomicamente tutte le modifiche EF Core
//    2. Pubblica gli eventi di dominio DOPO il commit (mai prima)
//    3. Supporta transazioni esplicite per operazioni multi-aggregate
//
//  La pubblicazione DOPO il commit \u00e8 critica: garantisce che gli handler
//  degli eventi trovino i dati gi\u00e0 persistiti.
// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550

public sealed class UnitOfWork(
    SistemaFDbContext db,
    IPublisher        publisher) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    // \u2500\u2500 SaveChanges con pubblicazione eventi \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Raccoglie gli eventi PRIMA del save (dopo il save i ChangeTracker
        // potrebbe perdere lo stato Added/Modified)
        var events = db.GetDomainEvents();

        var count = await db.SaveChangesAsync(ct);

        // Pubblica gli eventi di dominio solo dopo il commit fisico
        db.ClearAllDomainEvents();
        foreach (var evt in events)
            await publisher.Publish(evt, ct);

        return count;
    }

    // \u2500\u2500 Transazioni esplicite \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "Una transazione \u00e8 gi\u00e0 aperta. Fare Commit o Rollback prima di aprirne una nuova.");
        _transaction = await db.Database.BeginTransactionAsync(ct);
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("Nessuna transazione aperta.");
        try
        {
            await db.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        try   { await _transaction.RollbackAsync(ct); }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
