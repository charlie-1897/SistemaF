using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Models;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Integration.Federfarma.Dpc.Clients;

// ═══════════════════════════════════════════════════════════════════════════════
//  FEDERFARMA WEB DPC CLIENT
//
//  Migrazione di CSFWebDpcLombardia.getWEBDPC_Distinta.
//
//  Sostituisce:
//    - FurDpcServiceWse (proxy WSE2) → HttpClient con SOAP envelope manuale
//    - UsernameToken (WSE2) → SoapEnvelopeBuilder.BuildDpcGetFurEnvelope
//    - SoapException handling → FederfarmaFaultException
//
//  Registrazione DI (in Program.cs / Startup):
//    services.AddHttpClient<IFederfarmaWebDpcClient, FederfarmaWebDpcClient>()
//            .SetHandlerLifetime(TimeSpan.FromMinutes(5));
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class FederfarmaWebDpcClient(
    HttpClient              httpClient,
    FederfarmaConfiguration config,
    DpcXmlParser            parser,
    ILogger<FederfarmaWebDpcClient> logger)
    : IFederfarmaWebDpcClient
{
    public async Task<DistintaDpc> GetDistintaAsync(
        FederfarmaCredenziali  credenziali,
        FederfarmaRichiestaFur richiesta,
        CancellationToken      ct = default)
    {
        logger.LogInformation(
            "DPC GetFUR: ASL={Asl} Farm={Farm} {Mese}/{Anno}",
            richiesta.CodiceAsl, richiesta.CodiceFarmaciaAsl, richiesta.Mese, richiesta.Anno);

        var envelope = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(credenziali, richiesta);
        var content  = new StringContent(envelope, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPAction", $"\"{config.DpcSoapAction}\"");

        using var request = new HttpRequestMessage(HttpMethod.Post, config.DpcEndpointUrl)
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
                $"Timeout ({config.TimeoutSecondi}s) durante chiamata DPC GetFUR.", ex);
        }

        var soapXml = await response.Content.ReadAsStringAsync(ct);

        logger.LogDebug("DPC risposta HTTP {Status}", (int)response.StatusCode);

        // Gestisce HTTP 500 con SOAP Fault nel body
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.InternalServerError)
        {
            throw new HttpRequestException(
                $"DPC GetFUR fallito: HTTP {(int)response.StatusCode}");
        }

        try
        {
            var distinta = await SoapResponseParser.EstraiDistintaDpcAsync(soapXml, ct);
            logger.LogInformation(
                "DPC GetFUR completato: {NRic} ricette, {NProd} prodotti",
                distinta.NumeroRicette, distinta.NumeroProdotti);
            return distinta;
        }
        catch (FederfarmaFaultException ex)
        {
            logger.LogError("DPC SOAP Fault [{Cod}]: {Msg}", ex.Codice, ex.Dettaglio);
            throw;
        }
    }

    public Task<DistintaDpc> ParseXmlAsync(string pathFileXml, CancellationToken ct = default)
        => parser.ParseFileAsync(pathFileXml, ct);
}

// ═══════════════════════════════════════════════════════════════════════════════
//  FEDERFARMA WEBCARE CLIENT
//
//  Migrazione di CSFWebCareLombardia.getWEBCARE_Competenza.
//
//  Sostituisce:
//    - ServiceWse(nomeAsl) → HttpClient con URL dinamico basato su nomeAsl
//    - GetCompetenza(login, PIN, FurRequest) → SoapEnvelopeBuilder.BuildWebCareGetCompetenzaEnvelope
// ═══════════════════════════════════════════════════════════════════════════════

