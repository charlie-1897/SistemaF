using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SistemaF.Domain.Entities.Ordine;

namespace SistemaF.Infrastructure.Persistence.Configurations;

internal sealed class OrdineConfiguration : IEntityTypeConfiguration<Ordine>
{
    public void Configure(EntityTypeBuilder<Ordine> b)
    {
        b.ToTable("Ordini");
        b.HasKey(o => o.Id);
        b.Property(o => o.RowVersion).IsRowVersion();

        // Numero ordine come VO
        b.OwnsOne(o => o.Numero, n =>
        {
            n.Property(v => v.Valore).HasColumnName("NumeroOrdine").HasMaxLength(9).IsRequired();
            n.HasIndex(v => v.Valore).IsUnique().HasDatabaseName("UX_Ordini_Numero");
        });

        b.Property(o => o.PropostaId).IsRequired();
        b.Property(o => o.FornitoreId).IsRequired();
        b.Property(o => o.CodiceAnabase).IsRequired();
        b.Property(o => o.TipoFornitore).IsRequired();
        b.Property(o => o.RagioneSociale).HasMaxLength(100).IsRequired();
        b.Property(o => o.Stato).IsRequired();
        b.Property(o => o.DataEmissione).IsRequired();
        b.Property(o => o.DataTrasmissione);
        b.Property(o => o.DataRicezione);
        b.Property(o => o.OperatoreId).IsRequired();
        b.Property(o => o.Note).HasMaxLength(500);
        b.Property(o => o.NomeEmissione).HasMaxLength(100);
        b.Property(o => o.IsDeleted).HasDefaultValue(false);
        b.HasQueryFilter(o => !o.IsDeleted);

        // Audit
        b.Property(o => o.CreatedAt).IsRequired();
        b.Property(o => o.UpdatedAt);
        b.Property(o => o.CreatedBy);
        b.Property(o => o.UpdatedBy);
        b.Property(o => o.DeletedAt);
        b.Property(o => o.DeletedBy);

        // Righe
        b.HasMany<RigaOrdine>("_righe")
         .WithOne()
         .HasForeignKey(r => r.OrdineId)
         .OnDelete(DeleteBehavior.Cascade);

        // Indici
        b.HasIndex(o => o.FornitoreId).HasDatabaseName("IX_Ordini_Fornitore");
        b.HasIndex(o => o.Stato).HasDatabaseName("IX_Ordini_Stato");
        b.HasIndex(o => o.DataEmissione).HasDatabaseName("IX_Ordini_DataEmissione");

        b.Ignore(o => o.TotalePezzi);
        b.Ignore(o => o.ImportoTotale);
        b.Ignore(o => o.NumeroRighe);
        b.Ignore(o => o.DomainEvents);
    }
}

internal sealed class RigaOrdineConfiguration : IEntityTypeConfiguration<RigaOrdine>
{
    public void Configure(EntityTypeBuilder<RigaOrdine> b)
    {
        b.ToTable("RigheOrdine");
        b.HasKey(r => r.Id);

        b.OwnsOne(r => r.CodiceFarmaco, c =>
        {
            c.Property(v => v.Valore).HasColumnName("CodiceFarmaco").HasMaxLength(9).IsRequired();
        });

        b.OwnsOne(r => r.Costo, c =>
        {
            c.Property(v => v.Imponibile).HasColumnName("CostoUnitario")
             .HasColumnType("decimal(10,4)").IsRequired();
            c.Property(v => v.Sconto).HasColumnName("Sconto")
             .HasColumnType("decimal(6,4)");
            c.Property(v => v.ExtraSconto).HasColumnName("ExtraSconto")
             .HasColumnType("decimal(6,4)");
            c.Property(v => v.ScontoLordo).HasColumnName("ScontoLordo")
             .HasColumnType("decimal(6,4)");
            c.Property(v => v.Margine).HasColumnName("Margine")
             .HasColumnType("decimal(6,4)");
        });

        b.Property(r => r.OrdineId).IsRequired();
        b.Property(r => r.ProdottoId).IsRequired();
        b.Property(r => r.Descrizione).HasMaxLength(200);
        b.Property(r => r.Quantita).IsRequired();
        b.Property(r => r.QuantitaOmaggio).HasDefaultValue(0);
        b.Property(r => r.PrezzoListino).HasColumnType("decimal(10,2)");
        b.Property(r => r.AliquotaIVA);
        b.Property(r => r.QuantitaMancante).HasDefaultValue(0);
        b.Property(r => r.QuantitaNecessita).HasDefaultValue(0);
        b.Property(r => r.QuantitaPrenotata).HasDefaultValue(0);
        b.Property(r => r.QuantitaSospesa).HasDefaultValue(0);
        b.Property(r => r.QuantitaArchivio).HasDefaultValue(0);
        b.Property(r => r.QuantitaArrivata).HasDefaultValue(0);
        b.Property(r => r.QuantitaAssicurata).HasDefaultValue(0);
        b.Property(r => r.CodiceMancante).HasMaxLength(30);

        b.HasIndex(r => r.OrdineId).HasDatabaseName("IX_RigheOrdine_Ordine");
        b.HasIndex(r => r.ProdottoId).HasDatabaseName("IX_RigheOrdine_Prodotto");

        b.Ignore(r => r.CostoTotale);
        b.Ignore(r => r.DomainEvents);
    }
}

internal sealed class PropostaOrdineConfiguration : IEntityTypeConfiguration<PropostaOrdine>
{
    public void Configure(EntityTypeBuilder<PropostaOrdine> b)
    {
        b.ToTable("ProposteOrdine");
        b.HasKey(p => p.Id);
        b.Property(p => p.RowVersion).IsRowVersion();

        b.Property(p => p.OperatoreId).IsRequired();
        b.Property(p => p.ConfigurazioneId).IsRequired();
        b.Property(p => p.NomeEmissione).HasMaxLength(100);
        b.Property(p => p.Stato).IsRequired();
        b.Property(p => p.DataCreazione).IsRequired();
        b.Property(p => p.Interrotta).HasDefaultValue(false);

        b.Property(p => p.IsEmissioneDaArchivio).HasDefaultValue(false);
        b.Property(p => p.IsEmissioneNecessita).HasDefaultValue(false);
        b.Property(p => p.IsEmissionePrenotati).HasDefaultValue(false);
        b.Property(p => p.IsEmissioneSospesi).HasDefaultValue(false);
        b.Property(p => p.IsOrdineLiberoDitta).HasDefaultValue(false);
        b.Property(p => p.IsRicalcolaNecessita).HasDefaultValue(false);
        b.Property(p => p.IsMagazzinoPresente).HasDefaultValue(false);
        b.Property(p => p.IndiceMagazzino).HasDefaultValue(0);

        // ParametriIndiceDiVendita come VO owned
        b.OwnsOne(p => p.IndiceDiVendita, o =>
        {
            o.Property(v => v.Tipo).HasColumnName("IndiceVenditaTipo");
            o.Property(v => v.GiorniCopertura).HasColumnName("IndiceVenditaGiorni").HasDefaultValue(0);
            o.Property(v => v.ValoreMinimo).HasColumnName("IndiceVenditaMin").HasColumnType("decimal(10,4)");
            o.Property(v => v.ValoreMassimo).HasColumnName("IndiceVenditaMax").HasColumnType("decimal(10,4)");
            o.Property(v => v.IsInclude).HasColumnName("IndiceVenditaInclude").HasDefaultValue(true);
            o.Property(v => v.IsSottraiGiacenze).HasColumnName("IndiceVenditaSottraiGiacenze");
            o.Property(v => v.IsRicalcolaNecessita).HasColumnName("IndiceVenditaRicalcola");
        });

        // ParametriRipristinoScorta come VO owned
        b.OwnsOne(p => p.RipristinoScorta, o =>
        {
            o.Property(v => v.Tipo).HasColumnName("RipristinoScortaTipo");
            o.Property(v => v.IsConsideraProdottiEntroLeScorte).HasColumnName("RipristinoConsideraEntroScorte");
            o.Property(v => v.IsConsideraScortaDiSicurezza).HasColumnName("RipristinoConsideraScortaSicurezza");
        });

        // RiepilogoElaborazione come VO owned
        b.OwnsOne(p => p.Riepilogo, o =>
        {
            o.Property(v => v.NomeEmissione).HasColumnName("RiepilogoNome").HasMaxLength(100);
            o.Property(v => v.DataOraInizio).HasColumnName("RiepilogoInizio");
            o.Property(v => v.DataOraFine).HasColumnName("RiepilogoFine");
            o.Property(v => v.NumeroProdottiGlobali).HasColumnName("RiepilogoProdottiGlobali");
            o.Property(v => v.NumeroProdottiEsaminati).HasColumnName("RiepilogoProdottiEsaminati");
            o.Property(v => v.NumeroProdottiInOrdine).HasColumnName("RiepilogoProdottiInOrdine");
        });

        // Righe della proposta (PropostaRiga)
        b.HasMany<PropostaRiga>("_righe")
         .WithOne()
         .HasForeignKey(r => r.PropostaId)
         .OnDelete(DeleteBehavior.Cascade);

        // Fornitori come JSON column (non hanno bisogno di tabella separata)
        b.HasIndex(p => p.OperatoreId).HasDatabaseName("IX_Proposte_Operatore");
        b.HasIndex(p => p.Stato).HasDatabaseName("IX_Proposte_Stato");

        b.Ignore(p => p.Fornitori);   // list serializzata separatamente
        b.Ignore(p => p.NumeroProdottiInProposta);
        b.Ignore(p => p.NumeroProdottiConFornitore);
        b.Ignore(p => p.RigheDaEmettere);
        b.Ignore(p => p.DomainEvents);
    }
}

internal sealed class PropostaRigaConfiguration : IEntityTypeConfiguration<PropostaRiga>
{
    public void Configure(EntityTypeBuilder<PropostaRiga> b)
    {
        b.ToTable("ProposteRighe");
        b.HasKey(r => r.Id);

        b.OwnsOne(r => r.CodiceFarmaco, c =>
        {
            c.Property(v => v.Valore).HasColumnName("CodiceFarmaco").HasMaxLength(9).IsRequired();
        });

        b.Property(r => r.PropostaId).IsRequired();
        b.Property(r => r.ProdottoId).IsRequired();
        b.Property(r => r.Descrizione).HasMaxLength(200);

        // Quantità per fonte
        b.Property(r => r.QuantitaMancante).HasDefaultValue(0);
        b.Property(r => r.QuantitaNecessita).HasDefaultValue(0);
        b.Property(r => r.QuantitaPrenotata).HasDefaultValue(0);
        b.Property(r => r.QuantitaSospesa).HasDefaultValue(0);
        b.Property(r => r.QuantitaArchivio).HasDefaultValue(0);

        // Prezzi
        b.Property(r => r.PrezzoListino).HasColumnType("decimal(10,2)");
        b.Property(r => r.PrezzoVendita).HasColumnType("decimal(10,2)");
        b.Property(r => r.PrezzoFarmacia).HasColumnType("decimal(10,2)");
        b.Property(r => r.PrezzoRiferimento).HasColumnType("decimal(10,2)");
        b.Property(r => r.AliquotaIVA).HasDefaultValue(10);
        b.Property(r => r.Classe).HasMaxLength(1);

        // Giacenze
        b.Property(r => r.GiacenzaEsposizione).HasDefaultValue(0);
        b.Property(r => r.ScortaMinimaEsposizione).HasDefaultValue(0);
        b.Property(r => r.ScortaMassimaEsposizione).HasDefaultValue(0);
        b.Property(r => r.GiacenzaMagazzino).HasDefaultValue(0);
        b.Property(r => r.ScortaMinimaMagazzino).HasDefaultValue(0);
        b.Property(r => r.ScortaMassimaMagazzino).HasDefaultValue(0);

        // Flag classificazione
        b.Property(r => r.IsCongelato).HasDefaultValue(false);
        b.Property(r => r.IsVeterinario).HasDefaultValue(false);
        b.Property(r => r.IsStupefacente).HasDefaultValue(false);
        b.Property(r => r.IsSegnalato).HasDefaultValue(false);
        b.Property(r => r.IsTrattatoDitta).HasDefaultValue(false);
        b.Property(r => r.IsSistemaAutomatico).HasDefaultValue(false);
        b.Property(r => r.IsOtcSop).HasDefaultValue(false);
        b.Property(r => r.IsGenerico).HasDefaultValue(false);
        b.Property(r => r.IsInOrdine).HasDefaultValue(false);
        b.Property(r => r.IsPreferenziale).HasDefaultValue(false);
        b.Property(r => r.DaOrdinare).HasDefaultValue(true);
        b.Property(r => r.DaEliminare).HasDefaultValue(false);
        b.Property(r => r.SettoreInventario).HasMaxLength(5);
        b.Property(r => r.Priorita);

        // Indici di vendita
        b.Property(r => r.IndiceVenditaTendenziale).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(r => r.IndiceVenditaMensile).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(r => r.IndiceVenditaAnnuale).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(r => r.IndiceVenditaPeriodo).HasColumnType("decimal(10,4)").HasDefaultValue(0m);
        b.Property(r => r.IndiceVenditaMediaAritmetica).HasColumnType("decimal(10,4)").HasDefaultValue(0m);

        b.Property(r => r.FornitorePreferenzialeId);
        b.Property(r => r.CodiceFornitorePreferenziale).HasDefaultValue(0L);

        b.HasIndex(r => r.PropostaId).HasDatabaseName("IX_ProposteRighe_Proposta");
        b.HasIndex(r => r.ProdottoId).HasDatabaseName("IX_ProposteRighe_Prodotto");

        // Le colonne F1..F5 e QuantitaFornitore1..5 sono array non mappabili
        // direttamente da EF Core: vengono serializzati come colonne separate.
        // Una migration script le crea come INT/BIT columns.
        b.Ignore(r => r.QuantitaTotale);
        b.Ignore(r => r.TotaleQuantita);
        b.Ignore(r => r.QuantitaSottoOrdinata);
        b.Ignore(r => r.GiacenzaTotale);
        b.Ignore(r => r.HaFornitoriAbilitati);
        b.Ignore(r => r.NumeroFornitoriConQuantita);
        b.Ignore(r => r.DomainEvents);
    }
}
