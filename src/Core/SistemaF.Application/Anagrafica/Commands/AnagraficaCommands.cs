using FluentValidation;
using MediatR;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Application.Anagrafica.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
//  COMMANDS — Anagrafica
// ═══════════════════════════════════════════════════════════════════════════════

// ── Fornitore ─────────────────────────────────────────────────────────────────

public sealed record CreaFornitoreCommand(
    string        RagioneSociale,
    TipoFornitore Tipo,
    string?       PartitaIVA    = null,
    string?       CodiceFiscale = null,
    string?       Indirizzo     = null,
    string?       Cap           = null,
    string?       Localita      = null,
    string?       Provincia     = null,
    string?       Telefono      = null,
    string?       Email         = null,
    bool          IsMagazzino   = false) : IRequest<Result<Guid>>;

public sealed class CreaFornitoreValidator : AbstractValidator<CreaFornitoreCommand>
{
    public CreaFornitoreValidator()
    {
        RuleFor(c => c.RagioneSociale).NotEmpty().MaximumLength(150);
        RuleFor(c => c.PartitaIVA).MaximumLength(16).When(c => c.PartitaIVA is not null);
        RuleFor(c => c.Email).EmailAddress().When(c => c.Email is not null);
    }
}

public sealed class CreaFornitoreHandler(
    IFornitoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreaFornitoreCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreaFornitoreCommand cmd, CancellationToken ct)
    {
        try
        {
            var f = Fornitore.Crea(
                cmd.RagioneSociale, cmd.Tipo,
                cmd.PartitaIVA, cmd.CodiceFiscale, cmd.IsMagazzino);

            if (cmd.Indirizzo is not null)
                f.ImpostaSede(
                    IndirizzoPosta.Da(cmd.Indirizzo, cmd.Cap ?? "",
                        cmd.Localita ?? "", cmd.Provincia ?? ""),
                    ContattoTelefonico.Da(cmd.Telefono, email: cmd.Email));

            await repo.AddAsync(f, ct);
            await uow.SaveChangesAsync(ct);
            return Result<Guid>.Ok(f.Id);
        }
        catch (DomainException ex) { return Result<Guid>.Fail(ex); }
    }
}

// ── Operatore ─────────────────────────────────────────────────────────────────

public sealed record CreaOperatoreCommand(
    string Login,
    string NomeCognome,
    string PasswordHash,
    bool   IsAmministratore = false) : IRequest<Result<Guid>>;

public sealed class CreaOperatoreValidator : AbstractValidator<CreaOperatoreCommand>
{
    public CreaOperatoreValidator()
    {
        RuleFor(c => c.Login).NotEmpty().MaximumLength(50)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Login può contenere solo lettere, cifre, punti, trattini.");
        RuleFor(c => c.NomeCognome).NotEmpty().MaximumLength(100);
        RuleFor(c => c.PasswordHash).NotEmpty().MinimumLength(32);
    }
}

public sealed class CreaOperatoreHandler(
    IOperatoreRepository repo, IUnitOfWork uow)
    : IRequestHandler<CreaOperatoreCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreaOperatoreCommand cmd, CancellationToken ct)
    {
        try
        {
            // Verifica unicità login
            var esistente = await repo.GetByLoginAsync(cmd.Login, ct);
            if (esistente is not null)
                return Result<Guid>.Fail(
                    $"Login '{cmd.Login}' già in uso.", "LOGIN_DUPLICATO");

            var o = Operatore.Crea(
                cmd.Login, cmd.NomeCognome,
                cmd.PasswordHash, cmd.IsAmministratore);

            await repo.AddAsync(o, ct);
            await uow.SaveChangesAsync(ct);
            return Result<Guid>.Ok(o.Id);
        }
        catch (DomainException ex) { return Result<Guid>.Fail(ex); }
    }
}

