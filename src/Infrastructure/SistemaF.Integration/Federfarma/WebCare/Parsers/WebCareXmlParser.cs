using System.Xml.Linq;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.WebCare.Models;

namespace SistemaF.Integration.Federfarma.WebCare.Parsers;

// ═══════════════════════════════════════════════════════════════════════════════
//  WEBCARE XML PARSER
//
//  Migrazione di getXMLCARE_Competenza in CSFWebCareLombardia.vb.
//
//  Il VB.NET originale leggeva solo la testata (si fermava a <RiepiloghiIVA>).
//  Questa implementazione legge l'intera gerarchia.
//
//  Struttura XML attesa (dedotta dal VB.NET + WSDL):
//    <Competenza>
//      <codiceAsl>01234</codiceAsl>
//      <codiceFarmacia>030311</codiceFarmacia>
//      <ragioneSocialeFarmacia>Farmacia di Prova</ragioneSocialeFarmacia>
//      <annoCompetenza>2024</annoCompetenza>
//      <meseCompetenza>03</meseCompetenza>
//      <numeroMovimenti>1335</numeroMovimenti>
//      <importoNetto>61502.14</importoNetto>
//      <importoLordo>62763.04</importoLordo>
//      <importoIva>1260.90</importoIva>
//      <riepiloghiIva>
//        <RiepilogoIva>
//          <categoria>A</categoria>
//          <aliquotaIva>4.00</aliquotaIva>
//          <imponibile>55000.00</imponibile>
//          <importoIva>2200.00</importoIva>
//          <importoParziale>57200.00</importoParziale>
//        </RiepilogoIva>
//      </riepiloghiIva>
//      <contabilizzazioni>
//        <Contabilizzazione>
//          ...
//        </Contabilizzazione>
//      </contabilizzazioni>
//    </Competenza>
// ═══════════════════════════════════════════════════════════════════════════════

public sealed class WebCareXmlParser
{
    public async Task<CompetenzaWebCare> ParseFileAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File XML WebCare non trovato: {path}", path);
        await using var stream = File.OpenRead(path);
        return await ParseStreamAsync(stream, ct);
    }

    public Task<CompetenzaWebCare> ParseStreamAsync(Stream stream, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var doc = XDocument.Load(stream);
        return Task.FromResult(Parse(doc));
    }

    public Task<CompetenzaWebCare> ParseStringAsync(string xml, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var doc = XDocument.Parse(xml);
        return Task.FromResult(Parse(doc));
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static CompetenzaWebCare Parse(XDocument doc)
    {
        var comp = doc.Descendants()
                      .FirstOrDefault(e => e.Name.LocalName.Equals("Competenza", StringComparison.OrdinalIgnoreCase))
                   ?? doc.Root
                   ?? throw new InvalidDataException("Elemento <Competenza> non trovato.");

        return new CompetenzaWebCare
        {
            CodiceAsl              = Testo(comp, "codiceAsl"),
            CodiceFarmacia         = Testo(comp, "codiceFarmacia"),
            RagioneSocialeFarmacia = Testo(comp, "ragioneSocialeFarmacia"),
            AnnoCompetenza         = Testo(comp, "annoCompetenza"),
            MeseCompetenza         = Testo(comp, "meseCompetenza"),
            NumeroMovimenti        = Intero(comp, "numeroMovimenti"),
            ImportoNetto           = Decimale(comp, "importoNetto"),
            ImportoLordo           = Decimale(comp, "importoLordo"),
            ImportoIva             = Decimale(comp, "importoIva"),
            RiepiloghiIva          = ParseRiepiloghiIva(comp),
            Contabilizzazioni      = ParseContabilizzazioni(comp),
        };
    }

    private static IReadOnlyList<RiepilogoIvaWebCare> ParseRiepiloghiIva(XElement parent)
        => parent.Descendants()
                 .Where(e => e.Name.LocalName.Equals("RiepilogoIva", StringComparison.OrdinalIgnoreCase)
                          && e.Parent?.Name.LocalName.Equals("riepiloghiIva", StringComparison.OrdinalIgnoreCase) == true)
                 .Select(ParseRiepilogoIva)
                 .ToList();

    private static RiepilogoIvaWebCare ParseRiepilogoIva(XElement r) => new()
    {
        Categoria       = Testo(r, "categoria"),
        AliquotaIva     = Decimale(r, "aliquotaIva"),
        Imponibile      = Decimale(r, "imponibile"),
        ImportoIva      = Decimale(r, "importoIva"),
        ImportoParziale = Decimale(r, "importoParziale"),
    };

    private static IReadOnlyList<ContabilizzazioneWebCare> ParseContabilizzazioni(XElement parent)
        => parent.Descendants()
                 .Where(e => e.Name.LocalName.Equals("Contabilizzazione", StringComparison.OrdinalIgnoreCase)
                          && e.Parent?.Name.LocalName.Equals("contabilizzazioni", StringComparison.OrdinalIgnoreCase) == true)
                 .Select(ParseContabilizzazione)
                 .ToList();

    private static ContabilizzazioneWebCare ParseContabilizzazione(XElement c) => new()
    {
        NumeroContabilizzazione = Intero(c, "numeroContabilizzazione"),
        DataContabilizzazione   = Testo(c, "dataContabilizzazione"),
        NumeroMovimenti         = Intero(c, "numeroMovimenti"),
        ImportoNetto            = Decimale(c, "importoNetto"),
        ImportoIva              = Decimale(c, "importoIva"),
        ImportoLordo            = Decimale(c, "importoLordo"),
        RiepiloghiIva           = c.Descendants()
                                   .Where(e => e.Name.LocalName.Equals("RiepilogoIva", StringComparison.OrdinalIgnoreCase)
                                            && e.Parent?.Name.LocalName.Equals("riepiloghiIva", StringComparison.OrdinalIgnoreCase) == true)
                                   .Select(ParseRiepilogoIva)
                                   .ToList(),
        Documenti               = c.Descendants()
                                   .Where(e => e.Name.LocalName.Equals("Documento", StringComparison.OrdinalIgnoreCase)
                                            && e.Parent?.Name.LocalName.Equals("documenti", StringComparison.OrdinalIgnoreCase) == true)
                                   .Select(ParseDocumento)
                                   .ToList(),
    };

    private static DocumentoWebCare ParseDocumento(XElement d)
    {
        var tipoStr = Testo(d, "tipo");
        var tipo = tipoStr.Equals("fattura", StringComparison.OrdinalIgnoreCase)
            ? TipoDocumentoDpc.Fattura
            : TipoDocumentoDpc.RiepilogoContabile;

        return new DocumentoWebCare
        {
            Tipo            = tipo,
            NumeroDocumento = Testo(d, "numeroDocumento"),
            DataDocumento   = Testo(d, "dataDocumento"),
            NumeroMovimenti = Intero(d, "numeroMovimenti"),
            ImportoNetto    = Decimale(d, "importoNetto"),
            ImportoIva      = Decimale(d, "importoIva"),
            ImportoLordo    = Decimale(d, "importoLordo"),
            RiepiloghiIva   = d.Descendants()
                               .Where(e => e.Name.LocalName.Equals("RiepilogoIva", StringComparison.OrdinalIgnoreCase)
                                        && e.Parent?.Name.LocalName.Equals("riepiloghiIva", StringComparison.OrdinalIgnoreCase) == true)
                               .Select(ParseRiepilogoIva)
                               .ToList(),
            Movimenti       = d.Descendants()
                               .Where(e => e.Name.LocalName.Equals("Movimento", StringComparison.OrdinalIgnoreCase)
                                        && e.Parent?.Name.LocalName.Equals("movimenti", StringComparison.OrdinalIgnoreCase) == true)
                               .Select(ParseMovimento)
                               .ToList(),
        };
    }

    private static MovimentoWebCare ParseMovimento(XElement m) => new()
    {
        IdMovimento   = Testo(m, "idMovimento"),
        Categoria     = Testo(m, "categoria"),
        CodiceFiscale = Testo(m, "codiceFiscale"),
        DataNascita   = Testo(m, "dataNascita"),
        Sesso         = Testo(m, "sesso"),
        DataMovimento = Testo(m, "dataMovimento"),
        QtaTotale     = Intero(m, "qtaTotale"),
        ImportoNetto  = Decimale(m, "importoNetto"),
        ImportoIva    = Decimale(m, "importoIva"),
        ImportoLordo  = Decimale(m, "importoLordo"),
        RiepiloghiIva = m.Descendants()
                         .Where(e => e.Name.LocalName.Equals("RiepilogoIva", StringComparison.OrdinalIgnoreCase)
                                  && e.Parent?.Name.LocalName.Equals("riepiloghiIva", StringComparison.OrdinalIgnoreCase) == true)
                         .Select(ParseRiepilogoIva)
                         .ToList(),
        Righe         = m.Descendants()
                         .Where(e => e.Name.LocalName.Equals("RigaMovimento", StringComparison.OrdinalIgnoreCase)
                                  && e.Parent?.Name.LocalName.Equals("righe", StringComparison.OrdinalIgnoreCase) == true)
                         .Select(ParseRigaMovimento)
                         .ToList(),
    };

    private static RigaMovimentoWebCare ParseRigaMovimento(XElement r) => new()
    {
        CodiceParaf             = Testo(r, "codiceParaf"),
        CodiceNomenclatore      = Testo(r, "codiceNomenclatore"),
        DescrizioneNomenclatore = Testo(r, "descrizioneNomenclatore"),
        CodiceEan               = Testo(r, "codiceEan"),
        Quantita                = Intero(r, "quantita"),
        ImportoNetto            = Decimale(r, "importoNetto"),
        ImportoIva              = Decimale(r, "importoIva"),
        ImportoLordo            = Decimale(r, "importoLordo"),
        AliquotaIva             = Decimale(r, "aliquotaIva"),
        Tipo                    = Testo(r, "tipo"),
    };

    // ── Helper XML ────────────────────────────────────────────────────────────

    private static string Testo(XElement parent, string nome, string fb = "")
        => parent.Elements()
                 .FirstOrDefault(e => e.Name.LocalName.Equals(nome, StringComparison.OrdinalIgnoreCase))
                 ?.Value?.Trim()
           ?? fb;

    private static decimal Decimale(XElement parent, string nome)
        => DpcXmlParser.ParseDecimale(Testo(parent, nome, "0"));

    private static int Intero(XElement parent, string nome, int fb = 0)
    {
        var raw = Testo(parent, nome, fb.ToString());
        return int.TryParse(raw, out var v) ? v : fb;
    }
}
