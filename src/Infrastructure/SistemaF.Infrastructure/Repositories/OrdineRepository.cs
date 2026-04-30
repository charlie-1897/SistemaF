using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Interfaces;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Repositories;

internal sealed class OrdineRepository(SistemaFDbContext db) : IOrdineRepository
{
    public Task<Ordine?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.Ordini
             .Include("_righe")
             .FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.Ordini.AnyAsync(o => o.Id == id, ct);

    public async Task AddAsync(Ordine o, CancellationToken ct)
        => await db.Ordini.AddAsync(o, ct);

    public void Update(Ordine o) => db.Ordini.Update(o);

    public void Remove(Ordine o) => db.Ordini.Remove(o);

    public Task<Ordine?> GetByNumeroAsync(NumeroOrdine numero, CancellationToken ct)
        => db.Ordini
             .Include("_righe")
             .FirstOrDefaultAsync(o => o.Numero.Valore == numero.Valore, ct);

    public async Task<IReadOnlyList<Ordine>> GetByFornitoreAsync(Guid fornitoreId, CancellationToken ct)
        => await db.Ordini
                   .Where(o => o.FornitoreId == fornitoreId)
                   .OrderByDescending(o => o.DataEmissione)
                   .ToListAsync(ct);

    public async Task<IReadOnlyList<Ordine>> GetByPeriodoAsync(DateTime da, DateTime a, CancellationToken ct)
        => await db.Ordini
                   .Where(o => o.DataEmissione >= da && o.DataEmissione <= a)
                   .OrderByDescending(o => o.DataEmissione)
                   .ToListAsync(ct);

    public async Task<IReadOnlyList<Ordine>> GetByStatoAsync(StatoOrdine stato, CancellationToken ct)
        => await db.Ordini
                   .Where(o => o.Stato == stato)
                   .OrderByDescending(o => o.DataEmissione)
                   .ToListAsync(ct);

    public async Task<NumeroOrdine> GeneraNumeroProgressivoAsync(int anno, CancellationToken ct)
    {
        var prefisso  = $"{anno}";
        var maxEsistente = await db.Ordini
            .Where(o => o.Numero.Valore.StartsWith(prefisso))
            .MaxAsync(o => (string?)o.Numero.Valore, ct);

        var progressivo = maxEsistente is null
            ? 1L
            : long.Parse(maxEsistente[4..]) + 1;

        return NumeroOrdine.Da(anno, progressivo);
    }

    public Task<bool> EsisteOrdinePerFornitoreAsync(Guid fornitoreId, StatoOrdine stato, CancellationToken ct)
        => db.Ordini.AnyAsync(o => o.FornitoreId == fornitoreId && o.Stato == stato, ct);
}

internal sealed class PropostaOrdineRepository(SistemaFDbContext db)
    : IPropostaOrdineRepository
{
    public Task<PropostaOrdine?> GetByIdAsync(Guid id, CancellationToken ct)
        => db.ProposteOrdine
             .Include("_righe")
             .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct)
        => db.ProposteOrdine.AnyAsync(p => p.Id == id, ct);

    public async Task AddAsync(PropostaOrdine p, CancellationToken ct)
        => await db.ProposteOrdine.AddAsync(p, ct);

    public void Update(PropostaOrdine p) => db.ProposteOrdine.Update(p);

    public void Remove(PropostaOrdine p) => db.ProposteOrdine.Remove(p);

    public Task<PropostaOrdine?> GetByOperatoreAttivaAsync(Guid operatoreId, CancellationToken ct)
        => db.ProposteOrdine
             .Include("_righe")
             .FirstOrDefaultAsync(p => p.OperatoreId == operatoreId
                 && (p.Stato == PropostaOrdine.StatoProposta.Bozza
                  || p.Stato == PropostaOrdine.StatoProposta.Completata), ct);

    public Task<PropostaOrdine?> GetByConfigurazioneAsync(Guid configurazioneId, CancellationToken ct)
        => db.ProposteOrdine
             .Include("_righe")
             .FirstOrDefaultAsync(p => p.ConfigurazioneId == configurazioneId, ct);

    public async Task<IReadOnlyList<PropostaOrdine>> GetByOperatoreAsync(Guid operatoreId, CancellationToken ct)
        => await db.ProposteOrdine
                   .Where(p => p.OperatoreId == operatoreId)
                   .OrderByDescending(p => p.DataCreazione)
                   .ToListAsync(ct);
}
