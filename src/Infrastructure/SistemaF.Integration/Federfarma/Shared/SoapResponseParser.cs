using System.Xml.Linq;
using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.WebCare.Models;
using SistemaF.Integration.Federfarma.WebCare.Parsers;

namespace SistemaF.Integration.Federfarma.Shared;

public static class SoapResponseParser
{
    private static readonly XNamespace SoapNs =
        "http://schemas.xmlsoap.org/soap/envelope/";

    public static XElement EstraiBody(string soapXml)
    {
        var doc  = XDocument.Parse(soapXml);
        var body = doc.Root
                      ?.Element(SoapNs + "Body")
                   ?? doc.Descendants()
                         .FirstOrDefault(e => e.Name.LocalName.Equals("Body", StringComparison.OrdinalIgnoreCase))
                   ?? throw new InvalidDataException("Elemento <Body> non trovato nella risposta SOAP.");

        var fault = body.Descendants()
                        .FirstOrDefault(e => e.Name.LocalName.Equals("Fault", StringComparison.OrdinalIgnoreCase));
        if (fault is not null)
        {
            var codice    = fault.Descendants()
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

    public static async Task<DistintaDpc> EstraiDistintaDpcAsync(string soapXml, CancellationToken ct)
    {
        var body     = EstraiBody(soapXml);
        var distinta = body.Descendants()
                           .FirstOrDefault(e => e.Name.LocalName.Equals("Distinta", StringComparison.OrdinalIgnoreCase))
                       ?? body;
        var parser = new DpcXmlParser();
        return await parser.ParseStringAsync(distinta.ToString(), ct);
    }

    public static async Task<CompetenzaWebCare> EstraiCompetenzaWebCareAsync(string soapXml, CancellationToken ct)
    {
        var body = EstraiBody(soapXml);
        var comp = body.Descendants()
                       .FirstOrDefault(e => e.Name.LocalName.Equals("Competenza", StringComparison.OrdinalIgnoreCase))
                   ?? body;
        var parser = new WebCareXmlParser();
        return await parser.ParseStringAsync(comp.ToString(), ct);
    }
}
