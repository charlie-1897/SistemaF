using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SistemaF.Infrastructure.Persistence;

// ─────────────────────────────────────────────────────────────────────────────
//  Entità EF Core per le tabelle di supporto ai servizi ordine
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Offerta promozionale di un fornitore per un prodotto.
/// Corrisponde alla tabella CSFOrdCommon.OfferteFornitore (legacy VB6).
/// </summary>
public sealed class OffertaFornitoreEntity
{
    public Guid     Id               { get; private set; } = Guid.NewGuid();
    public Guid     ProdottoId       { get; set; }
    public Guid     FornitoreId      { get; set; }
    public decimal  CostoOfferta     { get; set; }
    public decimal  ScontoCalcolato  { get; set; }
    public int      QuantitaMinima   { get; set; }
    public int      QuantitaOmaggio  { get; set; }
    public DateOnly DataInizio       { get; set; }
    public DateOnly DataFine         { get; set; }
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
}

public sealed class OffertaFornitoreEntityConfiguration
    : IEntityTypeConfiguration<OffertaFornitoreEntity>
{
    public void Configure(EntityTypeBuilder<OffertaFornitoreEntity> b)
    {
        b.ToTable("OfferteFornitore");
        b.HasKey(x => x.Id);
        b.Property(x => x.CostoOfferta).HasColumnType("decimal(10,4)");
        b.Property(x => x.ScontoCalcolato).HasColumnType("decimal(6,4)");
        b.HasIndex(x => new { x.ProdottoId, x.FornitoreId, x.DataInizio, x.DataFine });
    }
}

/// <summary>
/// Movimento di vendita giornaliero per prodotto.
/// Corrisponde alla tabella CSFOrdCommon.MovimentiVendita (legacy VB6).
/// </summary>
public sealed class MovimentoVenditaEntity
{
    public Guid     Id          { get; private set; } = Guid.NewGuid();
    public Guid     ProdottoId  { get; set; }
    public DateOnly Data        { get; set; }
    public int      Quantita    { get; set; }
    public decimal  Importo     { get; set; }
    public string   Canale      { get; set; } = "Banco"; // Banco | Ricetta | Internet
}

public sealed class MovimentoVenditaEntityConfiguration
    : IEntityTypeConfiguration<MovimentoVenditaEntity>
{
    public void Configure(EntityTypeBuilder<MovimentoVenditaEntity> b)
    {
        b.ToTable("MovimentiVendita");
        b.HasKey(x => x.Id);
        b.Property(x => x.Importo).HasColumnType("decimal(10,2)");
        b.Property(x => x.Canale).HasMaxLength(20);
        b.HasIndex(x => new { x.ProdottoId, x.Data });
    }
}
