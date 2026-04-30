using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaF.Integration.Federfarma;
using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Models;

namespace SistemaF.Application.Federfarma.Commands;

// ═══════════════════════════════════════════════════════════════════════════════
//  COMMANDS — Federfarma
// ═══════════════════════════════════════════════════════════════════════════════

// ── Richiedi Distinta DPC da WebService ───────────────────────────────────────

/// <summary>
/// Recupera la distinta DPC dal WebService Federfarma per il mese indicato.
/// Migrazione di getWEBDPC_Distinta.
/// </summary>
public sealed record RichiediDistintaDpcCommand(
    string Username,
    string Pin,
    string CodiceAsl,
    string CodiceFarmaciaAsl,
    int    Mese,
    int    Anno) : IRequest<Result<DistintaDpc>>;

public sealed class RichiediDistintaDpcValidator : AbstractValidator<RichiediDistintaDpcCommand>
{
    public RichiediDistintaDpcValidator()
    {
        RuleFor(c => c.Username).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Pin).NotEmpty().MaximumLength(30);
        RuleFor(c => c.CodiceAsl).NotEmpty().MaximumLength(10);
        RuleFor(c => c.CodiceFarmaciaAsl).NotEmpty().MaximumLength(10);
        RuleFor(c => c.Mese).InclusiveBetween(1, 12);
        RuleFor(c => c.Anno).InclusiveBetween(2000, 2099);
    }
}

public sealed class RichiediDistintaDpcHandler(
    IFederfarmaWebDpcClient      client,
    ILogger<RichiediDistintaDpcHandler> log)
    : IRequestHandler<RichiediDistintaDpcCommand, Result<DistintaDpc>>
{
    public async Task<Result<DistintaDpc>> Handle(
        RichiediDistintaDpcCommand cmd, CancellationToken ct)
    {
        try
        {
            var cred = FederfarmaCredenziali.Da(cmd.Username, cmd.Pin);
            var req  = FederfarmaRichiestaFur.Da(
                cmd.CodiceAsl, cmd.CodiceFarmaciaAsl, cmd.Mese, cmd.Anno);

            var distinta = await client.GetDistintaAsync(cred, req, ct);
            log.LogInformation("DPC distinta {M}/{A}: {N} ricette", cmd.Mese, cmd.Anno, distinta.NumeroRicette);
            return Result<DistintaDpc>.Ok(distinta);
        }
        catch (FederfarmaFaultException ex)
        {
            return Result<DistintaDpc>.Fail($"Federfarma DPC: [{ex.Codice}] {ex.Dettaglio}", "FEDERFARMA_FAULT");
        }
        catch (TimeoutException ex)
        {
            log.LogWarning("Timeout DPC: {Msg}", ex.Message);
            return Result<DistintaDpc>.Fail("Timeout nella chiamata al servizio DPC Federfarma.", "TIMEOUT");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Errore DPC GetDistinta");
            return Result<DistintaDpc>.Fail(ex.Message, "CONNESSIONE_FALLITA");
        }
    }
}

// ── Importa Distinta DPC da file XML locale ───────────────────────────────────

/// <summary>
/// Legge la distinta DPC da un file XML locale.
/// Migrazione di getXMLDPC_Distinta.
/// </summary>
public sealed record ImportaDistintaDpcDaXmlCommand(
    string PathFileXml) : IRequest<Result<DistintaDpc>>;

public sealed class ImportaDistintaDpcDaXmlHandler(
    IFederfarmaWebDpcClient client)
    : IRequestHandler<ImportaDistintaDpcDaXmlCommand, Result<DistintaDpc>>
{
    public async Task<Result<DistintaDpc>> Handle(
        ImportaDistintaDpcDaXmlCommand cmd, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(cmd.PathFileXml))
                return Result<DistintaDpc>.Fail(
                    $"File non trovato: {cmd.PathFileXml}", "FILE_NON_TROVATO");

            var distinta = await client.ParseXmlAsync(cmd.PathFileXml, ct);
            return Result<DistintaDpc>.Ok(distinta);
        }
        catch (Exception ex)
        {
            return Result<DistintaDpc>.Fail(ex.Message, "PARSING_FALLITO");
        }
    }
}

// ── Richiedi Competenza WebCare da WebService ─────────────────────────────────

/// <summary>
/// Recupera la competenza WebCare dal WebService Federfarma.
/// Migrazione di getWEBCARE_Competenza.
/// </summary>
public sealed record RichiediCompetenzaWebCareCommand(
    string Username,
    string Pin,
    string CodiceAsl,
    string CodiceFarmaciaAsl,
    int    Mese,
    int    Anno,
    string NomeAsl) : IRequest<Result<CompetenzaWebCare>>;

public sealed class RichiediCompetenzaWebCareValidator : AbstractValidator<RichiediCompetenzaWebCareCommand>
{
    public RichiediCompetenzaWebCareValidator()
    {
        RuleFor(c => c.Username).NotEmpty().MaximumLength(50);
        RuleFor(c => c.Pin).NotEmpty().MaximumLength(30);
        RuleFor(c => c.CodiceAsl).NotEmpty().MaximumLength(10);
        RuleFor(c => c.CodiceFarmaciaAsl).NotEmpty().MaximumLength(10);
        RuleFor(c => c.Mese).InclusiveBetween(1, 12);
        RuleFor(c => c.Anno).InclusiveBetween(2000, 2099);
        RuleFor(c => c.NomeAsl).NotEmpty().MaximumLength(60);
    }
}

public sealed class RichiediCompetenzaWebCareHandler(
    IFederfarmaWebCareClient     client,
    ILogger<RichiediCompetenzaWebCareHandler> log)
    : IRequestHandler<RichiediCompetenzaWebCareCommand, Result<CompetenzaWebCare>>
{
    public async Task<Result<CompetenzaWebCare>> Handle(
        RichiediCompetenzaWebCareCommand cmd, CancellationToken ct)
    {
        try
        {
            var cred = FederfarmaCredenziali.Da(cmd.Username, cmd.Pin);
            var req  = FederfarmaRichiestaFur.Da(
                cmd.CodiceAsl, cmd.CodiceFarmaciaAsl, cmd.Mese, cmd.Anno);

            var competenza = await client.GetCompetenzaAsync(cred, req, cmd.NomeAsl, ct);
            log.LogInformation("WebCare {M}/{A}: {N} movimenti, €{L:F2} lordo",
                cmd.Mese, cmd.Anno, competenza.NumeroMovimenti, competenza.ImportoLordo);
            return Result<CompetenzaWebCare>.Ok(competenza);
        }
        catch (FederfarmaFaultException ex)
        {
            return Result<CompetenzaWebCare>.Fail(
                $"Federfarma WebCare: [{ex.Codice}] {ex.Dettaglio}", "FEDERFARMA_FAULT");
        }
        catch (TimeoutException ex)
        {
            log.LogWarning("Timeout WebCare: {Msg}", ex.Message);
            return Result<CompetenzaWebCare>.Fail(
                "Timeout nella chiamata al servizio WebCare Federfarma.", "TIMEOUT");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Errore WebCare GetCompetenza");
            return Result<CompetenzaWebCare>.Fail(ex.Message, "CONNESSIONE_FALLITA");
        }
    }
}

// ── Importa Competenza WebCare da file XML ────────────────────────────────────

public sealed record ImportaCompetenzaWebCareDaXmlCommand(
    string PathFileXml) : IRequest<Result<CompetenzaWebCare>>;

public sealed class ImportaCompetenzaWebCareDaXmlHandler(
    IFederfarmaWebCareClient client)
    : IRequestHandler<ImportaCompetenzaWebCareDaXmlCommand, Result<CompetenzaWebCare>>
{
    public async Task<Result<CompetenzaWebCare>> Handle(
        ImportaCompetenzaWebCareDaXmlCommand cmd, CancellationToken ct)
    {
        try
        {
            if (!File.Exists(cmd.PathFileXml))
                return Result<CompetenzaWebCare>.Fail(
                    $"File non trovato: {cmd.PathFileXml}", "FILE_NON_TROVATO");

            var competenza = await client.ParseXmlAsync(cmd.PathFileXml, ct);
            return Result<CompetenzaWebCare>.Ok(competenza);
        }
        catch (Exception ex)
        {
            return Result<CompetenzaWebCare>.Fail(ex.Message, "PARSING_FALLITO");
        }
    }
}
