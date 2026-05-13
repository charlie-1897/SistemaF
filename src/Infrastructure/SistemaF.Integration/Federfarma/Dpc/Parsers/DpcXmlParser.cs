using System.Xml.Linq;
using SistemaF.Integration.Federfarma.Dpc.Models;

namespace SistemaF.Integration.Federfarma.Dpc.Parsers;

// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550
//  DPC XML PARSER
//
//  Migrazione di getXMLDPC_Distinta in CSFWebDpcLombardia.vb.
//
//  Il VB.NET usava XmlTextReader in modalit\u00e0 forward-only con state machine
//  manuale (iR, iPT, iPR come contatori). La logica era:
//    - Quando trova <Ricetta> \u2192 crea nuova ricetta, incrementa iR
//    - Quando trova <Prodotto> \u2192 crea nuovo prodotto, incrementa iPT e iPR
//    - I prodotti vengono aggiunti sia a cProdotti (piatta) che a cProdotto (per ricetta)
//    - Quando trova </Prodotto> \u2192 assegna cProdotto alla ricetta e resetta iPR=-1
//
//  In C# usiamo XDocument (LINQ to XML) che \u00e8 pi\u00f9 leggibile e sicuro.
//
//  Struttura XML attesa (dedotta dal VB.NET):
//    <Distinta>
//      <MeseCompetenza>03</MeseCompetenza>
//      <AnnoCompetenza>2024</AnnoCompetenza>
//      <TotalePrezzoPubblicoProdotti>1234,56</TotalePrezzoPubblicoProdotti>
//      ...
//      <NumeroRicette>42</NumeroRicette>
//      <NumeroProdotti>87</NumeroProdotti>
//      <Ricette>
//        <Ricetta>
//          <Progressivo>1</Progressivo>
//          <Iup>ABC123</Iup>
//          ...
//          <Prodotto>
//            <Minsan>012345678</Minsan>
//            <Quantita>2</Quantita>
//            <PrezzoPubblico>12,50</PrezzoPubblico>
//            <AliquotaIva>10</AliquotaIva>
//            <PrezzoAcquisto>10,20</PrezzoAcquisto>
//            <PercentualeFarmacia>6,65</PercentualeFarmacia>
//            <PercentualeGrossista>2,00</PercentualeGrossista>
//          </Prodotto>
//        </Ricetta>
//      </Ricette>
//    </Distinta>
// \u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550\u2550

public sealed class DpcXmlParser
{
    /// <summary>
    /// Parsifica un file XML DPC locale (getXMLDPC_Distinta nel VB.NET).
    /// </summary>
    public async Task<DistintaDpc> ParseFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File XML DPC non trovato: {path}", path);

        await using var stream = File.OpenRead(path);
        return await ParseStreamAsync(stream, ct);
    }

    /// <summary>
    /// Parsifica un flusso XML DPC (es. risposta SOAP body gi\u00e0 estratto).
    /// </summary>
    public Task<DistintaDpc> ParseStreamAsync(Stream stream, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var doc = XDocument.Load(stream);
        return Task.FromResult(Parse(doc));
    }

    /// <summary>
    /// Parsifica una stringa XML DPC (risposta SOAP deserializzata).
    /// </summary>
    public Task<DistintaDpc> ParseStringAsync(string xml, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var doc = XDocument.Parse(xml);
        return Task.FromResult(Parse(doc));
    }

    // \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    private static DistintaDpc Parse(XDocument doc)
    {
        // Ricerca il nodo radice Distinta (pu\u00f2 essere annidato nell'envelope SOAP)
        var distinta = doc.Descendants()
                          .FirstOrDefault(e => e.Name.LocalName.Equals("Distinta", StringComparison.OrdinalIgnoreCase))
                      ?? doc.Root
                      ?? throw new InvalidDataException("Elemento <Distinta> non trovato nel documento XML.");

        // \u2500\u2500 Testata \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        var mese   = Testo(distinta, "MeseCompetenza");
        var anno   = Testo(distinta, "AnnoCompetenza");
        var totPP  = Decimale(distinta, "TotalePrezzoPubblicoProdotti");
        var impCNI = Decimale(distinta, "ImportoCompensoNettoIva");
        var impTLI = Decimale(distinta, "ImportoTicketRiscossiLordoIva");
        var impTNI = Decimale(distinta, "ImportoTicketRiscossiNettoIva");
        var totImp = Decimale(distinta, "TotaleImponibile");
        var totIva = Decimale(distinta, "TotaleIva");
        var totFat = Decimale(distinta, "TotaleFatturato");
        var nRic   = Intero(distinta, "NumeroRicette");
        var nProd  = Intero(distinta, "NumeroProdotti");

        // \u2500\u2500 Ricette \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        var ricetteXml   = distinta.Descendants()
                                   .Where(e => e.Name.LocalName.Equals("Ricetta", StringComparison.OrdinalIgnoreCase));
        var ricette      = new List<RicettaDpc>();
        var tuttiProdotti = new List<ProdottoDpc>();
        var progressivoRicetta = 0;

        foreach (var rXml in ricetteXml)
        {
            var prodottiRicetta = new List<ProdottoDpc>();

            foreach (var pXml in rXml.Descendants()
                                     .Where(e => e.Name.LocalName.Equals("Prodotto", StringComparison.OrdinalIgnoreCase)))
            {
                var prod = ParseProdotto(pXml, progressivoRicetta);
                prodottiRicetta.Add(prod);
                tuttiProdotti.Add(prod);
            }

            ricette.Add(ParseRicetta(rXml, prodottiRicetta));
            progressivoRicetta++;
        }

        return new DistintaDpc
        {
            MeseCompetenza                 = mese,
            AnnoCompetenza                 = anno,
            TotalePrezzoPubblicoProdotti   = totPP,
            ImportoCompensoNettoIva        = impCNI,
            ImportoTicketRiscossiLordoIva  = impTLI,
            ImportoTicketRiscossiNettoIva  = impTNI,
            TotaleImponibile               = totImp,
            TotaleIva                      = totIva,
            TotaleFatturato                = totFat,
            NumeroRicette                  = nRic > 0 ? nRic : ricette.Count,
            NumeroProdotti                 = nProd > 0 ? nProd : tuttiProdotti.Count,
            Ricette                        = ricette,
            TuttiProdotti                  = tuttiProdotti,
        };
    }

    private static RicettaDpc ParseRicetta(XElement r, List<ProdottoDpc> prodotti)
        => new()
        {
            Progressivo      = Intero(r, "Progressivo"),
            Iup              = Testo(r, "Iup"),
            AslAssistito     = Testo(r, "AslAssistito"),
            CodiceRegionale  = Testo(r, "CodiceRegionale"),
            CodiceRicetta    = Testo(r, "CodiceRicetta"),
            DataSpedizione   = Testo(r, "DataSpedizione"),
            DataMedico       = Testo(r, "DataMedico"),
            OnereFarmacia    = Decimale(r, "OnereFarmacia"),
            OnereGrossista   = Decimale(r, "OnereGrossista"),
            TipoEsenzione    = Testo(r, "TipoEsenzione"),
            TicketRiscosso   = Decimale(r, "TicketRiscosso"),
            CodiceFiscale    = Testo(r, "CodiceFiscale"),
            Difforme         = Testo(r, "Difforme"),
            DataNascita      = Testo(r, "DataNascita"),
            Sesso            = Testo(r, "Sesso"),
            Prodotti         = prodotti,
        };

    private static ProdottoDpc ParseProdotto(XElement p, int progressivoRicetta)
        => new()
        {
            ProgressivoRicetta   = Intero(p, "Progressivo", progressivoRicetta),
            MinSan               = Testo(p, "Minsan"),
            Quantita             = Intero(p, "Quantita"),
            PrezzoPubblico       = Decimale(p, "PrezzoPubblico"),
            AliquotaIva          = Decimale(p, "AliquotaIva"),
            PrezzoAcquisto       = Decimale(p, "PrezzoAcquisto"),
            PercentualeFarmacia  = Decimale(p, "PercentualeFarmacia"),
            PercentualeGrossista = Decimale(p, "PercentualeGrossista"),
        };

    // \u2500\u2500 Helper XML \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    /// <summary>Restituisce il testo del primo elemento figlio (case-insensitive).</summary>
    private static string Testo(XElement parent, string nome, string fallback = "")
        => parent.Descendants()
                 .FirstOrDefault(e => e.Name.LocalName.Equals(nome, StringComparison.OrdinalIgnoreCase))
                 ?.Value?.Trim()
           ?? fallback;

    /// <summary>
    /// Converte il valore numerico \u2014 il VB.NET usava MyFormatDbl che sostituiva
    /// il punto con la virgola (formato locale IT). Qui accettiamo entrambi.
    /// </summary>
    private static decimal Decimale(XElement parent, string nome)
    {
        var raw = Testo(parent, nome, "0");
        return ParseDecimale(raw);
    }

    private static int Intero(XElement parent, string nome, int fallback = 0)
    {
        var raw = Testo(parent, nome, fallback.ToString());
        return int.TryParse(raw, out var v) ? v : fallback;
    }

    public static decimal ParseDecimale(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return 0m;
        // Normalizza: "1.234,56" \u2192 "1234.56", "1234.56" \u2192 "1234.56"
        var normalized = raw.Trim().Replace(".", "").Replace(",", ".");
        if (normalized.Contains('.') && normalized.IndexOf('.') != normalized.LastIndexOf('.'))
        {
            // Ha pi\u00f9 di un punto: formato errato, tenta con sostituzione semplice
            normalized = raw.Trim().Replace(",", ".");
        }
        return decimal.TryParse(normalized,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v) ? v : 0m;
    }
}
