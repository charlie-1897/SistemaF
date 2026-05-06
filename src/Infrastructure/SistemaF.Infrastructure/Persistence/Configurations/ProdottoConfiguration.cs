using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configurazione EF Core per il Prodotto aggregate.
/// Mappa ogni proprietà C# alla colonna SQL equivalente
/// al vecchio schema Access (tabelle Prodotti, Esposizione, AltreGiacenze, ScadenzeProdotti).
/// </summary>
internal sealed class ProdottoConfiguration : IEntityTypeConfiguration<Prodotto>
{
    public void Configure(EntityTypeBuilder<Prodotto> b)
    {
        b.ToTable("Prodotti");
        b.HasKey(p => p.Id);

        // Concorrenza ottimistica (RowVersion).
        // Previene la perdita di dati in ambienti multi-postazione.
        b.Property(p => p.RowVersion).IsRowVersion();

        // ── Identificatori ─────────────────────────────────────────────────

        b.OwnsOne(p => p.CodiceFarmaco, o =>
        {
            o.Property(v => v.Valore)
             .HasColumnName("CodiceFarmaco").HasMaxLength(9).IsRequired();
            o.HasIndex(v => v.Valore).IsUnique().HasDatabaseName("UX_Prodotti_CodiceFarmaco");
        });

        b.OwnsOne(p => p.CodiceEAN, o =>
        {
            o.Property(v => v.Valore)
             .HasColumnName("CodiceEAN").HasMaxLength(13);
            o.HasIndex(v => v.Valore).IsUnique().HasDatabaseName("UX_Prodotti_CodiceEAN");
        });

        b.OwnsOne(p => p.CodiceATC, o =>
        {
            o.Property(v => v.Valore).HasColumnName("CodiceATC").HasMaxLength(7);
        });

        b.Property(p => p.CodiceDitta).HasMaxLength(20);
        b.Property(p => p.Targatura).HasMaxLength(20);

        // ── Descrizione ────────────────────────────────────────────────────

        b.Property(p => p.Descrizione).HasMaxLength(200).IsRequired();
        b.Property(p => p.FormaBiotica).HasMaxLength(100);
        b.Property(p => p.Sostanza).HasMaxLength(150);
        b.Property(p => p.Patologia).HasMaxLength(100);
        b.Property(p => p.Gruppo).HasMaxLength(80);
        b.Property(p => p.LineaDitta).HasMaxLength(80);
        b.Property(p => p.Nomenclatore).HasMaxLength(20);

        // ── Classificazione ────────────────────────────────────────────────

        // ClasseFarmaco come Enumeration: persiste l'Id intero.
        b.Property(p => p.Classe)
         .HasColumnName("ClasseId")
         .HasConversion(c => c.Id, id => Enumeration.FromIdOrThrow<ClasseFarmaco>(id));

        b.Property(p => p.CategoriaRicetta)
         .HasColumnName("CategoriaRicettaId")
         .HasConversion(c => c.Id, id => Enumeration.FromIdOrThrow<CategoriaRicetta>(id));

        b.Property(p => p.IsStupefacente);
        b.Property(p => p.IsVeterinario);
        b.Property(p => p.IsCongelato);
        b.Property(p => p.IsTrattato);
        b.Property(p => p.IsIntegrativo);
        b.Property(p => p.IsPluriPrescrizione);

        // ── Prezzi ─────────────────────────────────────────────────────────

        b.OwnsOne(p => p.PrezzoVendita, o =>
        {
            o.Property(v => v.Importo).HasColumnName("PrezzoVendita")
             .HasColumnType("decimal(10,2)").IsRequired();
            o.Property(v => v.AliquotaIVA).HasColumnName("AliquotaIVAVendita").IsRequired();
        });

        b.OwnsOne(p => p.PrezzoListino, o =>
        {
            o.Property(v => v.Importo).HasColumnName("PrezzoListino")
             .HasColumnType("decimal(10,2)");
            o.Property(v => v.AliquotaIVA).HasColumnName("AliquotaIVAListino");
        });

        b.OwnsOne(p => p.PrezzoRiferimentoSSN, o =>
        {
            o.Property(v => v.Importo).HasColumnName("PrezzoRiferimentoSSN")
             .HasColumnType("decimal(10,2)");
            o.Property(v => v.AliquotaIVA).HasColumnName("AliquotaIVASSN");
        });

        b.OwnsOne(p => p.PrezzoAcquisto, o =>
        {
            o.Property(v => v.Importo).HasColumnName("PrezzoAcquisto")
             .HasColumnType("decimal(10,2)");
            o.Property(v => v.AliquotaIVA).HasColumnName("AliquotaIVAAcquisto");
        });

        // ── Giacenze Esposizione ──────────────────────────────────────────

        b.OwnsOne(p => p.GiacenzaEsposizione, o =>
        {
            o.Property(v => v.Giacenza)
             .HasColumnName("GiacenzaEsposizione").HasDefaultValue(0);
            o.Property(v => v.ScortaMinima)
             .HasColumnName("ScortaMinimaEsposizione").HasDefaultValue(0);
            o.Property(v => v.ScortaMassima)
             .HasColumnName("ScortaMassimaEsposizione").HasDefaultValue(0);
        });

        // ── Giacenze Magazzino retro ──────────────────────────────────────

        b.OwnsOne(p => p.GiacenzaMagazzino, o =>
        {
            o.Property(v => v.Giacenza)
             .HasColumnName("GiacenzaMagazzino").HasDefaultValue(0);
            o.Property(v => v.ScortaMinima)
             .HasColumnName("ScortaMinimaMagazzino").HasDefaultValue(0);
            o.Property(v => v.ScortaMassima)
             .HasColumnName("ScortaMassimaMagazzino").HasDefaultValue(0);
        });

        b.Property(p => p.IsGestioneScorteAutomatica).HasDefaultValue(false);

        // ── Invendibili ────────────────────────────────────────────────────

        b.Property(p => p.IsInvendibile).HasDefaultValue(false);
        b.Property(p => p.GiacenzaInvendibile).HasDefaultValue(0);
        b.Property(p => p.IsSegnalato).HasDefaultValue(false);

        // ── Soft delete ────────────────────────────────────────────────────

        b.Property(p => p.IsDeleted).HasDefaultValue(false);
        b.Property(p => p.DeletedAt);
        b.Property(p => p.DeletedBy);

        // Global query filter: esclude automaticamente i prodotti cancellati.
        b.HasQueryFilter(p => !p.IsDeleted);

        // ── Audit ──────────────────────────────────────────────────────────

        b.Property(p => p.IsAttivo).HasDefaultValue(true);
        b.Property(p => p.DataAggiornamento);
        b.Property(p => p.CreatedAt).IsRequired();
        b.Property(p => p.UpdatedAt);
        b.Property(p => p.CreatedBy);
        b.Property(p => p.UpdatedBy);

        // ── Relazione con ScadenzeProdotti ────────────────────────────────

        b.HasMany<ScadenzaProdotto>("_scadenze")
         .WithOne()
         .HasForeignKey(s => s.ProdottoId)
         .OnDelete(DeleteBehavior.Cascade);

        // ── Ignora domain events (non persistiti) ──────────────────────────

        b.Ignore(p => p.GiacenzaTotale);
        b.Ignore(p => p.IsSottoscorta);
        b.Ignore(p => p.IsMutuabile);

        // ── Indici ─────────────────────────────────────────────────────────

        b.HasIndex(p => p.IsAttivo).HasDatabaseName("IX_Prodotti_IsAttivo");
        b.HasIndex(p => p.IsSottoscorta).HasDatabaseName("IX_Prodotti_Sottoscorta");
    }
}

internal sealed class ScadenzaProdottoConfiguration : IEntityTypeConfiguration<ScadenzaProdotto>
{
    public void Configure(EntityTypeBuilder<ScadenzaProdotto> b)
    {
        b.ToTable("ScadenzeProdotti");
        b.HasKey(s => s.Id);

        b.OwnsOne(s => s.Lotto, o =>
        {
            o.Property(v => v.Valore).HasColumnName("Lotto").HasMaxLength(30).IsRequired();
        });

        b.Property(s => s.DataScadenza).IsRequired();
        b.Property(s => s.Quantita).IsRequired();
        b.Property(s => s.ProdottoId).IsRequired();

        b.HasIndex(s => new { s.ProdottoId, s.DataScadenza })
         .HasDatabaseName("IX_Scadenze_ProdottoData");

        b.Ignore(s => s.IsScaduto);
        b.Ignore(s => s.IsInScadenza);
        b.Ignore(s => s.GiorniAllaScadenza);
    }
}
