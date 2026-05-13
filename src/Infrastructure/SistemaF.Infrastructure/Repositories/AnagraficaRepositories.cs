using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Repositories;

// \u2500\u2500 Fornitore \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

public sealed class FornitoreRepository(SistemaFDbContext db)
    : IFornitoreRepository
{
    public Task<Fornitore?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Fornitori.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.Fornitori.AnyAsync(f => f.Id == id, ct);

    public async Task AddAsync(Fornitore f, CancellationToken ct = default)
        => await db.Fornitori.AddAsync(f, ct);

    public void Update(Fornitore f) => db.Fornitori.Update(f);
    public void Remove(Fornitore f) => db.Fornitori.Remove(f);

    public Task<Fornitore?> GetByCodiceAnabaseAsync(long codice, CancellationToken ct)
        => db.Fornitori.FirstOrDefaultAsync(f => f.CodiceAnabase == codice, ct);

    public async Task<IReadOnlyList<Fornitore>> GetAttiviAsync(CancellationToken ct)
        => await db.Fornitori
             .Where(f => f.IsAttivo)
             .OrderBy(f => f.RagioneSociale)
             .ToListAsync(ct);

    public async Task<IReadOnlyList<Fornitore>> GetByTipoAsync(
        TipoFornitore tipo, CancellationToken ct)
        => await db.Fornitori
             .Where(f => f.Tipo == tipo && f.IsAttivo)
             .OrderBy(f => f.RagioneSociale)
             .ToListAsync(ct);

    public Task<Fornitore?> GetMagazzinoInternoAsync(CancellationToken ct)
        => db.Fornitori.FirstOrDefaultAsync(
             f => f.IsMagazzino && f.IsAttivo, ct);
}

// \u2500\u2500 Operatore \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

public sealed class OperatoreRepository(SistemaFDbContext db)
    : IOperatoreRepository
{
    public Task<Operatore?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Operatori.FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => db.Operatori.AnyAsync(o => o.Id == id, ct);

    public async Task AddAsync(Operatore o, CancellationToken ct = default)
        => await db.Operatori.AddAsync(o, ct);

    public void Update(Operatore o) => db.Operatori.Update(o);
    public void Remove(Operatore o) => db.Operatori.Remove(o);

    public Task<Operatore?> GetByLoginAsync(string login, CancellationToken ct)
        => db.Operatori.FirstOrDefaultAsync(
             o => o.Login == login.ToLowerInvariant() && o.IsAttivo, ct);

    public Task<Operatore?> GetByBadgeAsync(string badge, CancellationToken ct)
        => db.Operatori.FirstOrDefaultAsync(
             o => o.Badge == badge && o.IsAttivo, ct);

    public async Task<IReadOnlyList<Operatore>> GetAttiviAsync(CancellationToken ct)
        => await db.Operatori
             .Where(o => o.IsAttivo)
             .OrderBy(o => o.NomeCognome)
             .ToListAsync(ct);
}

// \u2500\u2500 ConfigurazioneEmissione \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

public sealed class ConfigurazioneEmissioneRepository(SistemaFDbContext db)
    : IConfigurazioneEmissioneRepository
{
    public Task<ConfigurazioneEmissione?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.ConfigurazioniEmissione.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.ConfigurazioniEmissione.AnyAsync(c => c.Id == id, ct);

    public async Task AddAsync(ConfigurazioneEmissione c, CancellationToken ct)
        => await db.ConfigurazioniEmissione.AddAsync(c, ct);

    public void Update(ConfigurazioneEmissione c)
        => db.ConfigurazioniEmissione.Update(c);

    public void Remove(ConfigurazioneEmissione c)
        => db.ConfigurazioniEmissione.Remove(c);

    public async Task<IReadOnlyList<ConfigurazioneEmissione>> GetAttiveAsync(
        CancellationToken ct)
        => await db.ConfigurazioniEmissione
             .Where(c => c.IsAttiva)
             .OrderBy(c => c.Nome)
             .ToListAsync(ct);

    public async Task<IReadOnlyList<Fornitore>> GetFornitoriAsync(
        Guid configurazioneId, CancellationToken ct)
    {
        // Carica i fornitoriId associati a questa configurazione in ordine
        var fornitoriIds = await db.ConfigurazioniEmissioneFornitori
            .Where(cf => cf.ConfigurazioneId == configurazioneId && cf.IsAbilitato)
            .OrderBy(cf => cf.OrdineIndice)
            .Select(cf => cf.FornitoreId)
            .ToListAsync(ct);

        if (fornitoriIds.Count == 0) return [];

        var fornitori = await db.Fornitori
            .Where(f => fornitoriIds.Contains(f.Id) && f.IsAttivo)
            .ToListAsync(ct);

        // Rispetta l'ordinamento originale
        return fornitoriIds
            .Select(id => fornitori.FirstOrDefault(f => f.Id == id))
            .Where(f => f is not null)
            .Cast<Fornitore>()
            .ToList();
    }
}

// \u2500\u2500 Farmacia \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

public sealed class FarmaciaRepository(SistemaFDbContext db)
    : IFarmaciaRepository
{
    public Task<Farmacia?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Farmacie.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.Farmacie.AnyAsync(f => f.Id == id, ct);

    public async Task AddAsync(Farmacia f, CancellationToken ct)
        => await db.Farmacie.AddAsync(f, ct);

    public void Update(Farmacia f) => db.Farmacie.Update(f);
    public void Remove(Farmacia f) => db.Farmacie.Remove(f);

    // Istanza singleton: sempre la prima farmacia nel DB
    public Task<Farmacia?> GetFarmaciaCorrente(CancellationToken ct)
        => db.Farmacie.OrderBy(f => f.CreatedAt).FirstOrDefaultAsync(ct);
}
