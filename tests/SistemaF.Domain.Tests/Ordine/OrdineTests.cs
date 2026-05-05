using Ordine = SistemaF.Domain.Entities.Ordine.Ordine;
using FluentAssertions;
using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Ordine;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;
using Xunit;

namespace SistemaF.Domain.Tests.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST SUITE — Modulo Ordine
//  Copertura: NumeroOrdine, CostoFornitore, PropostaRiga,
//             PropostaOrdine, Ordine, transizioni di stato
// ═══════════════════════════════════════════════════════════════════════════════

// ── Helpers ───────────────────────────────────────────────────────────────────

internal static class OrdineTestFactory
{
    public static PropostaOrdine Proposta(Guid? operatoreId = null)
    {
        var p = PropostaOrdine.Crea(
            operatoreId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            "Emissione Test Grossista");
        p.ImpostaFonti(true, true, false, false);
        p.AggiungiFornitori([
            new InfoFornitore(Guid.NewGuid(), 1001, TipoFornitore.Grossista, false, 100, false)
        ]);
        return p;
    }

    public static PropostaRiga Riga(Guid? propostaId = null)
        => PropostaRiga.Crea(
            propostaId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            CodiceProdotto.Da("012345678"),
            "TACHIPIRINA 1000MG");

    public static InfoFornitore Grossista()
        => new(Guid.NewGuid(), 1001, TipoFornitore.Grossista, false, 100, false);

    public static NumeroOrdine Numero2025()
        => NumeroOrdine.Da(2025, 1);
}

// ── NumeroOrdine ──────────────────────────────────────────────────────────────

public sealed class NumeroOrdineTests
{
    [Fact] public void Formato_corretto()
        => NumeroOrdine.Da(2025, 1).Valore.Should().Be("202500001");

    [Fact] public void Anno_e_progressivo_estratti_correttamente()
    {
        var n = NumeroOrdine.Da(2025, 99);
        n.Anno.Should().Be(2025);
        n.Progressivo.Should().Be(99);
    }

    [Fact] public void Parsing_da_stringa()
        => NumeroOrdine.Da("202500042").Progressivo.Should().Be(42);

    [Fact] public void Formato_invalido_lancia_eccezione()
        => ((Action)(() => NumeroOrdine.Da("ABC"))).Should().Throw<DomainException>();

    [Theory]
    [InlineData(1999)]
    [InlineData(2100)]
    public void Anno_fuori_range_lancia_eccezione(int anno)
        => ((Action)(() => NumeroOrdine.Da(anno, 1))).Should().Throw<DomainException>();
}

// ── CostoFornitore ─────────────────────────────────────────────────────────────

public sealed class CostoFornitoreTests
{
    [Fact] public void Zero_ha_tutti_campi_zero()
    {
        var z = CostoFornitore.Zero;
        z.Imponibile.Should().Be(0);
        z.Sconto.Should().Be(0);
    }

    [Fact] public void CostoReale_senza_extra_sconto_uguale_imponibile()
        => CostoFornitore.Da(10m).CostoReale().Should().Be(10m);

    [Fact] public void CostoReale_con_extra_sconto_3_percento()
        => CostoFornitore.Da(10m).CostoReale(3m).Should().BeApproximately(9.7m, 0.01m);

    [Fact] public void Imponibile_negativo_lancia_eccezione()
        => ((Action)(() => CostoFornitore.Da(-1m))).Should().Throw<DomainException>();

    [Fact] public void Sconto_oltre_100_lancia_eccezione()
        => ((Action)(() => CostoFornitore.Da(10m, 101m))).Should().Throw<DomainException>();

    [Fact] public void Uguaglianza_per_valore()
    {
        var c1 = CostoFornitore.Da(5m, 10m);
        var c2 = CostoFornitore.Da(5m, 10m);
        c1.Should().Be(c2);
    }
}

// ── PropostaRiga ──────────────────────────────────────────────────────────────

public sealed class PropostaRigaTests
{
    [Fact]
    public void Crea_riga_valida()
    {
        var r = OrdineTestFactory.Riga();
        r.QuantitaTotale.Should().Be(0);
        r.HaFornitoriAbilitati.Should().BeFalse();
        r.DaOrdinare.Should().BeTrue();
    }

    [Fact]
    public void AggiungiQuantita_per_fonte_incrementa_campo_corretto()
    {
        var r = OrdineTestFactory.Riga();
        r.AggiungiQuantita(FonteAggiunta.Mancanti, 5);
        r.AggiungiQuantita(FonteAggiunta.Prenotati, 3);
        r.AggiungiQuantita(FonteAggiunta.Sospesi, 2);

        r.QuantitaMancante.Should().Be(5);
        r.QuantitaPrenotata.Should().Be(3);
        r.QuantitaSospesa.Should().Be(2);
        r.QuantitaTotale.Should().Be(10);
    }

    [Fact]
    public void AbilitaFornitore_e_imposta_quantita()
    {
        var r = OrdineTestFactory.Riga();
        r.AbilitaFornitore(1, true);
        r.ImpostaQuantitaFornitore(1, 12);

        r.IsFornitoreAbilitato(1).Should().BeTrue();
        r.QuantitaPerFornitore(1).Should().Be(12);
        r.TotaleQuantita.Should().Be(12);
    }

    [Fact]
    public void AbilitaSoloFornitore_disabilita_gli_altri()
    {
        var r = OrdineTestFactory.Riga();
        r.AbilitaFornitore(1, true);
        r.AbilitaFornitore(2, true);
        r.AbilitaFornitore(3, true);

        r.AbilitaSoloFornitore(2);

        r.IsFornitoreAbilitato(1).Should().BeFalse();
        r.IsFornitoreAbilitato(2).Should().BeTrue();
        r.IsFornitoreAbilitato(3).Should().BeFalse();
    }

    [Fact]
    public void QuantitaSottoOrdinata_calcolata_correttamente()
    {
        var r = OrdineTestFactory.Riga();
        r.AggiungiQuantita(FonteAggiunta.Necessita, 10);
        r.AbilitaFornitore(1, true);
        r.ImpostaQuantitaFornitore(1, 8);

        r.QuantitaSottoOrdinata.Should().Be(-2);  // sotto-ordinato
    }

    [Fact]
    public void CostoTotale_esclude_omaggio()
    {
        var r = OrdineTestFactory.Riga();
        r.AbilitaFornitore(1, true);
        r.ImpostaQuantitaFornitore(1, 10);
        r.ImpostaOmaggioFornitore(1, 2);
        r.ImpostaCostoFornitore(1, CostoFornitore.Da(5m));

        r.CalcolaCostoTotale().Should().Be(40m);  // (10 - 2) * 5
    }

    [Fact]
    public void Indice_fornitore_invalido_lancia_eccezione()
    {
        var r = OrdineTestFactory.Riga();
        ((Action)(() => r.AbilitaFornitore(6, true))).Should().Throw<DomainException>();
        ((Action)(() => r.AbilitaFornitore(0, true))).Should().Throw<DomainException>();
    }
}

// ── PropostaOrdine ────────────────────────────────────────────────────────────

public sealed class PropostaOrdineTests
{
    [Fact]
    public void Crea_proposta_con_evento_PropostaOrdineCreata()
    {
        var p = OrdineTestFactory.Proposta();
        p.Stato.Should().Be(PropostaOrdine.StatoProposta.Bozza);
        p.DomainEvents.Should().ContainSingle(e => e is PropostaOrdineCreata);
    }

    [Fact]
    public void ImpostaFonti_configura_correttamente()
    {
        var p = OrdineTestFactory.Proposta();
        p.ImpostaFonti(true, false, true, false);

        p.IsEmissioneDaArchivio.Should().BeTrue();
        p.IsEmissioneNecessita.Should().BeFalse();
        p.IsEmissionePrenotati.Should().BeTrue();
        p.IsEmissioneSospesi.Should().BeFalse();
    }

    [Fact]
    public void AggiungiFornitori_detecta_magazzino()
    {
        var p = OrdineTestFactory.Proposta();
        p.AggiungiFornitori([
            new InfoFornitore(Guid.NewGuid(), 1001, TipoFornitore.Grossista, false, 70, false),
            new InfoFornitore(Guid.NewGuid(), 9999, TipoFornitore.Magazzino, false, 30, true),
        ]);

        p.IsMagazzinoPresente.Should().BeTrue();
        p.IndiceMagazzino.Should().Be(2);
    }

    [Fact]
    public void AggiungiProdottoManuale_crea_riga_con_evento()
    {
        var p    = OrdineTestFactory.Proposta();
        var pid  = Guid.NewGuid();
        var cod  = CodiceProdotto.Da("012345678");

        p.ClearDomainEvents();
        p.AggiungiProdottoManuale(pid, cod, "ASPIRINA 500MG", 10,
            FonteAggiunta.Mancanti);

        p.Righe.Should().HaveCount(1);
        p.DomainEvents.Should().ContainSingle(e => e is ProdottoAggiuntoProposta);
    }

    [Fact]
    public void AggiungiProdottoManuale_stesso_prodotto_incrementa_quantita()
    {
        var p   = OrdineTestFactory.Proposta();
        var pid = Guid.NewGuid();
        var cod = CodiceProdotto.Da("012345678");

        p.AggiungiProdottoManuale(pid, cod, "BRUFEN", 5, FonteAggiunta.Necessita);
        p.AggiungiProdottoManuale(pid, cod, "BRUFEN", 3, FonteAggiunta.Necessita);

        p.Righe.Should().HaveCount(1);
        p.Righe[0].QuantitaNecessita.Should().Be(8);
    }

    [Fact]
    public void EliminaProdotto_marca_da_eliminare()
    {
        var p   = OrdineTestFactory.Proposta();
        var pid = Guid.NewGuid();
        var cod = CodiceProdotto.Da("012345678");

        p.AggiungiProdottoManuale(pid, cod, "PRODOTTO X", 5, FonteAggiunta.Necessita);
        p.EliminaProdotto(pid);

        p.Righe[0].DaEliminare.Should().BeTrue();
        p.NumeroProdottiInProposta.Should().Be(0);
    }

    [Fact]
    public void EliminaProdotto_inesistente_lancia_eccezione()
    {
        var p = OrdineTestFactory.Proposta();
        ((Action)(() => p.EliminaProdotto(Guid.NewGuid()))).Should()
            .Throw<EntityNotFoundException>();
    }

    [Fact]
    public void MarcaCompletata_transisce_stato_e_genera_evento()
    {
        var p         = OrdineTestFactory.Proposta();
        var riepilogo = new RiepilogoElaborazione("Test", DateTime.UtcNow, DateTime.UtcNow, 100, 50, 30);

        p.MarcaCompletata(riepilogo);

        p.Stato.Should().Be(PropostaOrdine.StatoProposta.Completata);
        p.DomainEvents.Should().Contain(e => e is PropostaOrdineCompletata);
        p.Riepilogo.NumeroProdottiInOrdine.Should().Be(30);
    }

    [Fact]
    public void MarcaCompletata_su_proposta_non_bozza_lancia_eccezione()
    {
        var p = OrdineTestFactory.Proposta();
        p.MarcaCompletata(RiepilogoElaborazione.Vuoto);
        ((Action)(() => p.MarcaCompletata(RiepilogoElaborazione.Vuoto))).Should()
            .Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Annulla_su_bozza_genera_evento()
    {
        var p = OrdineTestFactory.Proposta();
        p.Annulla();
        p.Stato.Should().Be(PropostaOrdine.StatoProposta.Annullata);
        p.DomainEvents.Should().Contain(e => e is PropostaOrdineAnnullata);
    }

    [Fact]
    public void Annulla_su_proposta_emessa_lancia_eccezione()
    {
        var p   = OrdineTestFactory.Proposta();
        var pid = Guid.NewGuid();
        var cod = CodiceProdotto.Da("012345678");

        // Simula una riga con fornitore assegnato per poter emettere
        p.AggiungiProdottoManuale(pid, cod, "TEST", 5, FonteAggiunta.Necessita, 1);
        var riga = (PropostaRiga)p.Righe[0];
        riga.ImpostaQuantitaFornitore(1, 5);
        p.MarcaCompletata(RiepilogoElaborazione.Vuoto);
        p.MarcaEmessa(Guid.NewGuid());

        ((Action)(() => p.Annulla())).Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Interrompi_imposta_flag()
    {
        var p = OrdineTestFactory.Proposta();
        p.Interrompi();
        p.Interrotta.Should().BeTrue();
    }
}

// ── Ordine ────────────────────────────────────────────────────────────────────

public sealed class OrdineTests
{
    private static (PropostaOrdine proposta, SistemaF.Domain.Entities.Ordine.Ordine ordine) CreaOrdineTest()
    {
        var operatoreId  = Guid.NewGuid();
        var proposta     = OrdineTestFactory.Proposta(operatoreId);
        var fornitore    = OrdineTestFactory.Grossista();
        var prodottoId   = Guid.NewGuid();
        var cod          = CodiceProdotto.Da("023456789");

        proposta.AggiungiFornitori([fornitore]);

        // Aggiunge prodotto manuale e simula assegnazione fornitore
        proposta.AggiungiProdottoManuale(prodottoId, cod, "AMOXICILLINA 500MG", 12,
            FonteAggiunta.Necessita, 1);
        var riga = (PropostaRiga)proposta.Righe[0];
        riga.ImpostaQuantitaFornitore(1, 12);
        riga.AbilitaFornitore(1, true);
        riga.ImpostaPrezzi(8.50m, 6.20m, 0, 0, 10, "A");
        riga.ImpostaCostoFornitore(1, CostoFornitore.Da(4.50m, 32m));

        proposta.MarcaCompletata(RiepilogoElaborazione.Vuoto);

        var ordine = Ordine.DaProposta(
            OrdineTestFactory.Numero2025(),
            proposta, fornitore, "DISTRIBUZIONE SRL");

        return (proposta, ordine);
    }

    [Fact]
    public void DaProposta_crea_ordine_con_righe_e_evento()
    {
        var (_, ordine) = CreaOrdineTest();

        ordine.Stato.Should().Be(StatoOrdine.Emesso);
        ordine.NumeroRighe.Should().Be(1);
        ordine.Righe[0].Quantita.Should().Be(12);
        ordine.DomainEvents.Should().ContainSingle(e => e is OrdineCreato);
    }

    [Fact]
    public void ImportoTotale_calcolato_correttamente()
    {
        var (_, ordine) = CreaOrdineTest();
        // 12 pezzi * €4.50 = €54
        ordine.ImportoTotale.Should().Be(54m);
    }

    [Fact]
    public void DaProposta_su_proposta_non_completata_lancia_eccezione()
    {
        var proposta  = OrdineTestFactory.Proposta();
        var fornitore = OrdineTestFactory.Grossista();
        proposta.AggiungiFornitori([fornitore]);

        ((Action)(() => Ordine.DaProposta(
            OrdineTestFactory.Numero2025(),
            proposta, fornitore, "TEST SRL")))
        .Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Trasmetti_cambia_stato_e_genera_evento()
    {
        var (_, ordine) = CreaOrdineTest();
        ordine.Trasmetti();

        ordine.Stato.Should().Be(StatoOrdine.Trasmesso);
        ordine.DataTrasmissione.Should().NotBeNull();
        ordine.DomainEvents.Should().Contain(e => e is OrdineTrasmesso);
    }

    [Fact]
    public void Trasmetti_su_ordine_gia_trasmesso_lancia_eccezione()
    {
        var (_, ordine) = CreaOrdineTest();
        ordine.Trasmetti();
        ((Action)(() => ordine.Trasmetti())).Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void RegistraRicezione_cambia_stato_e_genera_evento()
    {
        var (_, ordine) = CreaOrdineTest();
        ordine.Trasmetti();
        ordine.RegistraRicezione(DateTime.Today);

        ordine.Stato.Should().Be(StatoOrdine.Ricevuto);
        ordine.DataRicezione.Should().NotBeNull();
        ordine.DomainEvents.Should().Contain(e => e is OrdineRicevuto);
    }

    [Fact]
    public void Annulla_genera_evento_e_soft_delete()
    {
        var (_, ordine) = CreaOrdineTest();
        ordine.Annulla("Test annullo");

        ordine.IsDeleted.Should().BeTrue();
        ordine.DomainEvents.Should().Contain(e => e is OrdineAnnullato);
    }

    [Fact]
    public void Annulla_su_ordine_ricevuto_lancia_eccezione()
    {
        var (_, ordine) = CreaOrdineTest();
        ordine.Trasmetti();
        ordine.RegistraRicezione(DateTime.Today);

        ((Action)(() => ordine.Annulla("impossibile"))).Should()
            .Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void ImpostaRicevuto_aggiorna_riga()
    {
        var (_, ordine) = CreaOrdineTest();
        var prodottoId  = ordine.Righe[0].ProdottoId;

        ordine.ImpostaRicevuto(prodottoId, qtaArrivata: 10, qtaAssicurata: 12, codiceMancante: "M001");

        ordine.Righe[0].QuantitaArrivata.Should().Be(10);
        ordine.Righe[0].QuantitaAssicurata.Should().Be(12);
        ordine.Righe[0].CodiceMancante.Should().Be("M001");
    }
}

// ── RiepilogoElaborazione ─────────────────────────────────────────────────────

public sealed class RiepilogoElaborazioneTests
{
    [Fact]
    public void DurataSecondi_calcolata_correttamente()
    {
        var inizio = DateTime.UtcNow.AddSeconds(-30);
        var r = new RiepilogoElaborazione("Test", inizio, DateTime.UtcNow, 100, 50, 30);
        r.DurataSecondi.Should().BeApproximately(30, 1);
    }

    [Fact]
    public void Vuoto_ha_tutti_campi_a_zero()
    {
        var v = RiepilogoElaborazione.Vuoto;
        v.NumeroProdottiInOrdine.Should().Be(0);
        v.NomeEmissione.Should().BeEmpty();
    }
}

// ── ParametriIndiceDiVendita ──────────────────────────────────────────────────

public sealed class ParametriIndiceDiVenditaTests
{
    [Fact]
    public void Vuoto_ha_valori_default()
    {
        var p = ParametriIndiceDiVendita.Vuoto;
        p.GiorniCopertura.Should().Be(0);
        p.IsInclude.Should().BeTrue();
    }
}
