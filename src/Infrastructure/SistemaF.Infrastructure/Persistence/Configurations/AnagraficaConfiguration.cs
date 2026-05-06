using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaF.Domain.Entities.Anagrafica;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Infrastructure.Persistence.Configurations;

// ── Fornitore ──────────────────────────────────────────────────────────────────

internal sealed class FornitoreConfiguration : IEntityTypeConfiguration<Fornitore>
{
    public void Configure(EntityTypeBuilder<Fornitore> b)
    {
        b.ToTable("Fornitori");
        b.HasKey(f => f.Id);
        b.Property(f => f.RowVersion).IsRowVersion();

        b.Property(f => f.CodiceAnabase).HasDefaultValue(0L);
        b.Property(f => f.RagioneSociale).HasMaxLength(150).IsRequired();
        b.Property(f => f.Tipo).IsRequired();
        b.Property(f => f.PartitaIVA).HasMaxLength(16);
        b.Property(f => f.CodiceFiscale).HasMaxLength(16);
        b.Property(f => f.Annotazione).HasMaxLength(500);
        b.Property(f => f.NomeDeposito).HasMaxLength(100);
        b.Property(f => f.NomeContatto).HasMaxLength(100);
        b.Property(f => f.EmailContatto).HasMaxLength(100);
        b.Property(f => f.BudgetStimato).HasColumnType("decimal(12,2)").HasDefaultValue(0m);
        b.Property(f => f.IsMagazzino).HasDefaultValue(false);
        b.Property(f => f.IsPreferenzialeDefault).HasDefaultValue(false);
        b.Property(f => f.IsFornitoreGruppo).HasDefaultValue(false);
        b.Property(f => f.PercentualeRipartizione).HasDefaultValue(100);
        b.Property(f => f.IsAttivo).HasDefaultValue(true);

        // Sede — Owned Value Object
        b.OwnsOne(f => f.Sede, o =>
        {
            o.Property(v => v.Indirizzo).HasColumnName("Indirizzo").HasMaxLength(150);
            o.Property(v => v.Cap).HasColumnName("CAP").HasMaxLength(10);
            o.Property(v => v.Localita).HasColumnName("Localita").HasMaxLength(80);
            o.Property(v => v.Provincia).HasColumnName("Provincia").HasMaxLength(2);
        });

        // Contatti — Owned Value Object
        b.OwnsOne(f => f.Contatti, o =>
        {
            o.Property(v => v.Telefono).HasColumnName("Telefono").HasMaxLength(30);
            o.Property(v => v.Fax).HasColumnName("Fax").HasMaxLength(30);
            o.Property(v => v.Email).HasColumnName("Email").HasMaxLength(100);
            o.Property(v => v.Cellulare).HasColumnName("Cellulare").HasMaxLength(30);
            o.Property(v => v.SitoWeb).HasColumnName("SitoWeb").HasMaxLength(200);
        });

        // IndirizzoDeposito — Owned nullable
        b.OwnsOne(f => f.IndirizzoDeposito, o =>
        {
            o.Property(v => v.Indirizzo).HasColumnName("DepositoIndirizzo").HasMaxLength(150);
            o.Property(v => v.Cap).HasColumnName("DepositoCAP").HasMaxLength(10);
            o.Property(v => v.Localita).HasColumnName("DepositoLocalita").HasMaxLength(80);
            o.Property(v => v.Provincia).HasColumnName("DepositoProvincia").HasMaxLength(2);
        });

        // Audit
        b.Property(f => f.CreatedAt).IsRequired();
        b.Property(f => f.UpdatedAt);
        b.Property(f => f.CreatedBy);
        b.Property(f => f.UpdatedBy);

        // Indici
        b.HasIndex(f => f.CodiceAnabase).IsUnique()
         .HasFilter("[CodiceAnabase] > 0")
         .HasDatabaseName("UX_Fornitori_CodiceAnabase");
        b.HasIndex(f => f.Tipo).HasDatabaseName("IX_Fornitori_Tipo");
        b.HasIndex(f => f.IsAttivo).HasDatabaseName("IX_Fornitori_Attivi");

        b.Ignore(f => f.DomainEvents);
    }
}

// ── Operatore ─────────────────────────────────────────────────────────────────

internal sealed class OperatoreConfiguration : IEntityTypeConfiguration<Operatore>
{
    public void Configure(EntityTypeBuilder<Operatore> b)
    {
        b.ToTable("Operatori");
        b.HasKey(o => o.Id);

        b.Property(o => o.Login).HasMaxLength(50).IsRequired();
        b.Property(o => o.NomeCognome).HasMaxLength(100).IsRequired();
        b.Property(o => o.PasswordHash).HasMaxLength(256).IsRequired();
        b.Property(o => o.Badge).HasMaxLength(50);
        b.Property(o => o.AutorizzazioniLegacy).HasMaxLength(50)
         .HasDefaultValue(new string('0', 50));
        b.Property(o => o.IsAttivo).HasDefaultValue(true);
        b.Property(o => o.IsAmministratore).HasDefaultValue(false);

        b.Property(o => o.CreatedAt).IsRequired();
        b.Property(o => o.UpdatedAt);
        b.Property(o => o.CreatedBy);
        b.Property(o => o.UpdatedBy);

        b.HasIndex(o => o.Login).IsUnique()
         .HasDatabaseName("UX_Operatori_Login");
        b.HasIndex(o => o.Badge).IsUnique()
         .HasFilter("[Badge] IS NOT NULL")
         .HasDatabaseName("UX_Operatori_Badge");

        b.Ignore(o => o.DomainEvents);
    }
}

// ── Farmacia ──────────────────────────────────────────────────────────────────

internal sealed class FarmaciaConfiguration : IEntityTypeConfiguration<Farmacia>
{
    public void Configure(EntityTypeBuilder<Farmacia> b)
    {
        b.ToTable("Farmacia");
        b.HasKey(f => f.Id);

        b.Property(f => f.Nome).HasMaxLength(150).IsRequired();
        b.Property(f => f.RagioneSociale).HasMaxLength(150);
        b.Property(f => f.PartitaIVA).HasMaxLength(16);
        b.Property(f => f.CodiceFiscale).HasMaxLength(16);
        b.Property(f => f.CodiceFarmaciaAsl).HasMaxLength(20);
        b.Property(f => f.CodiceAsl).HasMaxLength(20);
        b.Property(f => f.NomeAsl).HasMaxLength(80);
        b.Property(f => f.RegioneFarmacia).HasMaxLength(50);
        b.Property(f => f.TitolareFarmacia).HasMaxLength(100);
        b.Property(f => f.HaMagazzino).HasDefaultValue(false);

        b.OwnsOne(f => f.Sede, o =>
        {
            o.Property(v => v.Indirizzo).HasColumnName("Indirizzo").HasMaxLength(150);
            o.Property(v => v.Cap).HasColumnName("CAP").HasMaxLength(10);
            o.Property(v => v.Localita).HasColumnName("Localita").HasMaxLength(80);
            o.Property(v => v.Provincia).HasColumnName("Provincia").HasMaxLength(2);
        });

        b.OwnsOne(f => f.Contatti, o =>
        {
            o.Property(v => v.Telefono).HasColumnName("Telefono").HasMaxLength(30);
            o.Property(v => v.Fax).HasColumnName("Fax").HasMaxLength(30);
            o.Property(v => v.Email).HasColumnName("Email").HasMaxLength(100);
            o.Property(v => v.SitoWeb).HasColumnName("SitoWeb").HasMaxLength(200);
        });

        b.Property(f => f.CreatedAt).IsRequired();
        b.Property(f => f.UpdatedAt);

        b.Ignore(f => f.DomainEvents);
    }
}

// ── ConfigurazioneEmissione ───────────────────────────────────────────────────

internal sealed class ConfigurazioneEmissioneConfiguration
    : IEntityTypeConfiguration<ConfigurazioneEmissione>
{
    public void Configure(EntityTypeBuilder<ConfigurazioneEmissione> b)
    {
        b.ToTable("ConfigurazioniEmissione");
        b.HasKey(c => c.Id);

        b.Property(c => c.Nome).HasMaxLength(100).IsRequired();
        b.Property(c => c.Descrizione).HasMaxLength(300);
        b.Property(c => c.IsAttiva).HasDefaultValue(true);

        // Fonti
        b.Property(c => c.DaArchivio).HasDefaultValue(true);
        b.Property(c => c.DaNecessita).HasDefaultValue(true);
        b.Property(c => c.DaPrenotati).HasDefaultValue(false);
        b.Property(c => c.DaSospesi).HasDefaultValue(false);

        // Flags
        b.Property(c => c.OrdineLiberoDitta).HasDefaultValue(false);
        b.Property(c => c.RicalcolaNecessita).HasDefaultValue(false);

        // Indice di vendita
        b.Property(c => c.TipoIndiceVendita).HasDefaultValue(TipoIndiceVendita.Tendenziale);
        b.Property(c => c.GiorniCopertura).HasDefaultValue(7);
        b.Property(c => c.IndiceVenditaMin).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(c => c.IndiceVenditaMax).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(c => c.SottraiGiacenze).HasDefaultValue(false);

        // Ripristino scorte
        b.Property(c => c.TipoRipristinoScorta)
         .HasDefaultValue(TipoRipristinoScorta.ScortaMinimaEsposizione);
        b.Property(c => c.ConsideraEntroLeScorte).HasDefaultValue(false);
        b.Property(c => c.ConsideraScortaDiSicurezza).HasDefaultValue(false);

        b.Property(c => c.TipoOrdinamento).HasDefaultValue(0);
        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.UpdatedAt);
        b.Property(c => c.CreatedBy);
        b.Property(c => c.UpdatedBy);

        b.HasIndex(c => c.Nome).HasDatabaseName("IX_Configurazioni_Nome");
        b.HasIndex(c => c.IsAttiva).HasDatabaseName("IX_Configurazioni_Attive");

    }
}

// ── ConfigurazioneEmissioneFornitore ─────────────────────────────────────────

internal sealed class ConfigurazioneEmissioneFornitoreConfiguration
    : IEntityTypeConfiguration<ConfigurazioneEmissioneFornitore>
{
    public void Configure(EntityTypeBuilder<ConfigurazioneEmissioneFornitore> b)
    {
        b.ToTable("ConfigurazioniEmissioneFornitori");
        b.HasKey(c => c.Id);

        b.Property(c => c.ConfigurazioneId).IsRequired();
        b.Property(c => c.FornitoreId).IsRequired();
        b.Property(c => c.OrdineIndice).HasDefaultValue(1);
        b.Property(c => c.PercentualeRipartizione).HasDefaultValue(100);
        b.Property(c => c.IsAbilitato).HasDefaultValue(true);
        b.Property(c => c.CreatedAt).IsRequired();
        b.Property(c => c.UpdatedAt);

        b.HasIndex(c => c.ConfigurazioneId)
         .HasDatabaseName("IX_ConfigurazioniFornitori_Config");
        b.HasIndex(c => new { c.ConfigurazioneId, c.FornitoreId }).IsUnique()
         .HasDatabaseName("UX_ConfigurazioniFornitori_Pair");

    }
}
