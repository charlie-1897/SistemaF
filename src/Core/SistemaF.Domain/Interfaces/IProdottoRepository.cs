using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Specifications;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Interfaces;

/// <summary>
/// Contratto di persistenza per il Prodotto aggregate.
/// Ogni metodo corrisponde a una query che nel VB6 era una stringa SQL
/// hardcoded in un modulo .bas o direttamente in una Form.
/// </summary>
public interface IProdottoRepository : IRepository<Prodotto>
{
    // ── Ricerche per chiave ───────────────────────────────────────────────────

    Task<Prodotto?> GetByCodiceFarmacoAsync(CodiceProdotto codice, CancellationToken ct = default);
    Task<Prodotto?> GetByCodiceEANAsync(CodiceEAN ean, CancellationToken ct = default);

    // ── Ricerche per Specification ────────────────────────────────────────────

    Task<IReadOnlyList<Prodotto>> CercaAsync(
        Specification<Prodotto> spec,
        int                     limit  = 50,
        int                     offset = 0,
        CancellationToken       ct     = default);

    Task<int> ContaAsync(Specification<Prodotto> spec, CancellationToken ct = default);

    // ── Query di business ─────────────────────────────────────────────────────

    Task<IReadOnlyList<Prodotto>> GetSottoscortaAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Prodotto>> GetInScadenzaAsync(int giorni = 90, CancellationToken ct = default);
    Task<IReadOnlyList<Prodotto>> GetInvendibiliAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Prodotto>> GetSegnalatiAsync(CancellationToken ct = default);

    // ── Esistenza ─────────────────────────────────────────────────────────────

    Task<bool> EsisteCodiceFarmacoAsync(CodiceProdotto codice, CancellationToken ct = default);
    Task<bool> EsisteCodiceEANAsync(CodiceEAN ean, CancellationToken ct = default);

    // ── Lista completa ────────────────────────────────────────────────────────

    Task<IReadOnlyList<Prodotto>> GetAllAsync(CancellationToken ct = default);
}
