using FluentAssertions;
using SistemaF.Domain.Common;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;
using Xunit;

namespace SistemaF.Domain.Tests.Prodotto;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST SUITE — Modulo Prodotto
//  Copertura: Value Objects, Prodotto (giacenze, prezzi, lotti, scadenze)
// ═══════════════════════════════════════════════════════════════════════════════

// ── Helper ────────────────────────────────────────────────────────────────────

internal static class ProdottoFactory
{
    public static SistemaF.Domain.Entities.Prodotto.Prodotto Tachipirina() => SistemaF.Domain.Entities.Prodotto.Prodotto.Crea(
        CodiceProdotto.Da("012345678"),
        "TACHIPIRINA 1000MG 20 CPR",
        ClasseFarmaco.C,
        CategoriaRicetta.NessunObbligo,
        Prezzo.Di(4.50m));

    public static SistemaF.Domain.Entities.Prodotto.Prodotto Amoxicillina() => SistemaF.Domain.Entities.Prodotto.Prodotto.Crea(
        CodiceProdotto.Da("023456789"),
        "AMOXICILLINA 500MG 12 CPR",
        ClasseFarmaco.A,
        CategoriaRicetta.RRipetibile,
        Prezzo.Di(5.80m));
}

// ── Value Object Tests ────────────────────────────────────────────────────────

public sealed class CodiceProdottoTests
{
    [Fact] public void Nove_cifre_valido()
        => CodiceProdotto.Da("012345678").Valore.Should().Be("012345678");

    [Fact] public void Codice_corto_viene_completato_con_zeri()
        => CodiceProdotto.Da("12345").Valore.Should().Be("000012345");

    [Theory]
    [InlineData("ABCDEFGHI")]
    [InlineData("0123456789")] // 10 cifre
    [InlineData("")]
    public void Codice_invalido_lancia_eccezione(string valore)
        => ((Action)(() => CodiceProdotto.Da(valore))).Should()
              .Throw<DomainException>().Which.ErrorCode.Should().Be("CODICE_PRODOTTO_INVALIDO");

    [Fact] public void TryCreate_restituisce_fail_senza_lanciare()
        => CodiceProdotto.TryCreate("ABC").IsFailure.Should().BeTrue();
}

public sealed class CodiceEANTests
{
    [Fact] public void EAN_valido_con_check_digit_corretto()
        => CodiceEAN.Da("8001234567894").Valore.Should().Be("8001234567894");

    [Fact] public void EAN_con_check_digit_errato_lancia_eccezione()
        => ((Action)(() => CodiceEAN.Da("8001234567895"))).Should()
              .Throw<DomainException>().Which.ErrorCode.Should().Be("EAN_CHECK_DIGIT_INVALIDO");

    [Fact] public void EAN_troppo_corto_lancia_eccezione()
        => ((Action)(() => CodiceEAN.Da("123"))).Should()
              .Throw<DomainException>().Which.ErrorCode.Should().Be("EAN_INVALIDO");
}

public sealed class PrezzoTests
{
    [Fact] public void Prezzo_valido_con_aliquota_10()
        => Prezzo.Di(9.90m, 10).Importo.Should().Be(9.90m);

    [Fact] public void IVA_inclusa_calcolata_correttamente()
        => Prezzo.Di(10.00m, 10).ImportoIVAInclusa.Should().Be(11.00m);

    [Fact] public void Prezzo_negativo_lancia_eccezione()
        => ((Action)(() => Prezzo.Di(-1m))).Should().Throw<DomainException>();

    [Fact] public void Aliquota_IVA_invalida_lancia_eccezione()
        => ((Action)(() => Prezzo.Di(10m, 15))).Should()
              .Throw<DomainException>().Which.ErrorCode.Should().Be("ALIQUOTA_IVA_INVALIDA");

    [Fact] public void Sconto_percentuale_applicato_correttamente()
    {
        var scontato = Prezzo.Di(10.00m).ApplicaSconto(10m);
        scontato.Importo.Should().Be(9.00m);
    }

    [Fact] public void Due_prezzi_uguali_sono_uguali()
        => Prezzo.Di(5.00m, 10).Should().Be(Prezzo.Di(5.00m, 10));

    [Fact] public void Prezzi_con_aliquote_diverse_sono_diversi()
        => Prezzo.Di(5.00m, 10).Should().NotBe(Prezzo.Di(5.00m, 22));
}

public sealed class CodiceLottoTests
{
    [Fact] public void Lotto_valido_viene_uppercase()
        => CodiceLotto.Da("abc123").Valore.Should().Be("ABC123");

    [Fact] public void Lotto_vuoto_lancia_eccezione()
        => ((Action)(() => CodiceLotto.Da(""))).Should().Throw<DomainException>();

    [Fact] public void Lotto_troppo_lungo_lancia_eccezione()
        => ((Action)(() => CodiceLotto.Da(new string('A', 31)))).Should().Throw<DomainException>();
}

// ── Prodotto — Creazione ──────────────────────────────────────────────────────

public sealed class ProdottoCreazioneTests
{
    [Fact]
    public void Crea_prodotto_valido_con_evento_ProdottoCreato()
    {
        var p = ProdottoFactory.Tachipirina();

        p.CodiceFarmaco.Valore.Should().Be("012345678");
        p.Descrizione.Should().Be("TACHIPIRINA 1000MG 20 CPR");
        p.IsAttivo.Should().BeTrue();
        p.Classe.Should().Be(ClasseFarmaco.C);
        p.DomainEvents.Should().ContainSingle(e => e is ProdottoCreato);
    }

    [Fact]
    public void Prodotto_classe_A_e_mutuabile()
        => ProdottoFactory.Amoxicillina().IsMutuabile.Should().BeTrue();

    [Fact]
    public void Prodotto_classe_C_non_mutuabile()
        => ProdottoFactory.Tachipirina().IsMutuabile.Should().BeFalse();

    [Fact]
    public void Descrizione_vuota_lancia_eccezione()
        => ((Action)(() => SistemaF.Domain.Entities.Prodotto.Prodotto.Crea(
                CodiceProdotto.Da("012345678"), "",
                ClasseFarmaco.C, CategoriaRicetta.NessunObbligo,
                Prezzo.Di(4m)))).Should().Throw<DomainException>();
}

// ── Prodotto — Prezzi ─────────────────────────────────────────────────────────

public sealed class ProdottoPrezziTests
{
    [Fact]
    public void AggiornaPrezzo_aggiorna_e_genera_evento()
    {
        var p = ProdottoFactory.Tachipirina();
        p.ClearDomainEvents();

        p.AggiornaPrezzo(Prezzo.Di(5.00m));

        p.PrezzoVendita.Importo.Should().Be(5.00m);
        p.DomainEvents.OfType<PrezzoProdottoAggiornato>()
            .Should().ContainSingle(e => e.TipoCosa == TipoCosaRettifica.PrezzoPubblico);
    }

    [Fact]
    public void AggiornaPrezzo_su_prodotto_inattivo_lancia_eccezione()
    {
        var p = ProdottoFactory.Tachipirina();
        p.Disattiva();

        ((Action)(() => p.AggiornaPrezzo(Prezzo.Di(5m)))).Should()
            .Throw<BusinessRuleViolationException>();
    }
}

// ── Prodotto — Giacenze ───────────────────────────────────────────────────────

public sealed class ProdottoGiacenzeTests
{
    [Fact]
    public void Varia_giacenza_esposizione_sostituzione_aggiorna_valore()
    {
        var p = ProdottoFactory.Tachipirina();
        p.VariaGiacenzaEsposizione(
            ModalitaVariazioneGiacenza.Sostituzione, 20,
            TipoModuloRettifica.Magazzino);

        p.GiacenzaEsposizione.Giacenza.Should().Be(20);
    }

    [Fact]
    public void Varia_giacenza_aggiunta_incrementa_valore()
    {
        var p = ProdottoFactory.Tachipirina();
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 10,
            TipoModuloRettifica.Magazzino);
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Aggiunta, 5,
            TipoModuloRettifica.OrdineRicarico);

        p.GiacenzaEsposizione.Giacenza.Should().Be(15);
    }

    [Fact]
    public void Varia_giacenza_sottrazione_decrementa_valore()
    {
        var p = ProdottoFactory.Tachipirina();
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 10,
            TipoModuloRettifica.Magazzino);
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sottrazione, 3,
            TipoModuloRettifica.Vendita);

        p.GiacenzaEsposizione.Giacenza.Should().Be(7);
    }

    [Fact]
    public void Giacenza_negativa_lancia_eccezione()
    {
        var p = ProdottoFactory.Tachipirina();
        ((Action)(() =>
            p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sottrazione, 5,
                TipoModuloRettifica.Vendita)))
        .Should().Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void Variazione_genera_evento_GiacenzaVariata()
    {
        var p = ProdottoFactory.Tachipirina();
        p.ClearDomainEvents();

        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 10,
            TipoModuloRettifica.Magazzino);

        p.DomainEvents.Should().ContainSingle(e => e is GiacenzaVariata);
    }

    [Fact]
    public void Giacenza_sotto_scorta_genera_evento_SottoscortaRilevata()
    {
        var p = ProdottoFactory.Tachipirina();
        p.ImpostaScorteEsposizione(5, 20);
        p.ClearDomainEvents();

        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 3,
            TipoModuloRettifica.Vendita);

        p.IsSottoscorta.Should().BeTrue();
        p.DomainEvents.Should().Contain(e => e is SottoscortaRilevata);
    }

    [Fact]
    public void Scorta_massima_minore_di_minima_lancia_eccezione()
    {
        var p = ProdottoFactory.Tachipirina();
        ((Action)(() => p.ImpostaScorteEsposizione(10, 5))).Should()
            .Throw<BusinessRuleViolationException>();
    }

    [Fact]
    public void AzzeraGiacenza_imposta_zero()
    {
        var p = ProdottoFactory.Tachipirina();
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 50,
            TipoModuloRettifica.Magazzino);
        p.AzzeraGiacenzaEsposizione();

        p.GiacenzaEsposizione.Giacenza.Should().Be(0);
    }

    [Fact]
    public void GiacenzaTotale_somma_esposizione_e_magazzino()
    {
        var p = ProdottoFactory.Tachipirina();
        p.VariaGiacenzaEsposizione(ModalitaVariazioneGiacenza.Sostituzione, 10,
            TipoModuloRettifica.Magazzino);
        p.VariaGiacenzaMagazzino(ModalitaVariazioneGiacenza.Sostituzione, 30,
            TipoModuloRettifica.Magazzino);

        p.GiacenzaTotale.Should().Be(40);
    }
}

// ── Prodotto — Lotti e scadenze ───────────────────────────────────────────────

public sealed class ProdottoLottiTests
{
    [Fact]
    public void AggiungLotto_aggiunge_scadenza()
    {
        var p     = ProdottoFactory.Tachipirina();
        var lotto = CodiceLotto.Da("LOT001");
        var data  = DateOnly.FromDateTime(DateTime.Today.AddDays(365));

        p.AggiungLotto(lotto, data, 50);

        p.Scadenze.Should().ContainSingle(s => s.Lotto == lotto);
    }

    [Fact]
    public void AggiungLotto_stesso_lotto_due_volte_incrementa_quantita()
    {
        var p     = ProdottoFactory.Tachipirina();
        var lotto = CodiceLotto.Da("LOT001");
        var data  = DateOnly.FromDateTime(DateTime.Today.AddDays(365));

        p.AggiungLotto(lotto, data, 20);
        p.AggiungLotto(lotto, data, 30);

        p.Scadenze.Should().ContainSingle();
        p.Scadenze[0].Quantita.Should().Be(50);
    }

    [Fact]
    public void Lotto_in_scadenza_entro_90_giorni_genera_evento()
    {
        var p     = ProdottoFactory.Tachipirina();
        var lotto = CodiceLotto.Da("LOT002");
        var data  = DateOnly.FromDateTime(DateTime.Today.AddDays(30));

        p.ClearDomainEvents();
        p.AggiungLotto(lotto, data, 10);

        p.DomainEvents.Should().Contain(e => e is ProdottoInScadenza);
    }

    [Fact]
    public void Lotto_lontano_dalla_scadenza_non_genera_evento()
    {
        var p     = ProdottoFactory.Tachipirina();
        var lotto = CodiceLotto.Da("LOT003");
        var data  = DateOnly.FromDateTime(DateTime.Today.AddDays(365));

        p.ClearDomainEvents();
        p.AggiungLotto(lotto, data, 10);

        p.DomainEvents.Should().NotContain(e => e is ProdottoInScadenza);
    }

    [Fact]
    public void ScadenzeEntro_restituisce_solo_scadenze_nel_periodo()
    {
        var p = ProdottoFactory.Tachipirina();
        p.AggiungLotto(CodiceLotto.Da("L1"), DateOnly.FromDateTime(DateTime.Today.AddDays(30)), 10);
        p.AggiungLotto(CodiceLotto.Da("L2"), DateOnly.FromDateTime(DateTime.Today.AddDays(200)), 10);

        var inScadenza = p.ScadenzeEntro(90).ToList();

        inScadenza.Should().HaveCount(1);
        inScadenza[0].Lotto.Valore.Should().Be("L1");
    }

    [Fact]
    public void RimuoviLotto_inesistente_lancia_eccezione()
    {
        var p = ProdottoFactory.Tachipirina();
        ((Action)(() => p.RimuoviLotto(CodiceLotto.Da("NONEXIST")))).Should()
            .Throw<EntityNotFoundException>();
    }
}

// ── Prodotto — Invendibili e ciclo di vita ────────────────────────────────────

public sealed class ProdottoCicloVitaTests
{
    [Fact]
    public void Disattiva_genera_evento_e_marca_come_eliminato()
    {
        var p = ProdottoFactory.Tachipirina();
        p.Disattiva();

        p.IsAttivo.Should().BeFalse();
        p.IsDeleted.Should().BeTrue();
        p.DomainEvents.Should().Contain(e => e is ProdottoDisattivato);
    }

    [Fact]
    public void Disattiva_due_volte_lancia_eccezione()
    {
        var p = ProdottoFactory.Tachipirina();
        p.Disattiva();
        ((Action)(() => p.Disattiva())).Should().Throw<DomainException>();
    }

    [Fact]
    public void ImpostaInvendibile_genera_evento()
    {
        var p = ProdottoFactory.Tachipirina();
        p.ClearDomainEvents();
        p.ImpostaInvendibile(true, 5);

        p.IsInvendibile.Should().BeTrue();
        p.GiacenzaInvendibile.Should().Be(5);
        p.DomainEvents.Should().Contain(e => e is ProdottoMarcatoInvendibile);
    }

    [Fact]
    public void ImpostaInvendibile_false_azzera_giacenza_invendibile()
    {
        var p = ProdottoFactory.Tachipirina();
        p.ImpostaInvendibile(true, 10);
        p.ImpostaInvendibile(false);

        p.IsInvendibile.Should().BeFalse();
        p.GiacenzaInvendibile.Should().Be(0);
    }
}

// ── GiacenzaMagazzino Value Object ────────────────────────────────────────────

public sealed class GiacenzaMagazzinoTests
{
    [Fact]
    public void Zero_ha_giacenza_zero_e_scorte_zero()
    {
        GiacenzaMagazzino.Zero.Giacenza.Should().Be(0);
        GiacenzaMagazzino.Zero.IsScorteConfigurate.Should().BeFalse();
    }

    [Fact]
    public void Applica_sostituzione()
        => GiacenzaMagazzino.Crea(10).Applica(ModalitaVariazioneGiacenza.Sostituzione, 25)
              .Giacenza.Should().Be(25);

    [Fact]
    public void Applica_aggiunta()
        => GiacenzaMagazzino.Crea(10).Applica(ModalitaVariazioneGiacenza.Aggiunta, 5)
              .Giacenza.Should().Be(15);

    [Fact]
    public void Applica_sottrazione()
        => GiacenzaMagazzino.Crea(10).Applica(ModalitaVariazioneGiacenza.Sottrazione, 3)
              .Giacenza.Should().Be(7);

    [Fact]
    public void Applica_sottrazione_negativa_lancia_eccezione()
        => ((Action)(() => GiacenzaMagazzino.Crea(5)
                .Applica(ModalitaVariazioneGiacenza.Sottrazione, 10)))
              .Should().Throw<BusinessRuleViolationException>();

    [Fact]
    public void Immutabilita_Applica_non_modifica_originale()
    {
        var originale = GiacenzaMagazzino.Crea(10);
        _ = originale.Applica(ModalitaVariazioneGiacenza.Aggiunta, 99);
        originale.Giacenza.Should().Be(10);
    }

    [Fact]
    public void ConScorte_preserva_giacenza()
    {
        var g = GiacenzaMagazzino.Crea(10).ConScorte(5, 20);
        g.Giacenza.Should().Be(10);
        g.ScortaMinima.Should().Be(5);
        g.ScortaMassima.Should().Be(20);
    }

    [Fact]
    public void Uguaglianza_per_valore()
    {
        var g1 = GiacenzaMagazzino.Crea(10, 5, 20);
        var g2 = GiacenzaMagazzino.Crea(10, 5, 20);
        g1.Should().Be(g2);
    }
}
