using FluentAssertions;
using SistemaF.Domain.Common;
using Xunit;

namespace SistemaF.Domain.Tests.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  TEST SUITE — Domain Common Layer
//  Copertura: Entity, AggregateRoot, ValueObject, Guard, Result, Enumeration
// ═══════════════════════════════════════════════════════════════════════════════

// ── Entità di supporto per i test ─────────────────────────────────────────────

internal sealed class ProdottoFake : AggregateRoot
{
    public string Nome { get; private set; } = string.Empty;
    public int    Quantita { get; private set; }

    private ProdottoFake() { }

    public static ProdottoFake Crea(string nome, int quantita, Guid? operatoreId = null)
    {
        Guard.AgainstNullOrEmpty(nome);
        Guard.AgainstNegative(quantita);

        var p = new ProdottoFake
        {
            Nome     = nome,
            Quantita = quantita
        };
        p.Raise(new ProdottoFakeCreato(p.Id, nome));
        return p;
    }

    public void CambiaQuantita(int nuovaQty, Guid? operatoreId = null)
    {
        Guard.AgainstNegative(nuovaQty);
        Quantita = nuovaQty;
        SetUpdated(operatoreId);
    }

    public void Elimina(Guid? operatoreId = null)
        => Raise(new ProdottoFakeEliminato(Id));
}

internal record ProdottoFakeCreato(Guid ProdottoId, string Nome) : DomainEvent;
internal record ProdottoFakeEliminato(Guid ProdottoId)           : DomainEvent;

internal sealed class PrezzoFake : ValueObject
{
    public decimal Importo { get; }
    public string  Valuta  { get; }

    public PrezzoFake(decimal importo, string valuta)
    {
        Guard.AgainstNegative(importo);
        Importo = importo;
        Valuta  = valuta;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Importo;
        yield return Valuta;
    }
}

// ── Test Entity ───────────────────────────────────────────────────────────────

public sealed class EntityTests
{
    [Fact]
    public void Nuova_entita_ha_Id_non_vuoto()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);

        p.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Due_entita_create_separatamente_hanno_Id_diversi()
    {
        var p1 = ProdottoFake.Crea("Tachipirina", 10);
        var p2 = ProdottoFake.Crea("Aspirina", 5);

        p1.Id.Should().NotBe(p2.Id);
    }

    [Fact]
    public void Due_riferimenti_allo_stesso_oggetto_sono_uguali()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);

        (p == p).Should().BeTrue();
        p.Equals(p).Should().BeTrue();
    }

    [Fact]
    public void Due_entita_con_stesso_Id_sono_uguali()
    {
        // Questo scenario avviene quando EF Core carica due istanze dello stesso record.
        var p1 = ProdottoFake.Crea("Tachipirina", 10);
        var p2 = ProdottoFake.Crea("Altro nome", 99); // stesso tipo, Id diverso

        p1.Equals(p2).Should().BeFalse();
    }

    [Fact]
    public void CreatedAt_viene_impostato_alla_creazione()
    {
        var prima  = DateTime.UtcNow.AddSeconds(-1);
        var p      = ProdottoFake.Crea("Tachipirina", 10);
        var dopo   = DateTime.UtcNow.AddSeconds(1);

        p.CreatedAt.Should().BeAfter(prima).And.BeBefore(dopo);
    }

    [Fact]
    public void UpdatedAt_e_null_alla_creazione()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);

        p.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdatedAt_viene_impostato_dopo_una_modifica()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);
        var operatoreId = Guid.NewGuid();

        p.CambiaQuantita(20, operatoreId);

        p.UpdatedAt.Should().NotBeNull();
        p.UpdatedBy.Should().Be(operatoreId);
    }
}

// ── Test AggregateRoot ────────────────────────────────────────────────────────

public sealed class AggregateRootTests
{
    [Fact]
    public void Raise_accoda_lEvento_di_dominio()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);

        p.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProdottoFakeCreato>();
    }

    [Fact]
    public void Piu_operazioni_accumulano_piu_eventi()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);
        p.Elimina();

        p.DomainEvents.Should().HaveCount(2);
    }

    [Fact]
    public void ClearDomainEvents_svuota_la_coda()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);
        p.ClearDomainEvents();

        p.DomainEvents.Should().BeEmpty();
        p.HasDomainEvents.Should().BeFalse();
    }

    [Fact]
    public void DomainEvent_ha_OccurredAt_e_EventId_valorizzati()
    {
        var p = ProdottoFake.Crea("Tachipirina", 10);
        var ev = p.DomainEvents.First();

        ev.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        ev.EventId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Due_eventi_dello_stesso_tipo_hanno_EventId_diversi()
    {
        var p1 = ProdottoFake.Crea("Tachipirina", 10);
        var p2 = ProdottoFake.Crea("Aspirina", 5);

        var id1 = p1.DomainEvents.First().EventId;
        var id2 = p2.DomainEvents.First().EventId;

        id1.Should().NotBe(id2);
    }
}

// ── Test ValueObject ──────────────────────────────────────────────────────────

public sealed class ValueObjectTests
{
    [Fact]
    public void Stessi_valori_producono_oggetti_uguali()
    {
        var p1 = new PrezzoFake(9.90m, "EUR");
        var p2 = new PrezzoFake(9.90m, "EUR");

        p1.Equals(p2).Should().BeTrue();
        (p1 == p2).Should().BeTrue();
    }

    [Fact]
    public void Valori_diversi_producono_oggetti_diversi()
    {
        var p1 = new PrezzoFake(9.90m, "EUR");
        var p2 = new PrezzoFake(9.90m, "USD");

        p1.Equals(p2).Should().BeFalse();
        (p1 != p2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_uguale_per_oggetti_uguali()
    {
        var p1 = new PrezzoFake(5.00m, "EUR");
        var p2 = new PrezzoFake(5.00m, "EUR");

        p1.GetHashCode().Should().Be(p2.GetHashCode());
    }

    [Fact]
    public void ValueObject_non_e_uguale_a_null()
    {
        var p = new PrezzoFake(5.00m, "EUR");

        p.Equals(null).Should().BeFalse();
        (p == null).Should().BeFalse();
    }
}

// ── Test Guard ────────────────────────────────────────────────────────────────

public sealed class GuardTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AgainstNullOrEmpty_lancia_per_stringa_vuota(string? value)
    {
        var act = () => Guard.AgainstNullOrEmpty(value!);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("EMPTY_VALUE");
    }

    [Fact]
    public void AgainstNullOrEmpty_restituisce_stringa_trimmed()
    {
        var result = Guard.AgainstNullOrEmpty("  Tachipirina  ");

        result.Should().Be("Tachipirina");
    }

    [Fact]
    public void AgainstNonPositive_lancia_per_zero()
    {
        var act = () => Guard.AgainstNonPositive(0);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("NON_POSITIVE");
    }

    [Fact]
    public void AgainstNegative_lancia_per_valore_negativo()
    {
        var act = () => Guard.AgainstNegative(-1m);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("NEGATIVE_VALUE");
    }

    [Fact]
    public void AgainstOutOfRange_lancia_fuori_intervallo()
    {
        var act = () => Guard.AgainstOutOfRange(105, 0, 100);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("OUT_OF_RANGE");
    }

    [Fact]
    public void AgainstEmptyGuid_lancia_per_Guid_Empty()
    {
        var act = () => Guard.AgainstEmptyGuid(Guid.Empty);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("EMPTY_GUID");
    }

    [Fact]
    public void AgainstEmpty_lancia_per_lista_vuota()
    {
        var act = () => Guard.AgainstEmpty(Array.Empty<string>());

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("EMPTY_COLLECTION");
    }
}

// ── Test Result ───────────────────────────────────────────────────────────────

public sealed class ResultTests
{
    [Fact]
    public void Result_Ok_e_successo()
    {
        var r = Result.Ok();

        r.IsSuccess.Should().BeTrue();
        r.IsFailure.Should().BeFalse();
    }

    [Fact]
    public void Result_Fail_e_fallimento_con_messaggio()
    {
        var r = Result.Fail("Prodotto non trovato", "NOT_FOUND");

        r.IsFailure.Should().BeTrue();
        r.ErrorMessage.Should().Be("Prodotto non trovato");
        r.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void ResultT_Ok_espone_il_valore()
    {
        var r = Result<int>.Ok(42);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void ResultT_Fail_lancia_su_accesso_a_Value()
    {
        var r = Result<int>.Fail("Errore");

        var act = () => r.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ResultT_Map_trasforma_il_valore_se_successo()
    {
        var r = Result<int>.Ok(10).Map(x => x * 2);

        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(20);
    }

    [Fact]
    public void ResultT_Map_propaga_il_fallimento()
    {
        var r = Result<int>.Fail("Errore").Map(x => x * 2);

        r.IsFailure.Should().BeTrue();
        r.ErrorMessage.Should().Be("Errore");
    }

    [Fact]
    public void ResultT_Match_esegue_ramo_corretto()
    {
        var ok   = Result<string>.Ok("ciao").Match(v => v.ToUpper(), _ => "ERRORE");
        var fail = Result<string>.Fail("ops").Match(v => v.ToUpper(), e => e);

        ok.Should().Be("CIAO");
        fail.Should().Be("ops");
    }
}

// ── Test Enumeration ──────────────────────────────────────────────────────────

public sealed class EnumerationTests
{
    [Fact]
    public void GetAll_restituisce_tutti_i_valori()
    {
        var tutti = Enumeration.GetAll<ClasseFarmaco>().ToList();

        tutti.Should().HaveCount(6);
    }

    [Fact]
    public void FromId_restituisce_il_valore_corretto()
    {
        var classe = Enumeration.FromId<ClasseFarmaco>(1);

        classe.Should().Be(ClasseFarmaco.A);
        classe!.Nome.Should().Be("A");
        classe.IsMutuabile.Should().BeTrue();
    }

    [Fact]
    public void FromId_restituisce_null_per_Id_inesistente()
    {
        var classe = Enumeration.FromId<ClasseFarmaco>(999);

        classe.Should().BeNull();
    }

    [Fact]
    public void FromIdOrThrow_lancia_per_Id_inesistente()
    {
        var act = () => Enumeration.FromIdOrThrow<ClasseFarmaco>(999);

        act.Should().Throw<DomainException>()
            .Which.ErrorCode.Should().Be("INVALID_ENUM_ID");
    }

    [Fact]
    public void Stesso_Id_produce_oggetti_uguali()
    {
        var a1 = Enumeration.FromId<ClasseFarmaco>(1)!;
        var a2 = ClasseFarmaco.A;

        a1.Equals(a2).Should().BeTrue();
        (a1 == a2).Should().BeTrue();
    }

    [Fact]
    public void CategoriaRicetta_richiedeRicetta_per_valori_maggiori_di_zero()
    {
        CategoriaRicetta.NessunObbligo.RichiedeRicetta.Should().BeFalse();
        CategoriaRicetta.RRipetibile.RichiedeRicetta.Should().BeTrue();
        CategoriaRicetta.Stupefacente.RichiedeRicetta.Should().BeTrue();
    }
}

// ── Test Specification ────────────────────────────────────────────────────────

public sealed class SpecificationTests
{
    private sealed class ProdottoConNome : Specification<ProdottoFake>
    {
        private readonly string _nome;
        public ProdottoConNome(string nome) => _nome = nome;
        public override System.Linq.Expressions.Expression<Func<ProdottoFake, bool>>
            ToExpression() => p => p.Nome == _nome;
    }

    private sealed class ProdottoConQuantitaMinima : Specification<ProdottoFake>
    {
        private readonly int _min;
        public ProdottoConQuantitaMinima(int min) => _min = min;
        public override System.Linq.Expressions.Expression<Func<ProdottoFake, bool>>
            ToExpression() => p => p.Quantita >= _min;
    }

    [Fact]
    public void Specification_soddisfatta_restituisce_true()
    {
        var spec = new ProdottoConNome("Tachipirina");
        var p    = ProdottoFake.Crea("Tachipirina", 10);

        spec.IsSatisfiedBy(p).Should().BeTrue();
    }

    [Fact]
    public void Specification_non_soddisfatta_restituisce_false()
    {
        var spec = new ProdottoConNome("Aspirina");
        var p    = ProdottoFake.Crea("Tachipirina", 10);

        spec.IsSatisfiedBy(p).Should().BeFalse();
    }

    [Fact]
    public void And_richiede_entrambe_le_condizioni()
    {
        var spec = new ProdottoConNome("Tachipirina")
            .And(new ProdottoConQuantitaMinima(5));

        var ok     = ProdottoFake.Crea("Tachipirina", 10);
        var noQty  = ProdottoFake.Crea("Tachipirina", 2);
        var noNome = ProdottoFake.Crea("Aspirina", 10);

        spec.IsSatisfiedBy(ok).Should().BeTrue();
        spec.IsSatisfiedBy(noQty).Should().BeFalse();
        spec.IsSatisfiedBy(noNome).Should().BeFalse();
    }

    [Fact]
    public void Or_richiede_almeno_una_condizione()
    {
        var spec = new ProdottoConNome("Tachipirina")
            .Or(new ProdottoConNome("Aspirina"));

        var tachi   = ProdottoFake.Crea("Tachipirina", 10);
        var aspirina = ProdottoFake.Crea("Aspirina", 5);
        var altro    = ProdottoFake.Crea("Moment", 3);

        spec.IsSatisfiedBy(tachi).Should().BeTrue();
        spec.IsSatisfiedBy(aspirina).Should().BeTrue();
        spec.IsSatisfiedBy(altro).Should().BeFalse();
    }

    [Fact]
    public void Not_inverte_la_condizione()
    {
        var spec = new ProdottoConNome("Tachipirina").Not();

        var tachi  = ProdottoFake.Crea("Tachipirina", 10);
        var altro  = ProdottoFake.Crea("Moment", 3);

        spec.IsSatisfiedBy(tachi).Should().BeFalse();
        spec.IsSatisfiedBy(altro).Should().BeTrue();
    }
}
