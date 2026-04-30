using System.Xml.Linq;
using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.WebCare.Models;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Integration.Federfarma.Shared;

// ═══════════════════════════════════════════════════════════════════════════════
//  SOAP RESPONSE PARSER
//
//  Estrae il body dalla risposta SOAP 1.1 e rileva i fault.
//  Migrazione del blocco try/catch con SoapException in entrambe le classi VB.NET.
//
//  Nel VB.NET il DisplayNode estraeva Code + Message dal fault detail.
//  Qui facciamo lo stesso con LINQ to XML.
// ═══════════════════════════════════════════════════════════════════════════════

internal static class SoapResponseParser
{
    private static readonly XNamespace SoapNs =
        "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Estrae il contenuto del &lt;Body&gt; SOAP.
    /// Se il body contiene un &lt;Fault&gt;, lancia FederfarmaFaultException.
    /// </summary>
    public static XElement EstraiBody(string soapXml)
    {
        var doc  = XDocument.Parse(soapXml);
        var body = doc.Root
                      ?.Element(SoapNs + "Body")
                   ?? doc.Descendants()
                         .FirstOrDefault(e => e.Name.LocalName.Equals("Body", StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidDataException("Elemento <Body> non trovato nella risposta SOAP.");

        // Verifica fault
        var fault = body.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
        if (fault is not null)
        {
            var codice   = fault.Descendants()
                                .FirstOrDefault(e => e.Name.LocalName.Equals("Code", StringComparison.OrdinalIgnoreCase))
                                ?.Value ?? "FAULT";
            var messaggio = fault.Descendants()
                                 .FirstOrDefault(e => e.Name.LocalName.Equals("Message", StringComparison.OrdinalIgnoreCase))
                                 ?.Value
                           ?? fault.Descendants()
                                   .FirstOrDefault(e => e.Name.LocalName.Equals("faultstring", StringComparison.OrdinalIgnoreCase))
                                   ?.Value
                           ?? "Errore sconosciuto";
            throw new FederfarmaFaultException(codice, messaggio);
        }

        return body;
    }

    /// <summary>
    /// Estrae la DistintaDpc dalla risposta SOAP GetFUR.
    /// </summary>
    public static async Task<DistintaDpc> EstraiDistintaDpcAsync(
        string soapXml, CancellationToken ct)
    {
        var body = EstraiBody(soapXml);
        // Il risultato si trova in GetFURResponse/GetFURResult/Distinta
        var distinta = body.Descendants()
                           .FirstOrDefault(e => e.Name.LocalName.Equals("Distinta", StringComparison.OrdinalIgnoreCase));
        if (distinta is null)
        {
            // Può essere che l'intera risposta body = body
            distinta = body;
        }
        var xmlString = distinta.ToString();
        var parser    = new DpcXmlParser();
        return await parser.ParseStringAsync(xmlString, ct);
    }

    /// <summary>
    /// Estrae la CompetenzaWebCare dalla risposta SOAP GetCompetenza.
    /// </summary>
    public static async Task<CompetenzaWebCare> EstraiCompetenzaWebCareAsync(
        string soapXml, CancellationToken ct)
    {
        var body = EstraiBody(soapXml);
        var comp = body.Descendants()
                       .FirstOrDefault(e => e.Name.LocalName.Equals("Competenza", StringComparison.OrdinalIgnoreCase));
        if (comp is null) comp = body;
        var xmlString = comp.ToString();
        var parser    = new WebCareXmlParser();
        return await parser.ParseStringAsync(xmlString, ct);
    }
}
