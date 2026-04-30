using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.Interfaces;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Application.Prodotti.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
//  COMMANDS — Modulo Prodotto
//  Lato scrittura (CQRS): modificano lo stato, non restituiscono dati.
// ═══════════════════════════════════════════════════════════════════════════════

// ── CreaProdotto ──────────────────────────────────────────────────────────────

public sealed record CreaProdottoCommand(
    string CodiceFarmaco,
    string Descrizione,
    int    ClasseId,
    int    CategoriaRicettaId,
    decimal PrezzoVendita,
    int    AliquotaIVA,
    string? CodiceEAN,
    string? CodiceATC,
    Guid    OperatoreId) : IRequest<Result<Guid>>;

public sealed class CreaProdottoValidator : AbstractValidator<CreaProdottoCommand>
{
    public CreaProdottoValidator()
    {
        RuleFor(c => c.CodiceFarmaco)
            .NotEmpty()
            .Length(1, 9)
            .Matches(@"^\d+$").WithMessage("Il codice farmaco deve contenere solo cifre.");

        RuleFor(c => c.Descrizione)
            .NotEmpty().MaximumLength(200);

        RuleFor(c => c.PrezzoVendita)
            .GreaterThanOrEqualTo(0);

        RuleFor(c => c.AliquotaIVA)
            .Must(a => new[] {4, 10, 22}.Contains(a))
            .WithMessage("Aliquota IVA deve essere 4, 10 o 22.");

        RuleFor(c => c.OperatoreId)
            .NotEqual(Guid.Empty);
    }
}

public sealed class CreaProdottoHandler(
    IProdottoRepository repo,
    IUnitOfWork         uow,
    ILogger<CreaProdottoHandler> log)
    : IRequestHandler<CreaProdottoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreaProdottoCommand cmd, CancellationToken ct)
    {
        try
        {
            var codice = CodiceProdotto.Da(cmd.CodiceFarmaco);

            if (await repo.EsisteCodiceFarmacoAsync(codice, ct))
                return Result<Guid>.Fail(
                    $"Esiste già un prodotto con codice {codice}.", "DUPLICATE");

            var classe           = Enumeration.FromIdOrThrow<ClasseFarmaco>(cmd.ClasseId);
            var categoriaRicetta = Enumeration.FromIdOrThrow<CategoriaRicetta>(cmd.CategoriaRicettaId);
            var prezzo           = Prezzo.Di(cmd.PrezzoVendita, cmd.AliquotaIVA);

            var prodotto = Prodotto.Crea(codice, cmd.Descrizione, classe, categoriaRicetta,
                                         prezzo, cmd.OperatoreId);

            if (!string.IsNullOrWhiteSpace(cmd.CodiceEAN))
            {
                var eanResult = CodiceEAN.TryCreate(cmd.CodiceEAN);
                if (eanResult.IsFailure)
                    return Result<Guid>.Fail(eanResult.ErrorMessage, eanResult.ErrorCode);

                if (await repo.EsisteCodiceEANAsync(eanResult.Value, ct))
                    return Result<Guid>.Fail(
                        $"Esiste già un prodotto con EAN {cmd.CodiceEAN}.", "DUPLICATE_EAN");

                prodotto.ImpostaCodici(eanResult.Value,
                    string.IsNullOrWhiteSpace(cmd.CodiceATC)
                        ? null : CodiceATC.Da(cmd.CodiceATC),
                    null, null);
            }

            await repo.AddAsync(prodotto, ct);
            await uow.SaveChangesAsync(ct);

            log.LogInformation("Prodotto {Codice} '{Desc}' creato da operatore {Op}",
                codice, cmd.Descrizione, cmd.OperatoreId);

            return Result<Guid>.Ok(prodotto.Id);
        }
        catch (DomainException ex)
        {
            log.LogWarning("Creazione prodotto fallita: {Msg}", ex.Message);
            return Result<Guid>.Fail(ex);
        }
    }
}

// ── AggiornaPrezzoProdotto ────────────────────────────────────────────────────

public sealed record AggiornaPrezzoProdottoCommand(
    Guid    ProdottoId,
    decimal NuovoPrezzo,
    int     AliquotaIVA,
    Guid    OperatoreId) : IRequest<Result>;

public sealed class AggiornaPrezzoProdottoValidator
    : AbstractValidator<AggiornaPrezzoProdottoCommand>
{
    public AggiornaPrezzoProdottoValidator()
    {
        RuleFor(c => c.ProdottoId).NotEqual(Guid.Empty);
        RuleFor(c => c.NuovoPrezzo).GreaterThanOrEqualTo(0);
        RuleFor(c => c.AliquotaIVA)
            .Must(a => new[] {4, 10, 22}.Contains(a))
            .WithMessage("Aliquota IVA deve essere 4, 10 o 22.");
    }
}

public sealed class AggiornaPrezzoProdottoHandler(
    IProdottoRepository repo,
    IUnitOfWork         uow,
    ILogger<AggiornaPrezzoProdottoHandler> log)
    : IRequestHandler<AggiornaPrezzoProdottoCommand, Result>
{
    public async Task<Result> Handle(AggiornaPrezzoProdottoCommand cmd, CancellationToken ct)
    {
        try
        {
            var prodotto = await repo.GetByIdOrThrowAsync(cmd.ProdottoId, ct);
            prodotto.AggiornaPrezzo(Prezzo.Di(cmd.NuovoPrezzo, cmd.AliquotaIVA), cmd.OperatoreId);
            repo.Update(prodotto);
            await uow.SaveChangesAsync(ct);

            log.LogInformation("Prezzo prodotto {Id} aggiornato a {P} da {Op}",
                cmd.ProdottoId, cmd.NuovoPrezzo, cmd.OperatoreId);
            return Result.Ok();
        }
        catch (DomainException ex)
        {
            log.LogWarning("Aggiornamento prezzo fallito: {Msg}", ex.Message);
            return Result.Fail(ex);
        }
    }
}

// ── VariaGiacenza ─────────────────────────────────────────────────────────────

public sealed record VariaGiacenzaCommand(
    Guid                       ProdottoId,
    TipoDepositoMagazzino      Deposito,
    ModalitaVariazioneGiacenza Modalita,
    int                        Valore,
    TipoModuloRettifica        Modulo,
    Guid                       OperatoreId) : IRequest<Result>;

public sealed class VariaGiacenzaValidator : AbstractValidator<VariaGiacenzaCommand>
{
    public VariaGiacenzaValidator()
    {
        RuleFor(c => c.ProdottoId).NotEqual(Guid.Empty);
        RuleFor(c => c.Valore).GreaterThanOrEqualTo(0);
        RuleFor(c => c.OperatoreId).NotEqual(Guid.Empty);
    }
}

public sealed class VariaGiacenzaHandler(
    IProdottoRepository repo,
    IUnitOfWork         uow,
    ILogger<VariaGiacenzaHandler> log)
    : IRequestHandler<VariaGiacenzaCommand, Result>
{
    public async Task<Result> Handle(VariaGiacenzaCommand cmd, CancellationToken ct)
    {
        try
        {
            var prodotto = await repo.GetByIdOrThrowAsync(cmd.ProdottoId, ct);

            if (cmd.Deposito == TipoDepositoMagazzino.Esposizione)
                prodotto.VariaGiacenzaEsposizione(cmd.Modalita, cmd.Valore, cmd.Modulo, cmd.OperatoreId);
            else
                prodotto.VariaGiacenzaMagazzino(cmd.Modalita, cmd.Valore, cmd.Modulo, cmd.OperatoreId);

            repo.Update(prodotto);
            await uow.SaveChangesAsync(ct);

            log.LogInformation(
                "Giacenza {Dep} prodotto {Id} variata ({Mod}) a {Val} da {Op}",
                cmd.Deposito, cmd.ProdottoId, cmd.Modalita, cmd.Valore, cmd.OperatoreId);
            return Result.Ok();
        }
        catch (DomainException ex)
        {
            log.LogWarning("Variazione giacenza fallita: {Msg}", ex.Message);
            return Result.Fail(ex);
        }
    }
}

// ── AggiungLotto ──────────────────────────────────────────────────────────────

public sealed record AggiungLottoCommand(
    Guid      ProdottoId,
    string    Lotto,
    DateOnly  DataScadenza,
    int       Quantita,
    Guid      OperatoreId) : IRequest<Result>;

public sealed class AggiungLottoValidator : AbstractValidator<AggiungLottoCommand>
{
    public AggiungLottoValidator()
    {
        RuleFor(c => c.Lotto).NotEmpty().MaximumLength(30);
        RuleFor(c => c.Quantita).GreaterThan(0);
        RuleFor(c => c.DataScadenza)
            .Must(d => d > DateOnly.FromDateTime(DateTime.Today))
            .WithMessage("La data di scadenza deve essere futura.");
    }
}

public sealed class AggiungLottoHandler(
    IProdottoRepository repo,
    IUnitOfWork         uow)
    : IRequestHandler<AggiungLottoCommand, Result>
{
    public async Task<Result> Handle(AggiungLottoCommand cmd, CancellationToken ct)
    {
        try
        {
            var prodotto = await repo.GetByIdOrThrowAsync(cmd.ProdottoId, ct);
            var lotto    = CodiceLotto.Da(cmd.Lotto);
            prodotto.AggiungLotto(lotto, cmd.DataScadenza, cmd.Quantita, cmd.OperatoreId);
            repo.Update(prodotto);
            await uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex);
        }
    }
}
