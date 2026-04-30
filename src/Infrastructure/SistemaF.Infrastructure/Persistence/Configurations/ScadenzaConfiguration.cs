using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Infrastructure.Persistence.Configurations;

// ── ScadenzaProdotto ──────────────────────────────────────────────────────────

internal sealed class ScadenzaProdottoConfiguration
    : IEntityTypeConfiguration<ScadenzaProdotto>
{
    public void Configure(EntityTypeBuilder<ScadenzaProdotto> b)
    {
        b.ToTable("ScadenzeProdotto");
        b.HasKey(s => s.Id);

        b.Property(s => s.ProdottoId).IsRequired();

        b.OwnsOne(s => s.CodiceLotto, l =>
        {
            l.Property(v => v.Valore)
             .HasColumnName("CodiceLotto")
             .HasMaxLength(30)
             .IsRequired();
        });

        b.Property(s => s.DataScadenza).IsRequired();
        b.Property(s => s.Quantita).IsRequired();
        b.Property(s => s.QuantitaMagazzino).HasDefaultValue(0);
        b.Property(s => s.IsInvendibile).HasDefaultValue(false);
        b.Property(s => s.MotivoInvendibilita).HasMaxLength(200);
        b.Property(s => s.DataCarico).IsRequired();
        b.Property(s => s.CreatedAt).IsRequired();
        b.Property(s => s.UpdatedAt);
        b.Property(s => s.CreatedBy);
        b.Property(s => s.UpdatedBy);

        b.HasIndex(s => s.ProdottoId).HasDatabaseName("IX_Scadenze_Prodotto");
        b.HasIndex(s => s.DataScadenza).HasDatabaseName("IX_Scadenze_Data");
        b.HasIndex(s => new { s.ProdottoId, s.DataScadenza })
         .HasDatabaseName("IX_Scadenze_Prodotto_Data");

        b.Ignore(s => s.IsScaduto);
        b.Ignore(s => s.IsInScadenza);
        b.Ignore(s => s.GiorniAllaScadenza);
        b.Ignore(s => s.DomainEvents);
    }
}

// ── PropostaRiga — configurazione con JSON per gli array F1..F5 ───────────────
// (questa configurazione integra quella già esistente in OrdineConfiguration.cs)

internal sealed class PropostaRigaArrayConfiguration
    : IEntityTypeConfiguration<PropostaRiga>
{
    public void Configure(EntityTypeBuilder<PropostaRiga> b)
    {
        // Gli array _fornitoreAbilitato[5] e _quantitaFornitore[5] non sono
        // mappabili direttamente da EF Core. Li serializziamo come JSON.
        // EF Core 8 supporta natively le colonne JSON su SQL Server.
        b.Property<string>("FornitoriAbilitatiJson")
         .HasColumnName("FornitoriAbilitati")
         .HasColumnType("nvarchar(50)")
         .HasDefaultValue("[false,false,false,false,false]");

        b.Property<string>("QuantitaFornitoriJson")
         .HasColumnName("QuantitaFornitori")
         .HasColumnType("nvarchar(50)")
         .HasDefaultValue("[0,0,0,0,0]");

        b.Property<string>("OmaggioFornitoriJson")
         .HasColumnName("OmaggioFornitori")
         .HasColumnType("nvarchar(50)")
         .HasDefaultValue("[0,0,0,0,0]");
    }
}
