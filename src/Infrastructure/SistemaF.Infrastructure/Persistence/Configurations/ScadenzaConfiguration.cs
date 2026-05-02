using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Infrastructure.Persistence.Configurations;

// ── PropostaRiga — serializzazione array F1..F5 come JSON ─────────────────────

internal sealed class PropostaRigaArrayConfiguration
    : IEntityTypeConfiguration<PropostaRiga>
{
    public void Configure(EntityTypeBuilder<PropostaRiga> b)
    {
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
