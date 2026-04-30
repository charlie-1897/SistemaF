using FluentAssertions;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;
using Xunit;

namespace SistemaF.Domain.Tests.Anagrafica;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST SUITE — Anagrafica
// ═══════════════════════════════════════════════════════════════════════════════

// ── IndirizzoPosta ─────────────────────────────────────────────────────────────

public sealed class IndirizzoPostaTests
{
    [Fact] public void Da_crea_correttamente()
    {
        var i = IndirizzoPosta.Da("Via Roma 1", "20100", "Milano", "MI");
        i.Indirizzo.Should().Be("Via Roma 1");
        i.Cap.Should().Be("20100");
        i.Localita.Should().Be("Milano");
        i.Provincia.Should().Be("MI");
    }

    [Fact] public void Provincia_troncata_a_2_caratteri()
    {
        var i = IndirizzoPosta.Da("Via", "00100", "Roma", "Roma");
        i.Provincia.Should().Be("RO");
    }

    [Fact] public void Vuoto_ha_tutti_campi_vuoti()
    {
        var v = IndirizzoPosta.Vuoto;
        v.Indirizzo.Should().BeEmpty();
        v.Provincia.Should().BeEmpty();
    }

    [Fact] public void Uguaglianza_per_valore()
    {
        var a = IndirizzoPosta.Da("Via Roma 1", "20100", "Milano", "MI");
        var b = IndirizzoPosta.Da("Via Roma 1", "20100", "Milano", "MI");
        a.Should().Be(b);
    }

    [Fact] public void Indirizzi_diversi_non_uguali()
    {
        var a = IndirizzoPosta.Da("Via Roma 1", "20100", "Milano", "MI");
        var b = IndirizzoPosta.Da("Via Verdi 2", "20100", "Milano", "MI");
        a.Should().NotBe(b);
    }
}

// ── Fornitore ─────────────────────────────────────────────────────────────────

public sealed class FornitoreTests
{
    private static Fornitore GrossistaTest()
        => Fornitore.Crea("COMIFAR S.P.A.", TipoFornitore.Grossista, "01234567891");

    [Fact] public void Crea_grossista_genera_evento()
    {
        var f = GrossistaTest();
        f.RagioneSociale.Should().Be("COMIFAR S.P.A.");
        f.Tipo.Should().Be(TipoFornitore.Grossista);
        f.IsAttivo.Should().BeTrue();
        f.IsMagazzino.Should().BeFalse();
        f.DomainEvents.Should().ContainSingle(e => e is FornitoreCreato);
    }

    [Fact] public void Crea_magazzino_interno()
    {
        var m = Fornitore.Crea("MAGAZZINO INTERNO",
            TipoFornitore.Magazzino, isMagazzino: true);
        m.IsMagazzino.Should().BeTrue();
    }

    [Fact] public void Crea_ragione_sociale_vuota_lancia_eccezione()
        => ((Action)(() => Fornitore.Crea("", TipoFornitore.Grossista)))
           .Should().Throw<DomainException>();

    [Fact] public void ImpostaSede_aggiorna_correttamente()
    {
        var f = GrossistaTest();
        f.ImpostaSede(
            IndirizzoPosta.Da("Via Industria 10", "20060", "Cassina", "MI"),
            ContattoTelefonico.Da("02 9546111", email: "ordini@comifar.it"));
        f.Sede.Localita.Should().Be("Cassina");
        f.Contatti.Email.Should().Be("ordini@comifar.it");
    }

    [Fact] public void ImpostaParametriCommerciali_aggiorna()
    {
        var f = GrossistaTest();
        f.ImpostaParametriCommerciali(500_000m, 70, isPreferenziale: true);
        f.BudgetStimato.Should().Be(500_000m);
        f.PercentualeRipartizione.Should().Be(70);
        f.IsPreferenzialeDefault.Should().BeTrue();
    }

    [Fact] public void Percentuale_fuori_range_lancia_eccezione()
    {
        var f = GrossistaTest();
        ((Action)(() => f.ImpostaParametriCommerciali(1000m, 101)))
            .Should().Throw<DomainException>();
    }

    [Fact] public void Disattiva_cambia_stato_e_genera_evento()
    {
        var f = GrossistaTest();
        f.ClearDomainEvents();
        f.Disattiva();
        f.IsAttivo.Should().BeFalse();
        f.DomainEvents.Should().ContainSingle(e => e is FornitoreDisattivato);
    }

    [Fact] public void Disattiva_gia_disattivato_lancia_eccezione()
    {
        var f = GrossistaTest();
        f.Disattiva();
        ((Action)(() => f.Disattiva())).Should().Throw<DomainException>();
    }

    [Fact] public void ToInfoFornitore_mappa_correttamente()
    {
        var f = GrossistaTest();
        f.ImpostaCodiceAnabase(1001);
        f.ImpostaParametriCommerciali(100m, 70);
        var info = f.ToInfoFornitore();
        info.FornitoreId.Should().Be(f.Id);
        info.CodiceAnabase.Should().Be(1001);
        info.Tipo.Should().Be(TipoFornitore.Grossista);
        info.PercentualeRipartizione.Should().Be(70);
        info.IsMagazzino.Should().BeFalse();
    }

    [Fact] public void AggiornaDatiAnagrafici_aggiorna_ragione_sociale()
    {
        var f = GrossistaTest();
        f.AggiornaDatiAnagrafici("COMIFAR DISTRIBUZIONE S.P.A.");
        f.RagioneSociale.Should().Be("COMIFAR DISTRIBUZIONE S.P.A.");
        f.DomainEvents.Should().Contain(e => e is FornitoreDatiAggiornati);
    }
}

// ── Operatore ─────────────────────────────────────────────────────────────────

public sealed class OperatoreTests
{
    private const string HashTest = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918";

    private static Operatore OperatoreTest()
        => Operatore.Crea("mario.rossi", "Mario Rossi", HashTest);

    [Fact] public void Crea_operatore_correttamente()
    {
        var o = OperatoreTest();
        o.Login.Should().Be("mario.rossi");
        o.NomeCognome.Should().Be("Mario Rossi");
        o.IsAttivo.Should().BeTrue();
        o.IsAmministratore.Should().BeFalse();
    }

    [Fact] public void Login_normalizzato_a_minuscolo()
    {
        var o = Operatore.Crea("MARIO.ROSSI", "Mario Rossi", HashTest);
        o.Login.Should().Be("mario.rossi");
    }

    [Fact] public void Login_vuoto_lancia_eccezione()
        => ((Action)(() => Operatore.Crea("", "Nome", HashTest)))
           .Should().Throw<DomainException>();

    [Fact] public void VerificaPassword_corretta_restituisce_true()
        => OperatoreTest().VerificaPassword(HashTest).Should().BeTrue();

    [Fact] public void VerificaPassword_errata_restituisce_false()
        => OperatoreTest().VerificaPassword("hashsbagliato").Should().BeFalse();

    [Fact] public void CambiaPassword_aggiorna_hash()
    {
        var o = OperatoreTest();
        var nuovoHash = "abc123" + new string('0', 26);
        o.CambiaPassword(nuovoHash);
        o.VerificaPassword(nuovoHash).Should().BeTrue();
    }

    [Fact] public void ImpostaAutorizzazioniLegacy_lunghezza_corretta()
    {
        var o = OperatoreTest();
        var auth = new string('1', 50);
        o.ImpostaAutorizzazioniLegacy(auth);
        o.HasAutorizzazione(0).Should().BeTrue();
        o.HasAutorizzazione(49).Should().BeTrue();
    }

    [Fact] public void ImpostaAutorizzazioniLegacy_lunghezza_sbagliata_lancia()
        => ((Action)(() =>
            OperatoreTest().ImpostaAutorizzazioniLegacy("111")))
           .Should().Throw<DomainException>();

    [Fact] public void HasAutorizzazione_fuori_range_restituisce_false()
        => OperatoreTest().HasAutorizzazione(99).Should().BeFalse();

    [Fact] public void Disattiva_e_riattiva()
    {
        var o = OperatoreTest();
        o.Disattiva();
        o.IsAttivo.Should().BeFalse();
        o.Riattiva();
        o.IsAttivo.Should().BeTrue();
    }

    [Fact] public void Crea_amministratore()
    {
        var a = Operatore.Crea("admin", "Admin", HashTest, isAmministratore: true);
        a.IsAmministratore.Should().BeTrue();
    }
}

// ── ConfigurazioneEmissione ───────────────────────────────────────────────────

public sealed class ConfigurazioneEmissioneTests
{
    [Fact] public void Crea_configurazione_base()
    {
        var c = ConfigurazioneEmissione.Crea("Test Config");
        c.Nome.Should().Be("Test Config");
        c.IsAttiva.Should().BeTrue();
        c.DaArchivio.Should().BeTrue();    // default
        c.DaNecessita.Should().BeTrue();   // default
    }

    [Fact] public void ProfiloPredefinito_OrdineSettimanaleGrossista()
    {
        var c = ConfigurazioneEmissione.OrdineSettimanaleGrossista();
        c.Nome.Should().Be("Ordine settimanale grossista");
        c.DaArchivio.Should().BeTrue();
        c.TipoIndiceVendita.Should().Be(TipoIndiceVendita.Tendenziale);
        c.GiorniCopertura.Should().Be(7);
    }

    [Fact] public void ProfiloPredefinito_OrdineMensileADitta()
    {
        var c = ConfigurazioneEmissione.OrdineMensileADitta();
        c.OrdineLiberoDitta.Should().BeTrue();
        c.TipoIndiceVendita.Should().Be(TipoIndiceVendita.MesePrecedente);
        c.GiorniCopertura.Should().Be(30);
    }

    [Fact] public void ImpostaFonti_aggiorna()
    {
        var c = ConfigurazioneEmissione.Crea("Test");
        c.ImpostaFonti(true, false, true, false);
        c.DaArchivio.Should().BeTrue();
        c.DaNecessita.Should().BeFalse();
        c.DaPrenotati.Should().BeTrue();
    }

    [Fact] public void ToParametriIndice_converte_correttamente()
    {
        var c = ConfigurazioneEmissione.OrdineSettimanaleGrossista();
        var p = c.ToParametriIndice();
        p.Tipo.Should().Be(TipoIndiceVendita.Tendenziale);
        p.GiorniCopertura.Should().Be(7);
    }

    [Fact] public void ToParametriRipristino_converte()
    {
        var c = ConfigurazioneEmissione.OrdineSettimanaleGrossista();
        var p = c.ToParametriRipristino();
        p.Tipo.Should().Be(TipoRipristinoScorta.ScortaMinimaEsposizione);
    }

    [Fact] public void Disattiva_cambia_flag()
    {
        var c = ConfigurazioneEmissione.Crea("Test");
        c.Disattiva();
        c.IsAttiva.Should().BeFalse();
    }

    [Fact] public void Nome_vuoto_lancia_eccezione()
        => ((Action)(() => ConfigurazioneEmissione.Crea("")))
           .Should().Throw<DomainException>();
}

// ── ConfigurazioneEmissioneFornitore ─────────────────────────────────────────

public sealed class ConfigurazioneEmissioneFornitoreTests
{
    [Fact] public void Crea_correttamente()
    {
        var cf = ConfigurazioneEmissioneFornitore.Crea(
            Guid.NewGuid(), Guid.NewGuid(), ordine: 1, percentuale: 70);
        cf.OrdineIndice.Should().Be(1);
        cf.PercentualeRipartizione.Should().Be(70);
        cf.IsAbilitato.Should().BeTrue();
    }

    [Fact] public void Percentuale_superiore_100_viene_clampata()
    {
        var cf = ConfigurazioneEmissioneFornitore.Crea(
            Guid.NewGuid(), Guid.NewGuid(), percentuale: 150);
        cf.PercentualeRipartizione.Should().Be(100);
    }

    [Fact] public void Guid_vuoto_lancia_eccezione()
        => ((Action)(() =>
            ConfigurazioneEmissioneFornitore.Crea(Guid.Empty, Guid.NewGuid())))
           .Should().Throw<DomainException>();
}
