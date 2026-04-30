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

namespace SistemaF.Application.Anagrafica.Queries;

// ═══════════════════════════════════════════════════════════════════════════════
//  QUERIES — Anagrafica
// ═══════════════════════════════════════════════════════════════════════════════

// ── Fornitore ─────────────────────────────────────────────────────────────────

public sealed record GetFornitoriAttiviQuery : IRequest<IReadOnlyList<FornitoreDto>>;

public sealed class GetFornitoriAttiviHandler(IFornitoreRepository repo)
    : IRequestHandler<GetFornitoriAttiviQuery, IReadOnlyList<FornitoreDto>>
{
    public async Task<IReadOnlyList<FornitoreDto>> Handle(
        GetFornitoriAttiviQuery _, CancellationToken ct)
    {
        var lista = await repo.GetAttiviAsync(ct);
        return lista.Select(FornitoreDto.Da).ToList();
    }
}

public sealed record GetFornitoriPerTipoQuery(TipoFornitore Tipo)
    : IRequest<IReadOnlyList<FornitoreDto>>;

public sealed class GetFornitoriPerTipoHandler(IFornitoreRepository repo)
    : IRequestHandler<GetFornitoriPerTipoQuery, IReadOnlyList<FornitoreDto>>
{
    public async Task<IReadOnlyList<FornitoreDto>> Handle(
        GetFornitoriPerTipoQuery q, CancellationToken ct)
    {
        var lista = await repo.GetByTipoAsync(q.Tipo, ct);
        return lista.Select(FornitoreDto.Da).ToList();
    }
}

// ── ConfigurazioneEmissione ───────────────────────────────────────────────────

public sealed record GetConfigurazioniEmissioneQuery
    : IRequest<IReadOnlyList<ConfigurazioneEmissioneDto>>;

public sealed class GetConfigurazioniEmissioneHandler(
    IConfigurazioneEmissioneRepository repo)
    : IRequestHandler<GetConfigurazioniEmissioneQuery,
                      IReadOnlyList<ConfigurazioneEmissioneDto>>
{
    public async Task<IReadOnlyList<ConfigurazioneEmissioneDto>> Handle(
        GetConfigurazioniEmissioneQuery _, CancellationToken ct)
    {
        var lista = await repo.GetAttiveAsync(ct);
        return lista.Select(ConfigurazioneEmissioneDto.Da).ToList();
    }
}

public sealed record GetFornitoriConfigurazioneQuery(Guid ConfigurazioneId)
    : IRequest<IReadOnlyList<FornitoreDto>>;

public sealed class GetFornitoriConfigurazioneHandler(
    IConfigurazioneEmissioneRepository repo)
    : IRequestHandler<GetFornitoriConfigurazioneQuery, IReadOnlyList<FornitoreDto>>
{
    public async Task<IReadOnlyList<FornitoreDto>> Handle(
        GetFornitoriConfigurazioneQuery q, CancellationToken ct)
    {
        var lista = await repo.GetFornitoriAsync(q.ConfigurazioneId, ct);
        return lista.Select(FornitoreDto.Da).ToList();
    }
}

// ── Operatore — verifica credenziali ─────────────────────────────────────────

public sealed record VerificaCredenziliQuery(
    string Login,
    string PasswordHash) : IRequest<Result<OperatoreDto>>;

public sealed class VerificaCredenzialiHandler(IOperatoreRepository repo)
    : IRequestHandler<VerificaCredenziliQuery, Result<OperatoreDto>>
{
    public async Task<Result<OperatoreDto>> Handle(
        VerificaCredenziliQuery q, CancellationToken ct)
    {
        var op = await repo.GetByLoginAsync(q.Login, ct);
        if (op is null)
            return Result<OperatoreDto>.Fail("Login non trovato.", "LOGIN_NON_TROVATO");
        if (!op.VerificaPassword(q.PasswordHash))
            return Result<OperatoreDto>.Fail("Password errata.", "PASSWORD_ERRATA");
        return Result<OperatoreDto>.Ok(OperatoreDto.Da(op));
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  DTO
// ═══════════════════════════════════════════════════════════════════════════════

public sealed record FornitoreDto(
    Guid          Id,
    long          CodiceAnabase,
    string        RagioneSociale,
    string        Tipo,
    string?       PartitaIVA,
    string?       Email,
    string?       Telefono,
    string?       Localita,
    string?       Provincia,
    bool          IsMagazzino,
    bool          IsPreferenziale,
    int           PercentualeRipartizione,
    bool          IsAttivo)
{
    internal static FornitoreDto Da(Fornitore f) => new(
        f.Id, f.CodiceAnabase, f.RagioneSociale, f.Tipo.ToString(),
        f.PartitaIVA, f.Contatti.Email, f.Contatti.Telefono,
        f.Sede.Localita, f.Sede.Provincia,
        f.IsMagazzino, f.IsPreferenzialeDefault,
        f.PercentualeRipartizione, f.IsAttivo);
}

public sealed record OperatoreDto(
    Guid   Id,
    string Login,
    string NomeCognome,
    bool   IsAmministratore,
    bool   IsAttivo)
{
    internal static OperatoreDto Da(Operatore o) => new(
        o.Id, o.Login, o.NomeCognome, o.IsAmministratore, o.IsAttivo);
}

public sealed record ConfigurazioneEmissioneDto(
    Guid   Id,
    string Nome,
    string? Descrizione,
    bool   DaArchivio,
    bool   DaNecessita,
    bool   DaPrenotati,
    bool   DaSospesi,
    string TipoIndiceVendita,
    int    GiorniCopertura,
    bool   IsAttiva)
{
    internal static ConfigurazioneEmissioneDto Da(ConfigurazioneEmissione c) => new(
        c.Id, c.Nome, c.Descrizione,
        c.DaArchivio, c.DaNecessita, c.DaPrenotati, c.DaSospesi,
        c.TipoIndiceVendita.ToString(), c.GiorniCopertura, c.IsAttiva);
}
