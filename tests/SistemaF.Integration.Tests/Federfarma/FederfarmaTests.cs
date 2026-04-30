using FluentAssertions;
using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Dpc.Parsers;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Models;
using SistemaF.Integration.Federfarma.WebCare.Parsers;
using Xunit;

namespace SistemaF.Integration.Tests.Federfarma;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST SUITE — Federfarma Integration
//  Copertura: FederfarmaCredenziali, SoapEnvelopeBuilder, DpcXmlParser,
//             WebCareXmlParser, FederfarmaRichiestaFur
// ═══════════════════════════════════════════════════════════════════════════════

// ── XML di test ───────────────────────────────────────────────────────────────

internal static class FederfarmaTestData
{
    /// <summary>XML DPC di test — struttura completa con 2 ricette, 3 prodotti.</summary>
    public const string DpcXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Distinta>
          <MeseCompetenza>03</MeseCompetenza>
          <AnnoCompetenza>2024</AnnoCompetenza>
          <TotalePrezzoPubblicoProdotti>1234,56</TotalePrezzoPubblicoProdotti>
          <ImportoCompensoNettoIva>1100,00</ImportoCompensoNettoIva>
          <ImportoTicketRiscossiLordoIva>45,00</ImportoTicketRiscossiLordoIva>
          <ImportoTicketRiscossiNettoIva>41,67</ImportoTicketRiscossiNettoIva>
          <TotaleImponibile>1100,00</TotaleImponibile>
          <TotaleIva>110,00</TotaleIva>
          <TotaleFatturato>1210,00</TotaleFatturato>
          <NumeroRicette>2</NumeroRicette>
          <NumeroProdotti>3</NumeroProdotti>
          <Ricette>
            <Ricetta>
              <Progressivo>1</Progressivo>
              <Iup>IUP001</Iup>
              <AslAssistito>030313</AslAssistito>
              <CodiceRegionale>REG001</CodiceRegionale>
              <CodiceRicetta>RIC001</CodiceRicetta>
              <DataSpedizione>2024-03-15</DataSpedizione>
              <DataMedico>2024-03-10</DataMedico>
              <OnereFarmacia>8,25</OnereFarmacia>
              <OnereGrossista>2,00</OnereGrossista>
              <TipoEsenzione>E01</TipoEsenzione>
              <TicketRiscosso>3,00</TicketRiscosso>
              <CodiceFiscale>RSSMRA80A01H501Z</CodiceFiscale>
              <Difforme></Difforme>
              <DataNascita>1980-01-01</DataNascita>
              <Sesso>M</Sesso>
              <Prodotto>
                <Minsan>023569287</Minsan>
                <Quantita>2</Quantita>
                <PrezzoPubblico>8,50</PrezzoPubblico>
                <AliquotaIva>10</AliquotaIva>
                <PrezzoAcquisto>6,20</PrezzoAcquisto>
                <PercentualeFarmacia>6,65</PercentualeFarmacia>
                <PercentualeGrossista>2,00</PercentualeGrossista>
              </Prodotto>
              <Prodotto>
                <Minsan>034512367</Minsan>
                <Quantita>1</Quantita>
                <PrezzoPubblico>15,90</PrezzoPubblico>
                <AliquotaIva>4</AliquotaIva>
                <PrezzoAcquisto>12,80</PrezzoAcquisto>
                <PercentualeFarmacia>6,65</PercentualeFarmacia>
                <PercentualeGrossista>2,00</PercentualeGrossista>
              </Prodotto>
            </Ricetta>
            <Ricetta>
              <Progressivo>2</Progressivo>
              <Iup>IUP002</Iup>
              <AslAssistito>030313</AslAssistito>
              <CodiceRegionale>REG002</CodiceRegionale>
              <CodiceRicetta>RIC002</CodiceRicetta>
              <DataSpedizione>2024-03-20</DataSpedizione>
              <DataMedico>2024-03-18</DataMedico>
              <OnereFarmacia>5,10</OnereFarmacia>
              <OnereGrossista>1,50</OnereGrossista>
              <TipoEsenzione></TipoEsenzione>
              <TicketRiscosso>0</TicketRiscosso>
              <CodiceFiscale>VRDLGI75B45H501X</CodiceFiscale>
              <Difforme>S</Difforme>
              <DataNascita>1975-02-05</DataNascita>
              <Sesso>F</Sesso>
              <Prodotto>
                <Minsan>012345678</Minsan>
                <Quantita>3</Quantita>
                <PrezzoPubblico>4,20</PrezzoPubblico>
                <AliquotaIva>4</AliquotaIva>
                <PrezzoAcquisto>3,10</PrezzoAcquisto>
                <PercentualeFarmacia>6,65</PercentualeFarmacia>
                <PercentualeGrossista>2,00</PercentualeGrossista>
              </Prodotto>
            </Ricetta>
          </Ricette>
        </Distinta>
        """;

    /// <summary>XML WebCare di test — testata + una contabilizzazione.</summary>
    public const string WebCareXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <Competenza>
          <codiceAsl>030313</codiceAsl>
          <codiceFarmacia>00073</codiceFarmacia>
          <ragioneSocialeFarmacia>Farmacia Rossi S.r.l.</ragioneSocialeFarmacia>
          <annoCompetenza>2024</annoCompetenza>
          <meseCompetenza>03</meseCompetenza>
          <numeroMovimenti>42</numeroMovimenti>
          <importoNetto>1100.00</importoNetto>
          <importoLordo>1210.00</importoLordo>
          <importoIva>110.00</importoIva>
          <riepiloghiIva>
            <RiepilogoIva>
              <categoria>A</categoria>
              <aliquotaIva>10.00</aliquotaIva>
              <imponibile>1000.00</imponibile>
              <importoIva>100.00</importoIva>
              <importoParziale>1100.00</importoParziale>
            </RiepilogoIva>
            <RiepilogoIva>
              <categoria>C</categoria>
              <aliquotaIva>4.00</aliquotaIva>
              <imponibile>100.00</imponibile>
              <importoIva>4.00</importoIva>
              <importoParziale>104.00</importoParziale>
            </RiepilogoIva>
          </riepiloghiIva>
          <contabilizzazioni>
            <Contabilizzazione>
              <numeroContabilizzazione>1</numeroContabilizzazione>
              <dataContabilizzazione>2024-04-05</dataContabilizzazione>
              <numeroMovimenti>42</numeroMovimenti>
              <importoNetto>1100.00</importoNetto>
              <importoIva>110.00</importoIva>
              <importoLordo>1210.00</importoLordo>
              <riepiloghiIva>
                <RiepilogoIva>
                  <categoria>A</categoria>
                  <aliquotaIva>10.00</aliquotaIva>
                  <imponibile>1000.00</imponibile>
                  <importoIva>100.00</importoIva>
                  <importoParziale>1100.00</importoParziale>
                </RiepilogoIva>
              </riepiloghiIva>
              <documenti>
                <Documento>
                  <tipo>fattura</tipo>
                  <numeroDocumento>FAT2024/001</numeroDocumento>
                  <dataDocumento>2024-04-05</dataDocumento>
                  <numeroMovimenti>42</numeroMovimenti>
                  <importoNetto>1100.00</importoNetto>
                  <importoIva>110.00</importoIva>
                  <importoLordo>1210.00</importoLordo>
                  <riepiloghiIva/>
                  <movimenti>
                    <Movimento>
                      <idMovimento>MOV001</idMovimento>
                      <categoria>A</categoria>
                      <codiceFiscale>RSSMRA80A01H501Z</codiceFiscale>
                      <dataNascita>1980-01-01</dataNascita>
                      <sesso>M</sesso>
                      <dataMovimento>2024-03-15</dataMovimento>
                      <qtaTotale>2</qtaTotale>
                      <importoNetto>12.40</importoNetto>
                      <importoIva>1.24</importoIva>
                      <importoLordo>13.64</importoLordo>
                      <riepiloghiIva/>
                      <righe>
                        <RigaMovimento>
                          <codiceParaf></codiceParaf>
                          <codiceNomenclatore>NC001</codiceNomenclatore>
                          <descrizioneNomenclatore>AMOXICILLINA 500MG</descrizioneNomenclatore>
                          <codiceEan>8012345678901</codiceEan>
                          <quantita>2</quantita>
                          <importoNetto>12.40</importoNetto>
                          <importoIva>1.24</importoIva>
                          <importoLordo>13.64</importoLordo>
                          <aliquotaIva>10.00</aliquotaIva>
                          <tipo>farmaco</tipo>
                        </RigaMovimento>
                      </righe>
                    </Movimento>
                  </movimenti>
                </Documento>
              </documenti>
            </Contabilizzazione>
          </contabilizzazioni>
        </Competenza>
        """;

    public const string SoapFaultXml = """
        <?xml version="1.0" encoding="utf-8"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body>
            <soap:Fault>
              <faultcode>Client</faultcode>
              <faultstring>Autenticazione fallita: credenziali non valide.</faultstring>
              <detail>
                <Code>AUTH_FAILED</Code>
                <Message>Username o PIN errati</Message>
              </detail>
            </soap:Fault>
          </soap:Body>
        </soap:Envelope>
        """;
}

// ── FederfarmaCredenziali ─────────────────────────────────────────────────────

public sealed class FederfarmaCredenzialiTests
{
    [Fact] public void Da_valido_crea_credenziali()
    {
        var c = FederfarmaCredenziali.Da("SO00073", "andrea58");
        c.Username.Should().Be("SO00073");
        c.Pin.Should().Be("andrea58");
    }

    [Fact] public void Da_username_vuoto_lancia_eccezione()
        => ((Action)(() => FederfarmaCredenziali.Da("", "pin"))).Should().Throw<ArgumentException>();

    [Fact] public void Da_pin_vuoto_lancia_eccezione()
        => ((Action)(() => FederfarmaCredenziali.Da("user", ""))).Should().Throw<ArgumentException>();

    [Fact] public void Da_rimuove_spazi()
    {
        var c = FederfarmaCredenziali.Da("  SO00073  ", "  pin  ");
        c.Username.Should().Be("SO00073");
        c.Pin.Should().Be("pin");
    }
}

// ── FederfarmaRichiestaFur ────────────────────────────────────────────────────

public sealed class FederfarmaRichiestaFurTests
{
    [Fact] public void Da_valido_crea_richiesta()
    {
        var r = FederfarmaRichiestaFur.Da("030313", "00073", 3, 2024);
        r.Mese.Should().Be("03");
        r.Anno.Should().Be("2024");
    }

    [Theory]
    [InlineData(0), InlineData(13)]
    public void Mese_invalido_lancia_eccezione(int mese)
        => ((Action)(() => FederfarmaRichiestaFur.Da("ASL", "FARM", mese, 2024)))
           .Should().Throw<ArgumentOutOfRangeException>();

    [Fact] public void Anno_invalido_lancia_eccezione()
        => ((Action)(() => FederfarmaRichiestaFur.Da("ASL", "FARM", 1, 1999)))
           .Should().Throw<ArgumentOutOfRangeException>();
}

// ── SoapEnvelopeBuilder ───────────────────────────────────────────────────────

public sealed class SoapEnvelopeBuilderTests
{
    private static readonly FederfarmaCredenziali  Cred = FederfarmaCredenziali.Da("SO00073", "andrea58");
    private static readonly FederfarmaRichiestaFur Req  = FederfarmaRichiestaFur.Da("030313", "00073", 3, 2024);

    [Fact] public void DpcEnvelope_contiene_credenziali()
    {
        var env = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(Cred, Req);
        env.Should().Contain("SO00073").And.Contain("andrea58");
    }

    [Fact] public void DpcEnvelope_contiene_parametri_richiesta()
    {
        var env = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(Cred, Req);
        env.Should().Contain("030313").And.Contain("00073").And.Contain("03").And.Contain("2024");
    }

    [Fact] public void DpcEnvelope_include_wssecurity_header()
    {
        var env = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(Cred, Req);
        env.Should().Contain("wsse:Security").And.Contain("UsernameToken");
    }

    [Fact] public void DpcEnvelope_mustunderstand_false()
    {
        var env = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(Cred, Req);
        env.Should().Contain("mustUnderstand=\"0\"");
    }

    [Fact] public void WebCareEnvelope_include_parametri_body()
    {
        var env = SoapEnvelopeBuilder.BuildWebCareGetCompetenzaEnvelope(Cred, Req);
        // WebCare passa anche login/PIN nel body (comportamento originale VB.NET)
        env.Should().Contain("<tns:login>SO00073</tns:login>")
           .And.Contain("<tns:PIN>andrea58</tns:PIN>");
    }

    [Fact] public void Escape_caratteri_speciali()
    {
        var credConSpec = FederfarmaCredenziali.Da("user&<test>", "pin\"'test");
        var env = SoapEnvelopeBuilder.BuildDpcGetFurEnvelope(credConSpec, Req);
        env.Should().Contain("user&amp;&lt;test&gt;");
    }
}

// ── DpcXmlParser ──────────────────────────────────────────────────────────────

public sealed class DpcXmlParserTests
{
    private readonly DpcXmlParser _parser = new();

    private Task<DistintaDpc> ParseAsync(string xml)
        => _parser.ParseStringAsync(xml);

    [Fact] public async Task Parse_distinta_testata_completa()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        d.MeseCompetenza.Should().Be("03");
        d.AnnoCompetenza.Should().Be("2024");
        d.TotaleFatturato.Should().Be(1210m);
        d.TotaleIva.Should().Be(110m);
        d.NumeroRicette.Should().Be(2);
        d.NumeroProdotti.Should().Be(3);
    }

    [Fact] public async Task Parse_importi_con_virgola_decimale()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        d.TotalePrezzoPubblicoProdotti.Should().Be(1234.56m);
        d.ImportoCompensoNettoIva.Should().Be(1100m);
    }

    [Fact] public async Task Parse_due_ricette()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        d.Ricette.Should().HaveCount(2);
    }

    [Fact] public async Task Parse_prima_ricetta_corretta()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        var r = d.Ricette[0];
        r.Iup.Should().Be("IUP001");
        r.AslAssistito.Should().Be("030313");
        r.CodiceRicetta.Should().Be("RIC001");
        r.CodiceFiscale.Should().Be("RSSMRA80A01H501Z");
        r.Sesso.Should().Be("M");
        r.OnereFarmacia.Should().Be(8.25m);
        r.TicketRiscosso.Should().Be(3m);
    }

    [Fact] public async Task Parse_prodotti_ricetta_corretti()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        var r = d.Ricette[0];
        r.Prodotti.Should().HaveCount(2);

        var p = r.Prodotti[0];
        p.MinSan.Should().Be("023569287");
        p.Quantita.Should().Be(2);
        p.PrezzoPubblico.Should().Be(8.50m);
        p.AliquotaIva.Should().Be(10m);
        p.PrezzoAcquisto.Should().Be(6.20m);
        p.PercentualeFarmacia.Should().Be(6.65m);
        p.PercentualeGrossista.Should().Be(2m);
    }

    [Fact] public async Task Parse_seconda_ricetta_difforme()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        var r = d.Ricette[1];
        r.IsDifforme.Should().BeTrue();
        r.Sesso.Should().Be("F");
        r.Prodotti.Should().HaveCount(1);
    }

    [Fact] public async Task Parse_tutti_prodotti_lista_piatta()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        d.TuttiProdotti.Should().HaveCount(3);
    }

    [Fact] public async Task Parse_competenza_mese_anno()
    {
        var d = await ParseAsync(FederfarmaTestData.DpcXml);
        d.Competenza.Should().Be(new DateOnly(2024, 3, 1));
    }

    [Fact] public async Task Parse_xml_vuoto_ritorna_distinta_vuota()
    {
        var xmlVuoto = "<Distinta><NumeroRicette>0</NumeroRicette><NumeroProdotti>0</NumeroProdotti></Distinta>";
        var d = await ParseAsync(xmlVuoto);
        d.IsVuota.Should().BeTrue();
    }

    [Fact] public async Task ParseFile_file_non_trovato_lancia_eccezione()
    {
        await ((Func<Task>)(() => _parser.ParseFileAsync("/non/esiste.xml")))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Theory]
    [InlineData("1234,56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    [InlineData("0", 0)]
    [InlineData("", 0)]
    [InlineData("  ", 0)]
    public void ParseDecimale_vari_formati(string input, double expected)
        => DpcXmlParser.ParseDecimale(input).Should().BeApproximately((decimal)expected, 0.001m);
}

// ── WebCareXmlParser ──────────────────────────────────────────────────────────

public sealed class WebCareXmlParserTests
{
    private readonly WebCareXmlParser _parser = new();

    private Task<CompetenzaWebCare> ParseAsync(string xml)
        => _parser.ParseStringAsync(xml);

    [Fact] public async Task Parse_testata_competenza()
    {
        var c = await ParseAsync(FederfarmaTestData.WebCareXml);
        c.CodiceAsl.Should().Be("030313");
        c.CodiceFarmacia.Should().Be("00073");
        c.RagioneSocialeFarmacia.Should().Be("Farmacia Rossi S.r.l.");
        c.AnnoCompetenza.Should().Be("2024");
        c.MeseCompetenza.Should().Be("03");
    }

    [Fact] public async Task Parse_importi_competenza()
    {
        var c = await ParseAsync(FederfarmaTestData.WebCareXml);
        c.NumeroMovimenti.Should().Be(42);
        c.ImportoNetto.Should().Be(1100m);
        c.ImportoLordo.Should().Be(1210m);
        c.ImportoIva.Should().Be(110m);
    }

    [Fact] public async Task Parse_riepilogliiva_competenza()
    {
        var c = await ParseAsync(FederfarmaTestData.WebCareXml);
        c.RiepiloghiIva.Should().HaveCount(2);
        c.RiepiloghiIva[0].Categoria.Should().Be("A");
        c.RiepiloghiIva[0].AliquotaIva.Should().Be(10m);
        c.RiepiloghiIva[0].Imponibile.Should().Be(1000m);
    }

    [Fact] public async Task Parse_contabilizzazioni()
    {
        var c = await ParseAsync(FederfarmaTestData.WebCareXml);
        c.Contabilizzazioni.Should().HaveCount(1);

        var cont = c.Contabilizzazioni[0];
        cont.NumeroContabilizzazione.Should().Be(1);
        cont.DataContabilizzazione.Should().Be("2024-04-05");
        cont.ImportoNetto.Should().Be(1100m);
    }

    [Fact] public async Task Parse_documenti()
    {
        var c   = await ParseAsync(FederfarmaTestData.WebCareXml);
        var doc = c.Contabilizzazioni[0].Documenti.Should().HaveCount(1).And.Subject.First();
        doc.Tipo.Should().Be(TipoDocumentoDpc.Fattura);
        doc.NumeroDocumento.Should().Be("FAT2024/001");
    }

    [Fact] public async Task Parse_movimenti()
    {
        var c   = await ParseAsync(FederfarmaTestData.WebCareXml);
        var mov = c.Contabilizzazioni[0].Documenti[0].Movimenti.Should().HaveCount(1).And.Subject.First();
        mov.IdMovimento.Should().Be("MOV001");
        mov.Categoria.Should().Be("A");
        mov.Sesso.Should().Be("M");
        mov.QtaTotale.Should().Be(2);
    }

    [Fact] public async Task Parse_righe_movimento()
    {
        var c   = await ParseAsync(FederfarmaTestData.WebCareXml);
        var riga = c.Contabilizzazioni[0].Documenti[0].Movimenti[0].Righe
                   .Should().HaveCount(1).And.Subject.First();
        riga.DescrizioneNomenclatore.Should().Be("AMOXICILLINA 500MG");
        riga.CodiceEan.Should().Be("8012345678901");
        riga.Quantita.Should().Be(2);
        riga.ImportoNetto.Should().Be(12.40m);
        riga.Tipo.Should().Be("farmaco");
    }

    [Fact] public async Task Parse_competenza_mese_anno()
    {
        var c = await ParseAsync(FederfarmaTestData.WebCareXml);
        c.Competenza.Should().Be(new DateOnly(2024, 3, 1));
    }
}

// ── SoapResponseParser ────────────────────────────────────────────────────────

public sealed class SoapResponseParserTests
{
    [Fact] public void EstraiBody_fault_lancia_FederfarmaFaultException()
    {
        var act = () => SoapResponseParser.EstraiBody(FederfarmaTestData.SoapFaultXml);
        act.Should().Throw<FederfarmaFaultException>()
           .Which.Dettaglio.Should().Contain("credenziali");
    }

    [Fact] public void EstraiBody_risposta_valida()
    {
        var soapOk = """
            <?xml version="1.0"?>
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body><test>ok</test></soap:Body>
            </soap:Envelope>
            """;
        var body = SoapResponseParser.EstraiBody(soapOk);
        body.Should().NotBeNull();
    }

    [Fact] public async Task EstraiDistintaDpcAsync_da_soap_wrapper()
    {
        var soapConDistinta = $"""
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                {FederfarmaTestData.DpcXml}
              </soap:Body>
            </soap:Envelope>
            """;
        var distinta = await SoapResponseParser.EstraiDistintaDpcAsync(soapConDistinta, default);
        distinta.NumeroRicette.Should().Be(2);
    }
}

// ── FederfarmaFaultException ──────────────────────────────────────────────────

public sealed class FederfarmaFaultExceptionTests
{
    [Fact] public void Costruzione_e_proprieta()
    {
        var ex = new FederfarmaFaultException("AUTH_FAILED", "Credenziali errate");
        ex.Codice.Should().Be("AUTH_FAILED");
        ex.Dettaglio.Should().Be("Credenziali errate");
        ex.Message.Should().Contain("AUTH_FAILED").And.Contain("Credenziali errate");
    }
}
