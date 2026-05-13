using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Interfaces;
using SistemaF.Domain.Specifications;
using SistemaF.Domain.ValueObjects;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Repositories;

public sealed class ProdottoRepository(SistemaFDbContext db) : IProdottoRepository
{
    // \u2500\u2500 Base IRepository<Prodotto> \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public Task<Prodotto?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Prodotti
             .Include("_scadenze")
             .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.Prodotti.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(Prodotto p, CancellationToken ct)
        => await db.Prodotti.AddAsync(p, ct);

    public void Update(Prodotto p)
        => db.Prodotti.Update(p);

    public void Remove(Prodotto p)
        => db.Prodotti.Remove(p);

    // \u2500\u2500 Ricerche per chiave \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public Task<Prodotto?> GetByCodiceFarmacoAsync(CodiceProdotto codice, CancellationToken ct)
        => db.Prodotti
             .Include("_scadenze")
             .FirstOrDefaultAsync(p => p.CodiceFarmaco.Valore == codice.Valore, ct);

    public Task<Prodotto?> GetByCodiceEANAsync(CodiceEAN ean, CancellationToken ct)
        => db.Prodotti
             .Include("_scadenze")
             .FirstOrDefaultAsync(p => p.CodiceEAN != null && p.CodiceEAN.Valore == ean.Valore, ct);

    // \u2500\u2500 Ricerche per Specification \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public async Task<IReadOnlyList<Prodotto>> CercaAsync(
        Specification<Prodotto> spec,
        int limit, int offset,
        CancellationToken ct)
        => await db.Prodotti
                   .Where(spec.ToExpression())
                   .OrderBy(p => p.Descrizione)
                   .Skip(offset)
                   .Take(limit)
                   .ToListAsync(ct);

    public Task<int> ContaAsync(Specification<Prodotto> spec, CancellationToken ct)
        => db.Prodotti.CountAsync(spec.ToExpression(), ct);

    // \u2500\u2500 Query di business \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public async Task<IReadOnlyList<Prodotto>> GetSottoscortaAsync(CancellationToken ct)
        => await db.Prodotti
                   .Where(p => p.IsAttivo
                             && p.GiacenzaEsposizione.ScortaMinima > 0
                             && p.GiacenzaEsposizione.Giacenza < p.GiacenzaEsposizione.ScortaMinima)
                   .OrderBy(p => p.Descrizione)
                   .ToListAsync(ct);

    public async Task<IReadOnlyList<Prodotto>> GetInScadenzaAsync(int giorni, CancellationToken ct)
    {
        var soglia = DateOnly.FromDateTime(DateTime.Today.AddDays(giorni));
        return await db.Prodotti
                       .Include("_scadenze")
                       .Where(p => p.IsAttivo &&
                                   p.Scadenze.Any(s => s.DataScadenza <= soglia))
                       .OrderBy(p => p.Descrizione)
                       .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Prodotto>> GetInvendibiliAsync(CancellationToken ct)
        => await db.Prodotti
                   .Where(p => p.IsInvendibile)
                   .OrderBy(p => p.Descrizione)
                   .ToListAsync(ct);

    public async Task<IReadOnlyList<Prodotto>> GetSegnalatiAsync(CancellationToken ct)
        => await db.Prodotti
                   .Where(p => p.IsAttivo && p.IsSegnalato)
                   .OrderBy(p => p.Descrizione)
                   .ToListAsync(ct);

    // \u2500\u2500 Esistenza \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    public Task<bool> EsisteCodiceFarmacoAsync(CodiceProdotto codice, CancellationToken ct)
        => db.Prodotti.AnyAsync(p => p.CodiceFarmaco.Valore == codice.Valore, ct);

    public Task<bool> EsisteCodiceEANAsync(CodiceEAN ean, CancellationToken ct)
        => db.Prodotti.AnyAsync(p => p.CodiceEAN != null && p.CodiceEAN.Valore == ean.Valore, ct);
}
