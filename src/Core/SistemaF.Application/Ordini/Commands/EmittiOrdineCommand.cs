using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaF.Domain.Entities;
using SistemaF.Domain.Interfaces;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Application.Ordini.Commands;

// ============================================================
//  COMMAND — Emissione ordine farmacia → grossista
//  Sostituisce CSFOrdEmissione.dll (VB6)
// ============================================================

public sealed record EmittiOrdineCommand(
    string CodiceGrossista,
    string RagioneSocialeGrossista,
    TipoOrdine Tipo,
    Guid OperatoreId,
    IReadOnlyList<RigaOrdineDto> Righe) : IRequest<OrdineEmmessoResult>;

public sealed record RigaOrdineDto(Guid ProdottoId, int Quantita);

public sealed record OrdineEmmessoResult(
    bool Successo,
    string? NumeroOrdine,
    string? Errore);

// ---- Validator ---------------------------------------------------------------

public sealed class EmittiOrdineCommandValidator : AbstractValidator<EmittiOrdineCommand>
{
    public EmittiOrdineCommandValidator()
    {
        RuleFor(c => c.CodiceGrossista)
            .NotEmpty().WithMessage("Il codice grossista è obbligatorio.")
            .MaximumLength(20);

        RuleFor(c => c.Righe)
            .NotEmpty().WithMessage("L'ordine deve contenere almeno una riga.")
            .Must(r => r.All(x => x.Quantita > 0))
            .WithMessage("Tutte le quantità devono essere positive.");

        RuleFor(c => c.OperatoreId)
            .NotEqual(Guid.Empty).WithMessage("L'operatore non è valido.");
    }
}

// ---- Handler -----------------------------------------------------------------

public sealed class EmittiOrdineCommandHandler(
    IUnitOfWork uow,
    ILogger<EmittiOrdineCommandHandler> logger)
    : IRequestHandler<EmittiOrdineCommand, OrdineEmmessoResult>
{
    public async Task<OrdineEmmessoResult> Handle(
        EmittiOrdineCommand request, CancellationToken ct)
    {
        try
        {
            var ordine = Ordine.Crea(
                request.CodiceGrossista,
                request.RagioneSocialeGrossista,
                request.Tipo,
                request.OperatoreId);

            foreach (var riga in request.Righe)
            {
                var prodotto = await uow.Prodotti.GetByIdAsync(riga.ProdottoId, ct)
                    ?? throw new DomainException($"Prodotto {riga.ProdottoId} non trovato.");

                ordine.AggiungiRiga(prodotto, riga.Quantita, prodotto.PrezzoVendita);
            }

            ordine.Emetti();

            await uow.Ordini.AddAsync(ordine, ct);
            await uow.SaveChangesAsync(ct);

            logger.LogInformation("Ordine {Numero} emesso ({Confezioni} conf.) → {Grossista}",
                ordine.NumeroOrdine, ordine.TotaleConfezioni, request.CodiceGrossista);

            return new OrdineEmmessoResult(true, ordine.NumeroOrdine, null);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Emissione ordine fallita: {Messaggio}", ex.Message);
            return new OrdineEmmessoResult(false, null, ex.Message);
        }
    }
}
