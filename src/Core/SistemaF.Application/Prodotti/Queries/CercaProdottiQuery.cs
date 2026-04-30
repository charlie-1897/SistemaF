using MediatR;
using SistemaF.Domain.Interfaces;

namespace SistemaF.Application.Prodotti.Queries;

// ============================================================
//  QUERY — Ricerca prodotti
//  Sostituisce la logica di ricerca in CSFRicerca.dll (VB6)
//  con i tipi CSFMINISTERIALE, CSFCODICEEAN, CSFDESCRIZIONE etc.
// ============================================================

public sealed record CercaProdottiQuery(string Termine, int Limit = 30) : IRequest<IEnumerable<ProdottoDto>>;

public sealed class CercaProdottiQueryHandler(IProdottoRepository repo)
    : IRequestHandler<CercaProdottiQuery, IEnumerable<ProdottoDto>>
{
    public async Task<IEnumerable<ProdottoDto>> Handle(
        CercaProdottiQuery request, CancellationToken ct)
    {
        var prodotti = await repo.CercaAsync(request.Termine, request.Limit, ct);
        return prodotti.Select(ProdottoDto.DaEntita);
    }
}

// ============================================================
//  QUERY — Dettaglio singolo prodotto
// ============================================================

public sealed record GetProdottoByIdQuery(Guid Id) : IRequest<ProdottoDto?>;

public sealed class GetProdottoByIdQueryHandler(IProdottoRepository repo)
    : IRequestHandler<GetProdottoByIdQuery, ProdottoDto?>
{
    public async Task<ProdottoDto?> Handle(GetProdottoByIdQuery request, CancellationToken ct)
    {
        var prodotto = await repo.GetByIdAsync(request.Id, ct);
        return prodotto is null ? null : ProdottoDto.DaEntita(prodotto);
    }
}

// ============================================================
//  DTO — Proiezione lato lettura
// ============================================================

public sealed record ProdottoDto(
    Guid   Id,
    string CodiceFarmaco,
    string? CodiceEAN,
    string? CodiceATC,
    string Descrizione,
    string Classe,
    decimal PrezzoVendita,
    decimal? PrezzoSSN,
    int GiacenzaAttuale,
    int ScortaMinima,
    bool IsMutuabile,
    bool IsStupefacente)
{
    internal static ProdottoDto DaEntita(Domain.Entities.Prodotto p) => new(
        p.Id,
        p.CodiceFarmaco.Valore,
        p.CodiceEAN?.Valore,
        p.CodiceATC,
        p.Descrizione,
        p.Classe.ToString(),
        p.PrezzoVendita.Valore,
        p.PrezzoSSN?.Valore,
        p.GiacenzaAttuale,
        p.ScortaMinima,
        p.IsMutuabile,
        p.IsStupefacente);
}
