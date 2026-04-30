using Microsoft.Extensions.Logging;

namespace SistemaF.Integration.Federfarma;

/// <summary>
/// Client per i web service di Federfarma Lombardia (WebDPC e WebCare).
/// Sostituisce CSFWSENET.dll del VB6 che usava i WSDL:
///   - it.lombardia.federfarma.webdpc → FurService.wsdl
///   - it.lombardia.federfarma.webcare → WebServiceFUR.wsdl
///
/// Per rigenerare il proxy dal WSDL:
///   dotnet-svcutil FurService.wsdl --outputDir Federfarma/Generated
/// </summary>
public interface IFederfarmaService
{
    /// <summary>Invia una riga DPC (Distribuzione Per Conto) al servizio regionale.</summary>
    Task<InvioDpcResult> InviaDpcAsync(RigaDpc riga, CancellationToken ct = default);

    /// <summary>Recupera lo stato delle ricette DPC inviate.</summary>
    Task<IEnumerable<StatoDpc>> GetStatoRicetteAsync(DateTime da, DateTime a, CancellationToken ct = default);
}

public sealed record RigaDpc(
    string CodiceFarmaco,
    string CodiceFiscalePaziente,
    DateTime DataErogazione,
    int Quantita,
    decimal ImportoSSN);

public sealed record InvioDpcResult(bool Successo, string? CodiceProtocollo, string? Errore);

public sealed record StatoDpc(string CodiceProtocollo, string Stato, DateTime DataAggiornamento);

// ---- Implementazione --------------------------------------------------------

public sealed class FederfarmaService(
    HttpClient httpClient,
    ILogger<FederfarmaService> logger) : IFederfarmaService
{
    // L'URL endpoint viene configurato in appsettings.json:
    //   "Federfarma:WebDpcEndpoint": "https://webdpc.federfarma.it/..."
    // e iniettato tramite IOptions<FederfarmaOptions>.

    public async Task<InvioDpcResult> InviaDpcAsync(RigaDpc riga, CancellationToken ct)
    {
        // TODO: costruire il messaggio SOAP secondo il WSDL FurService.wsdl
        // e inviarlo tramite System.ServiceModel.Http.
        // Il proxy WCF va generato con:
        //   dotnet-svcutil <url-wsdl> --outputDir Federfarma/Generated

        logger.LogInformation("Invio DPC codice farmaco {Codice} data {Data}",
            riga.CodiceFarmaco, riga.DataErogazione);

        // Placeholder per il corpo SOAP reale
        await Task.Delay(10, ct);
        return new InvioDpcResult(false, null, "Integrazione SOAP da completare con proxy generato da WSDL.");
    }

    public Task<IEnumerable<StatoDpc>> GetStatoRicetteAsync(
        DateTime da, DateTime a, CancellationToken ct) =>
        Task.FromResult(Enumerable.Empty<StatoDpc>());
}
