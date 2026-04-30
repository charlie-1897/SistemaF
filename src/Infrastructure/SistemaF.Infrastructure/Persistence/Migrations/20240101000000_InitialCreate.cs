using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaF.Infrastructure.Persistence.Migrations;

// ═══════════════════════════════════════════════════════════════════════════════
//  MIGRATION: InitialCreate
//
//  Crea lo schema completo per i moduli migrati nella Wave 1 MVP:
//    - Prodotti + ScadenzeProdotto
//    - Ordini + RigheOrdine
//    - ProposteOrdine + ProposteRighe
//
//  Applicare con:
//    dotnet ef database update --project SistemaF.Infrastructure
//      --startup-project SistemaF.UI.WPF
//
//  Oppure all'avvio dell'app (modalità sviluppo):
//    await InfrastructureDependencyInjection.InitializeDatabaseAsync(services);
// ═══════════════════════════════════════════════════════════════════════════════

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Prodotti ──────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "Prodotti",
            schema: "dbo",
            columns: t => new
            {
                Id                   = t.Column<Guid>(nullable: false),
                CodiceFarmaco        = t.Column<string>(maxLength: 9, nullable: false),
                CodiceEAN            = t.Column<string>(maxLength: 13, nullable: true),
                CodiceATC            = t.Column<string>(maxLength: 10, nullable: true),
                CodiceDitta          = t.Column<string>(maxLength: 30, nullable: true),
                Targatura            = t.Column<string>(maxLength: 30, nullable: true),
                Descrizione          = t.Column<string>(maxLength: 200, nullable: false),
                FormaBiotica         = t.Column<string>(maxLength: 50, nullable: true),
                Sostanza             = t.Column<string>(maxLength: 100, nullable: true),
                Patologia            = t.Column<string>(maxLength: 100, nullable: true),
                Gruppo               = t.Column<string>(maxLength: 50, nullable: true),
                LineaDitta           = t.Column<string>(maxLength: 50, nullable: true),
                Nomenclatore         = t.Column<string>(maxLength: 50, nullable: true),
                Classe               = t.Column<int>(nullable: false),
                CategoriaRicetta     = t.Column<int>(nullable: false),
                IsStupefacente       = t.Column<bool>(defaultValue: false, nullable: false),
                IsVeterinario        = t.Column<bool>(defaultValue: false, nullable: false),
                IsCongelato          = t.Column<bool>(defaultValue: false, nullable: false),
                IsTrattato           = t.Column<bool>(defaultValue: false, nullable: false),
                IsIntegrativo        = t.Column<bool>(defaultValue: false, nullable: false),
                IsPluriPrescrizione  = t.Column<bool>(defaultValue: false, nullable: false),
                IsAttivo             = t.Column<bool>(defaultValue: true, nullable: false),
                // PrezzoVendita (VO owned)
                PrezzoVendita        = t.Column<decimal>(type: "decimal(10,2)", nullable: false),
                AliquotaIVAVendita   = t.Column<int>(nullable: false),
                // PrezzoListino (VO owned nullable)
                PrezzoListino        = t.Column<decimal>(type: "decimal(10,2)", nullable: true),
                // PrezzoRiferimentoSSN
                PrezzoSSN            = t.Column<decimal>(type: "decimal(10,2)", nullable: true),
                // PrezzoAcquisto
                PrezzoAcquisto       = t.Column<decimal>(type: "decimal(10,2)", nullable: true),
                // GiacenzaEsposizione (VO owned)
                GiacenzaEsp_Giacenza      = t.Column<int>(defaultValue: 0, nullable: false),
                GiacenzaEsp_ScortaMinima  = t.Column<int>(defaultValue: 0, nullable: false),
                GiacenzaEsp_ScortaMassima = t.Column<int>(defaultValue: 0, nullable: false),
                // GiacenzaMagazzino (VO owned)
                GiacenzaMag_Giacenza      = t.Column<int>(defaultValue: 0, nullable: false),
                GiacenzaMag_ScortaMinima  = t.Column<int>(defaultValue: 0, nullable: false),
                GiacenzaMag_ScortaMassima = t.Column<int>(defaultValue: 0, nullable: false),
                // Giacenza invendibile
                GiacenzaInvendibile  = t.Column<int>(defaultValue: 0, nullable: false),
                IsInvendibile        = t.Column<bool>(defaultValue: false, nullable: false),
                // Soft delete
                IsDeleted            = t.Column<bool>(defaultValue: false, nullable: false),
                DeletedAt            = t.Column<DateTime>(nullable: true),
                DeletedBy            = t.Column<Guid>(nullable: true),
                // Audit
                CreatedAt            = t.Column<DateTime>(nullable: false),
                UpdatedAt            = t.Column<DateTime>(nullable: true),
                CreatedBy            = t.Column<Guid>(nullable: true),
                UpdatedBy            = t.Column<Guid>(nullable: true),
                RowVersion           = t.Column<byte[]>(rowVersion: true, nullable: true),
            },
            constraints: t => t.PrimaryKey("PK_Prodotti", x => x.Id));

        migrationBuilder.CreateIndex("UX_Prodotti_Codice", "Prodotti", "CodiceFarmaco",
            schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("IX_Prodotti_Descrizione", "Prodotti", "Descrizione",
            schema: "dbo");
        migrationBuilder.CreateIndex("IX_Prodotti_EAN", "Prodotti", "CodiceEAN",
            schema: "dbo", unique: false, filter: "[CodiceEAN] IS NOT NULL");

        // ── ScadenzeProdotto ──────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "ScadenzeProdotto",
            schema: "dbo",
            columns: t => new
            {
                Id                     = t.Column<Guid>(nullable: false),
                ProdottoId             = t.Column<Guid>(nullable: false),
                CodiceLotto            = t.Column<string>(maxLength: 30, nullable: false),
                DataScadenza           = t.Column<DateOnly>(nullable: false),
                Quantita               = t.Column<int>(nullable: false),
                QuantitaMagazzino      = t.Column<int>(defaultValue: 0, nullable: false),
                IsInvendibile          = t.Column<bool>(defaultValue: false, nullable: false),
                MotivoInvendibilita    = t.Column<string>(maxLength: 200, nullable: true),
                DataCarico             = t.Column<DateTime>(nullable: false),
                CreatedAt              = t.Column<DateTime>(nullable: false),
                UpdatedAt              = t.Column<DateTime>(nullable: true),
                CreatedBy              = t.Column<Guid>(nullable: true),
                UpdatedBy              = t.Column<Guid>(nullable: true),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_Scadenze", x => x.Id);
                t.ForeignKey("FK_Scadenze_Prodotti", x => x.ProdottoId,
                    principalSchema: "dbo", principalTable: "Prodotti",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Scadenze_Prodotto", "ScadenzeProdotto",
            "ProdottoId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_Scadenze_Data", "ScadenzeProdotto",
            "DataScadenza", schema: "dbo");

        // ── Ordini ────────────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "Ordini",
            schema: "dbo",
            columns: t => new
            {
                Id                   = t.Column<Guid>(nullable: false),
                NumeroOrdine         = t.Column<string>(maxLength: 9, nullable: false),
                PropostaId           = t.Column<Guid>(nullable: false),
                FornitoreId          = t.Column<Guid>(nullable: false),
                CodiceAnabase        = t.Column<long>(nullable: false),
                TipoFornitore        = t.Column<int>(nullable: false),
                RagioneSociale       = t.Column<string>(maxLength: 100, nullable: false),
                Stato                = t.Column<int>(nullable: false),
                DataEmissione        = t.Column<DateTime>(nullable: false),
                DataTrasmissione     = t.Column<DateTime>(nullable: true),
                DataRicezione        = t.Column<DateTime>(nullable: true),
                OperatoreId          = t.Column<Guid>(nullable: false),
                Note                 = t.Column<string>(maxLength: 500, nullable: true),
                NomeEmissione        = t.Column<string>(maxLength: 100, nullable: true),
                IsDeleted            = t.Column<bool>(defaultValue: false, nullable: false),
                DeletedAt            = t.Column<DateTime>(nullable: true),
                DeletedBy            = t.Column<Guid>(nullable: true),
                CreatedAt            = t.Column<DateTime>(nullable: false),
                UpdatedAt            = t.Column<DateTime>(nullable: true),
                CreatedBy            = t.Column<Guid>(nullable: true),
                UpdatedBy            = t.Column<Guid>(nullable: true),
                RowVersion           = t.Column<byte[]>(rowVersion: true, nullable: true),
            },
            constraints: t => t.PrimaryKey("PK_Ordini", x => x.Id));

        migrationBuilder.CreateIndex("UX_Ordini_Numero", "Ordini", "NumeroOrdine",
            schema: "dbo", unique: true);
        migrationBuilder.CreateIndex("IX_Ordini_Fornitore", "Ordini", "FornitoreId",
            schema: "dbo");
        migrationBuilder.CreateIndex("IX_Ordini_Stato", "Ordini", "Stato",
            schema: "dbo");
        migrationBuilder.CreateIndex("IX_Ordini_DataEmissione", "Ordini", "DataEmissione",
            schema: "dbo");

        // ── RigheOrdine ───────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "RigheOrdine",
            schema: "dbo",
            columns: t => new
            {
                Id               = t.Column<Guid>(nullable: false),
                OrdineId         = t.Column<Guid>(nullable: false),
                ProdottoId       = t.Column<Guid>(nullable: false),
                CodiceFarmaco    = t.Column<string>(maxLength: 9, nullable: false),
                Descrizione      = t.Column<string>(maxLength: 200, nullable: true),
                Quantita         = t.Column<int>(nullable: false),
                QuantitaOmaggio  = t.Column<int>(defaultValue: 0, nullable: false),
                CostoUnitario    = t.Column<decimal>(type: "decimal(10,4)", nullable: false),
                Sconto           = t.Column<decimal>(type: "decimal(6,4)", nullable: false),
                ExtraSconto      = t.Column<decimal>(type: "decimal(6,4)", nullable: false),
                ScontoLordo      = t.Column<decimal>(type: "decimal(6,4)", nullable: false),
                Margine          = t.Column<decimal>(type: "decimal(6,4)", nullable: false),
                PrezzoListino    = t.Column<decimal>(type: "decimal(10,2)", nullable: false),
                AliquotaIVA      = t.Column<int>(nullable: false),
                QuantitaMancante  = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaNecessita = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaPrenotata = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaSospesa   = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaArchivio  = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaArrivata  = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaAssicurata = t.Column<int>(defaultValue: 0, nullable: false),
                CodiceMancante   = t.Column<string>(maxLength: 30, nullable: true),
                CreatedAt        = t.Column<DateTime>(nullable: false),
                UpdatedAt        = t.Column<DateTime>(nullable: true),
                CreatedBy        = t.Column<Guid>(nullable: true),
                UpdatedBy        = t.Column<Guid>(nullable: true),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_RigheOrdine", x => x.Id);
                t.ForeignKey("FK_RigheOrdine_Ordini", x => x.OrdineId,
                    principalSchema: "dbo", principalTable: "Ordini",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_RigheOrdine_Ordine", "RigheOrdine",
            "OrdineId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_RigheOrdine_Prodotto", "RigheOrdine",
            "ProdottoId", schema: "dbo");

        // ── ProposteOrdine ────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "ProposteOrdine",
            schema: "dbo",
            columns: t => new
            {
                Id                       = t.Column<Guid>(nullable: false),
                OperatoreId              = t.Column<Guid>(nullable: false),
                ConfigurazioneId         = t.Column<Guid>(nullable: false),
                NomeEmissione            = t.Column<string>(maxLength: 100, nullable: true),
                Stato                    = t.Column<int>(nullable: false),
                DataCreazione            = t.Column<DateTime>(nullable: false),
                Interrotta               = t.Column<bool>(defaultValue: false, nullable: false),
                IsEmissioneDaArchivio    = t.Column<bool>(defaultValue: false, nullable: false),
                IsEmissioneNecessita     = t.Column<bool>(defaultValue: false, nullable: false),
                IsEmissionePrenotati     = t.Column<bool>(defaultValue: false, nullable: false),
                IsEmissioneSospesi       = t.Column<bool>(defaultValue: false, nullable: false),
                IsOrdineLiberoDitta      = t.Column<bool>(defaultValue: false, nullable: false),
                IsRicalcolaNecessita     = t.Column<bool>(defaultValue: false, nullable: false),
                IsMagazzinoPresente      = t.Column<bool>(defaultValue: false, nullable: false),
                IndiceMagazzino          = t.Column<int>(defaultValue: 0, nullable: false),
                // IndiceDiVendita owned
                IndiceVenditaTipo        = t.Column<int>(defaultValue: 0, nullable: false),
                IndiceVenditaGiorni      = t.Column<int>(defaultValue: 0, nullable: false),
                IndiceVenditaMin         = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaMax         = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaInclude     = t.Column<bool>(defaultValue: true, nullable: false),
                IndiceVenditaSottraiGiacenze = t.Column<bool>(defaultValue: false, nullable: false),
                IndiceVenditaRicalcola   = t.Column<bool>(defaultValue: false, nullable: false),
                // RipristinoScorta owned
                RipristinoScortaTipo     = t.Column<int>(defaultValue: 0, nullable: false),
                RipristinoConsideraEntroScorte = t.Column<bool>(defaultValue: false, nullable: false),
                RipristinoConsideraScortaSicurezza = t.Column<bool>(defaultValue: false, nullable: false),
                // Riepilogo owned
                RiepilogoNome            = t.Column<string>(maxLength: 100, nullable: true),
                RiepilogoInizio          = t.Column<DateTime>(nullable: false),
                RiepilogoFine            = t.Column<DateTime>(nullable: false),
                RiepilogoProdottiGlobali  = t.Column<long>(defaultValue: 0L, nullable: false),
                RiepilogoProdottiEsaminati = t.Column<long>(defaultValue: 0L, nullable: false),
                RiepilogoProdottiInOrdine  = t.Column<long>(defaultValue: 0L, nullable: false),
                CreatedAt                = t.Column<DateTime>(nullable: false),
                UpdatedAt                = t.Column<DateTime>(nullable: true),
                CreatedBy                = t.Column<Guid>(nullable: true),
                UpdatedBy                = t.Column<Guid>(nullable: true),
                RowVersion               = t.Column<byte[]>(rowVersion: true, nullable: true),
            },
            constraints: t => t.PrimaryKey("PK_ProposteOrdine", x => x.Id));

        migrationBuilder.CreateIndex("IX_Proposte_Operatore", "ProposteOrdine",
            "OperatoreId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_Proposte_Stato", "ProposteOrdine",
            "Stato", schema: "dbo");

        // ── ProposteRighe ─────────────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name:   "ProposteRighe",
            schema: "dbo",
            columns: t => new
            {
                Id                       = t.Column<Guid>(nullable: false),
                PropostaId               = t.Column<Guid>(nullable: false),
                ProdottoId               = t.Column<Guid>(nullable: false),
                CodiceFarmaco            = t.Column<string>(maxLength: 9, nullable: false),
                Descrizione              = t.Column<string>(maxLength: 200, nullable: true),
                QuantitaMancante         = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaNecessita        = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaPrenotata        = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaSospesa          = t.Column<int>(defaultValue: 0, nullable: false),
                QuantitaArchivio         = t.Column<int>(defaultValue: 0, nullable: false),
                PrezzoListino            = t.Column<decimal>(type: "decimal(10,2)", defaultValue: 0m, nullable: false),
                PrezzoVendita            = t.Column<decimal>(type: "decimal(10,2)", defaultValue: 0m, nullable: false),
                PrezzoFarmacia           = t.Column<decimal>(type: "decimal(10,2)", defaultValue: 0m, nullable: false),
                PrezzoRiferimento        = t.Column<decimal>(type: "decimal(10,2)", defaultValue: 0m, nullable: false),
                AliquotaIVA              = t.Column<int>(defaultValue: 10, nullable: false),
                Classe                   = t.Column<string>(maxLength: 1, nullable: true),
                GiacenzaEsposizione      = t.Column<int>(defaultValue: 0, nullable: false),
                ScortaMinimaEsposizione  = t.Column<int>(defaultValue: 0, nullable: false),
                ScortaMassimaEsposizione = t.Column<int>(defaultValue: 0, nullable: false),
                GiacenzaMagazzino        = t.Column<int>(defaultValue: 0, nullable: false),
                ScortaMinimaMagazzino    = t.Column<int>(defaultValue: 0, nullable: false),
                ScortaMassimaMagazzino   = t.Column<int>(defaultValue: 0, nullable: false),
                IsCongelato              = t.Column<bool>(defaultValue: false, nullable: false),
                IsVeterinario            = t.Column<bool>(defaultValue: false, nullable: false),
                IsStupefacente           = t.Column<bool>(defaultValue: false, nullable: false),
                IsSegnalato              = t.Column<bool>(defaultValue: false, nullable: false),
                IsTrattatoDitta          = t.Column<bool>(defaultValue: false, nullable: false),
                IsSistemaAutomatico      = t.Column<bool>(defaultValue: false, nullable: false),
                IsOtcSop                 = t.Column<bool>(defaultValue: false, nullable: false),
                IsGenerico               = t.Column<bool>(defaultValue: false, nullable: false),
                IsInOrdine               = t.Column<bool>(defaultValue: false, nullable: false),
                IsPreferenziale          = t.Column<bool>(defaultValue: false, nullable: false),
                DaOrdinare               = t.Column<bool>(defaultValue: true, nullable: false),
                DaEliminare              = t.Column<bool>(defaultValue: false, nullable: false),
                SettoreInventario        = t.Column<string>(maxLength: 5, nullable: true),
                Priorita                 = t.Column<int>(defaultValue: 0, nullable: false),
                IndiceVenditaTendenziale    = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaMensile        = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaAnnuale        = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaPeriodo        = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                IndiceVenditaMediaAritmetica = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m, nullable: false),
                FornitorePreferenzialeId    = t.Column<Guid>(nullable: true),
                CodiceFornitorePreferenziale = t.Column<long>(defaultValue: 0L, nullable: false),
                // Array fornitori serializzati come JSON
                FornitoriAbilitati       = t.Column<string>(maxLength: 50, defaultValue: "[false,false,false,false,false]", nullable: false),
                QuantitaFornitori        = t.Column<string>(maxLength: 50, defaultValue: "[0,0,0,0,0]", nullable: false),
                OmaggioFornitori         = t.Column<string>(maxLength: 50, defaultValue: "[0,0,0,0,0]", nullable: false),
                CreatedAt                = t.Column<DateTime>(nullable: false),
                UpdatedAt                = t.Column<DateTime>(nullable: true),
                CreatedBy                = t.Column<Guid>(nullable: true),
                UpdatedBy                = t.Column<Guid>(nullable: true),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_ProposteRighe", x => x.Id);
                t.ForeignKey("FK_ProposteRighe_ProposteOrdine", x => x.PropostaId,
                    principalSchema: "dbo", principalTable: "ProposteOrdine",
                    principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_ProposteRighe_Proposta", "ProposteRighe",
            "PropostaId", schema: "dbo");
        migrationBuilder.CreateIndex("IX_ProposteRighe_Prodotto", "ProposteRighe",
            "ProdottoId", schema: "dbo");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ProposteRighe",  schema: "dbo");
        migrationBuilder.DropTable(name: "ProposteOrdine", schema: "dbo");
        migrationBuilder.DropTable(name: "RigheOrdine",    schema: "dbo");
        migrationBuilder.DropTable(name: "Ordini",         schema: "dbo");
        migrationBuilder.DropTable(name: "ScadenzeProdotto", schema: "dbo");
        migrationBuilder.DropTable(name: "Prodotti",       schema: "dbo");
    }
}
