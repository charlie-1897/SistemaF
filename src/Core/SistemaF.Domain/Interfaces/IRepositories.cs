using SistemaF.Domain.Entities;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Interfaces;

/// <summary>
/// Contratto per il repository dei prodotti.
/// L'implementazione concreta vive in SistemaF.Infrastructure.
/// </summary>
public interface IProdottoRepository
{
    Task<Prodotto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Prodotto?> GetByCodiceAsync(CodiceProdotto codice, CancellationToken ct = default);
    Task<Prodotto?> GetByEANAsync(CodiceEAN ean, CancellationToken ct = default);
    Task<IEnumerable<Prodotto>> GetSottoscortaAsync(CancellationToken ct = default);
    Task<IEnumerable<Prodotto>> CercaAsync(string termineRicerca, int limit = 50, CancellationToken ct = default);
    Task AddAsync(Prodotto prodotto, CancellationToken ct = default);
    void Update(Prodotto prodotto);
}

/// <summary>Contratto per il repository degli ordini.</summary>
public interface IOrdineRepository
{
    Task<Ordine?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Ordine?> GetByNumeroAsync(string numero, CancellationToken ct = default);
    Task<IEnumerable<Ordine>> GetByStatoAsync(StatoOrdine stato, CancellationToken ct = default);
    Task<IEnumerable<Ordine>> GetByPeriodoAsync(DateTime da, DateTime a, CancellationToken ct = default);
    Task AddAsync(Ordine ordine, CancellationToken ct = default);
    void Update(Ordine ordine);
}

/// <summary>
/// Unit of Work: coordina il salvataggio transazionale di più aggregate.
/// Sostituisce il vecchio pattern con CSFDB As Database e Workspace del VB6.
/// </summary>
public interface IUnitOfWork
{
    IProdottoRepository Prodotti { get; }
    IOrdineRepository Ordini { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
