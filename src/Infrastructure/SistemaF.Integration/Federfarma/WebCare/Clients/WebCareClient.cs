using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Models;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Integration.Federfarma.WebCare.Clients;

public sealed class FederfarmaWebCareClient(
    HttpClient               httpClient,
    FederfarmaConfiguration  config,
    WebCareXmlParser         parser,
    ILogger<FederfarmaWebCareClient> logger)
    : IFederfarmaWebCareClient
{
    public async Task<CompetenzaWebCare> GetCompetenzaAsync(
        FederfarmaCredenziali  credenziali,
        FederfarmaRichiestaFur richiesta,
        string                 nomeAsl,
        CancellationToken      ct = default)
    {
        if (string.IsNullOrWhiteSpace(nomeAsl))
            throw new ArgumentException("Nome ASL obbligatorio.", nameof(nomeAsl));

        logger.LogInformation(
            "WebCare GetCompetenza: ASL={Asl}({NomeAsl}) Farm={Farm} {Mese}/{Anno}",
            richiesta.CodiceAsl, nomeAsl, richiesta.CodiceFarmaciaAsl, richiesta.Mese, richiesta.Anno);

        var endpoint = config.WebCareEndpointUrl(nomeAsl);
        var envelope = SoapEnvelopeBuilder.BuildWebCareGetCompetenzaEnvelope(credenziali, richiesta);
        var content  = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{config.WebCareSoapAction}\"");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content,
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("text/xml") } }
        };

        if (config.TimeoutSecondi > 0)
            httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSecondi);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, ct);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Timeout ({config.TimeoutSecondi}s) durante chiamata WebCare GetCompetenza.", ex);
        }

        var soapXml = await response.Content.ReadAsStringAsync(ct);
        logger.LogDebug("WebCare risposta HTTP {Status}", (int)response.StatusCode);

        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.InternalServerError)
        {
            throw new HttpRequestException(
                $"WebCare GetCompetenza fallito: HTTP {(int)response.StatusCode}");
        }

        try
        {
            var competenza = await SoapResponseParser.EstraiCompetenzaWebCareAsync(soapXml, ct);
            logger.LogInformation(
                "WebCare GetCompetenza completato: {NMov} movimenti, lordo €{Lordo:F2}",
                competenza.NumeroMovimenti, competenza.ImportoLordo);
            return competenza;
        }
        catch (FederfarmaFaultException ex)
        {
            logger.LogError("WebCare SOAP Fault [{Cod}]: {Msg}", ex.Codice, ex.Dettaglio);
            throw;
        }
    }

    public Task<CompetenzaWebCare> ParseXmlAsync(string pathFileXml, CancellationToken ct = default)
        => parser.ParseFileAsync(pathFileXml, ct);
}
