using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Interfaces;

namespace SistemaF.Application.Ordini.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
//  COMMANDS — Modulo Ordine
// ═══════════════════════════════════════════════════════════════════════════════

// ── CreaPropostaOrdine ────────────────────────────────────────────────────────

public sealed record CreaPropostaOrdineCommand(
    Guid   OperatoreId,
    Guid   ConfigurazioneId,
    string NomeEmissione,
    bool   DaArchivio,
    bool   DaNecessita,
    bool   DaPrenotati,
    bool   DaSospesi) : IRequest<Result<Guid>>;

public sealed class CreaPropostaOrdineValidator : AbstractValidator<CreaPropostaOrdineCommand>
{
    public CreaPropostaOrdineValidator()
    {
        RuleFor(c => c.OperatoreId).NotEqual(Guid.Empty);
        RuleFor(c => c.ConfigurazioneId).NotEqual(Guid.Empty);
        RuleFor(c => c.NomeEmissione).NotEmpty().MaximumLength(100);
        RuleFor(c => c).Must(c => c.DaArchivio || c.DaNecessita || c.DaPrenotati || c.DaSospesi)
            .WithMessage("Almeno una fonte deve essere abilitata.");
    }
}

public sealed class CreaPropostaOrdineHandler(
    IPropostaOrdineRepository repo,
    IUnitOfWork               uow,
    ILogger<CreaPropostaOrdineHandler> log)
    : IRequestHandler<CreaPropostaOrdineCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreaPropostaOrdineCommand cmd, CancellationToken ct)
    {
        try
        {
            // Verifica se esiste già una proposta attiva per questo operatore
            var attiva = await repo.GetByOperatoreAttivaAsync(cmd.OperatoreId, ct);
            if (attiva is not null)
                return Result<Guid>.Fail(
                    "Esiste già una proposta in lavorazione per questo operatore. " +
                    "Completare o annullare quella esistente prima di iniziarne una nuova.",
                    "PROPOSTA_GIA_ATTIVA");

            var proposta = PropostaOrdine.Crea(cmd.OperatoreId, cmd.ConfigurazioneId, cmd.NomeEmissione);
            proposta.ImpostaFonti(cmd.DaArchivio, cmd.DaNecessita, cmd.DaPrenotati, cmd.DaSospesi);

            await repo.AddAsync(proposta, ct);
            await uow.SaveChangesAsync(ct);

            log.LogInformation("Proposta ordine {Id} creata da operatore {Op}",
                proposta.Id, cmd.OperatoreId);
            return Result<Guid>.Ok(proposta.Id);
        }
        catch (DomainException ex)
        {
            return Result<Guid>.Fail(ex);
        }
    }
}

// ── AggiungiProdottoAProposta ──────────────────────────────────────────────────

public sealed record AggiungiProdottoProposta(
    Guid          PropostaId,
    Guid          ProdottoId,
    int           Quantita,
    FonteAggiunta Fonte,
    int           IndiceFornitore,
    Guid          OperatoreId) : IRequest<Result>;

public sealed class AggiungiProdottoPropostaValidator : AbstractValidator<AggiungiProdottoProposta>
{
    public AggiungiProdottoPropostaValidator()
    {
        RuleFor(c => c.PropostaId).NotEqual(Guid.Empty);
        RuleFor(c => c.ProdottoId).NotEqual(Guid.Empty);
        RuleFor(c => c.Quantita).GreaterThan(0);
        RuleFor(c => c.IndiceFornitore).InclusiveBetween(0, PropostaRiga.MaxFornitori);
    }
}

public sealed class AggiungiProdottoPropostaHandler(
    IPropostaOrdineRepository propostaRepo,
    IProdottoRepository       prodottoRepo,
    IUnitOfWork               uow)
    : IRequestHandler<AggiungiProdottoProposta, Result>
{
    public async Task<Result> Handle(AggiungiProdottoProposta cmd, CancellationToken ct)
    {
        try
        {
            var proposta = await propostaRepo.GetByIdOrThrowAsync(cmd.PropostaId, ct);
            var prodotto = await prodottoRepo.GetByIdOrThrowAsync(cmd.ProdottoId, ct);

            proposta.AggiungiProdottoManuale(
                cmd.ProdottoId,
                prodotto.CodiceFarmaco,
                prodotto.Descrizione,
                cmd.Quantita,
                cmd.Fonte,
                cmd.IndiceFornitore == 0 ? 1 : cmd.IndiceFornitore);

            propostaRepo.Update(proposta);
            await uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex);
        }
    }
}

// ── EseguiPipelineEmissione ────────────────────────────────────────────────────

public sealed record EseguiPipelineEmissioneCommand(
    Guid   PropostaId,
    Guid   OperatoreId) : IRequest<Result<RiepilogoElaborazione>>;

public sealed class EseguiPipelineEmissioneHandler(
    IPropostaOrdineRepository propostaRepo,
    EmissioneOrdineService    pipeline,
    IUnitOfWork               uow,
    ILogger<EseguiPipelineEmissioneHandler> log)
    : IRequestHandler<EseguiPipelineEmissioneCommand, Result<RiepilogoElaborazione>>
{
    public async Task<Result<RiepilogoElaborazione>> Handle(
        EseguiPipelineEmissioneCommand cmd, CancellationToken ct)
    {
        try
        {
            var proposta = await propostaRepo.GetByIdOrThrowAsync(cmd.PropostaId, ct);

            Guard.AgainstFalse(proposta.OperatoreId == cmd.OperatoreId,
                "EseguiPipeline", "Solo l'operatore che ha creato la proposta può eseguire la pipeline.");

            var riepilogo = await pipeline.EseguiAsync(proposta, cancellationToken: ct);

            propostaRepo.Update(proposta);
            await uow.SaveChangesAsync(ct);

            log.LogInformation(
                "Pipeline emissione completata: {N} prodotti in ordine su {E} esaminati",
                riepilogo.NumeroProdottiInOrdine, riepilogo.NumeroProdottiEsaminati);

            return Result<RiepilogoElaborazione>.Ok(riepilogo);
        }
        catch (DomainException ex)
        {
            return Result<RiepilogoElaborazione>.Fail(ex);
        }
    }
}

// ── EmittiOrdine (conferma proposta → crea Ordine) ────────────────────────────

public sealed record EmittiOrdineCommand(
    Guid   PropostaId,
    Guid   FornitoreId,
    string RagioneSocialeFornitore,
    string? Note,
    Guid   OperatoreId) : IRequest<Result<Guid>>;

public sealed class EmittiOrdineValidator : AbstractValidator<EmittiOrdineCommand>
{
    public EmittiOrdineValidator()
    {
        RuleFor(c => c.PropostaId).NotEqual(Guid.Empty);
        RuleFor(c => c.FornitoreId).NotEqual(Guid.Empty);
        RuleFor(c => c.RagioneSocialeFornitore).NotEmpty().MaximumLength(100);
    }
}

public sealed class EmittiOrdineHandler(
    IPropostaOrdineRepository propostaRepo,
    IOrdineRepository         ordineRepo,
    IUnitOfWork               uow,
    ILogger<EmittiOrdineHandler> log)
    : IRequestHandler<EmittiOrdineCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(EmittiOrdineCommand cmd, CancellationToken ct)
    {
        try
        {
            var proposta = await propostaRepo.GetByIdOrThrowAsync(cmd.PropostaId, ct);

            Guard.AgainstFalse(
                proposta.Stato == PropostaOrdine.StatoProposta.Completata,
                "EmittiOrdine",
                "La proposta deve essere completata prima di emettere l'ordine. " +
                "Eseguire prima la pipeline di emissione.");

            var fornitoreInfo = proposta.Fornitori.FirstOrDefault(f => f.FornitoreId == cmd.FornitoreId)
                ?? throw new EntityNotFoundException(nameof(InfoFornitore), nameof(cmd.FornitoreId),
                    cmd.FornitoreId);

            var numero  = await ordineRepo.GeneraNumeroProgressivoAsync(DateTime.Today.Year, ct);
            var ordine  = Ordine.DaProposta(numero, proposta, fornitoreInfo,
                cmd.RagioneSocialeFornitore, cmd.Note);

            proposta.MarcaEmessa(ordine.Id);

            await ordineRepo.AddAsync(ordine, ct);
            propostaRepo.Update(proposta);
            await uow.SaveChangesAsync(ct);

            log.LogInformation("Ordine {Num} emesso verso {Fornitore}, {N} righe, €{Imp:F2}",
                numero, cmd.RagioneSocialeFornitore, ordine.NumeroRighe, ordine.ImportoTotale);

            return Result<Guid>.Ok(ordine.Id);
        }
        catch (DomainException ex)
        {
            log.LogWarning("Emissione ordine fallita: {Msg}", ex.Message);
            return Result<Guid>.Fail(ex);
        }
    }
}

// ── AnnullaOrdine ─────────────────────────────────────────────────────────────

public sealed record AnnullaOrdineCommand(
    Guid   OrdineId,
    string Motivazione,
    Guid   OperatoreId) : IRequest<Result>;

public sealed class AnnullaOrdineHandler(
    IOrdineRepository ordineRepo,
    IUnitOfWork       uow)
    : IRequestHandler<AnnullaOrdineCommand, Result>
{
    public async Task<Result> Handle(AnnullaOrdineCommand cmd, CancellationToken ct)
    {
        try
        {
            var ordine = await ordineRepo.GetByIdOrThrowAsync(cmd.OrdineId, ct);
            ordine.Annulla(cmd.Motivazione, cmd.OperatoreId);
            ordineRepo.Update(ordine);
            await uow.SaveChangesAsync(ct);
            return Result.Ok();
        }
        catch (DomainException ex)
        {
            return Result.Fail(ex);
        }
    }
}
