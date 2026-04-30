using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaF.Infrastructure.Persistence.Migrations;

// ═══════════════════════════════════════════════════════════════════════════════
//  MIGRATION: AddAnagrafica (Sessione 2 MVP)
//
//  Aggiunge le tabelle:
//    - Fornitori
//    - Operatori
//    - Farmacia
//    - ConfigurazioniEmissione
//    - ConfigurazioniEmissioneFornitori
// ═══════════════════════════════════════════════════════════════════════════════

public partial class AddAnagrafica : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // ── Fornitori ─────────────────────────────────────────────────────────
        m.CreateTable("Fornitori", schema: "dbo", columns: t => new
        {
            Id                      = t.Column<Guid>(nullable: false),
            CodiceAnabase           = t.Column<long>(defaultValue: 0L),
            RagioneSociale          = t.Column<string>(maxLength: 150, nullable: false),
            Tipo                    = t.Column<int>(nullable: false),
            PartitaIVA              = t.Column<string>(maxLength: 16, nullable: true),
            CodiceFiscale           = t.Column<string>(maxLength: 16, nullable: true),
            Annotazione             = t.Column<string>(maxLength: 500, nullable: true),
            // Sede owned
            Indirizzo               = t.Column<string>(maxLength: 150, nullable: true),
            CAP                     = t.Column<string>(maxLength: 10, nullable: true),
            Localita                = t.Column<string>(maxLength: 80, nullable: true),
            Provincia               = t.Column<string>(maxLength: 2, nullable: true),
            // Contatti owned
            Telefono                = t.Column<string>(maxLength: 30, nullable: true),
            Fax                     = t.Column<string>(maxLength: 30, nullable: true),
            Email                   = t.Column<string>(maxLength: 100, nullable: true),
            Cellulare               = t.Column<string>(maxLength: 30, nullable: true),
            SitoWeb                 = t.Column<string>(maxLength: 200, nullable: true),
            // Deposito owned (nullable)
            NomeDeposito            = t.Column<string>(maxLength: 100, nullable: true),
            DepositoIndirizzo       = t.Column<string>(maxLength: 150, nullable: true),
            DepositoCAP             = t.Column<string>(maxLength: 10, nullable: true),
            DepositoLocalita        = t.Column<string>(maxLength: 80, nullable: true),
            DepositoProvincia       = t.Column<string>(maxLength: 2, nullable: true),
            // Parametri commerciali
            NomeContatto            = t.Column<string>(maxLength: 100, nullable: true),
            EmailContatto           = t.Column<string>(maxLength: 100, nullable: true),
            BudgetStimato           = t.Column<decimal>(type: "decimal(12,2)", defaultValue: 0m),
            IsMagazzino             = t.Column<bool>(defaultValue: false),
            IsPreferenzialeDefault  = t.Column<bool>(defaultValue: false),
            IsFornitoreGruppo       = t.Column<bool>(defaultValue: false),
            PercentualeRipartizione = t.Column<int>(defaultValue: 100),
            IsAttivo                = t.Column<bool>(defaultValue: true),
            // Audit
            CreatedAt               = t.Column<DateTime>(nullable: false),
            UpdatedAt               = t.Column<DateTime>(nullable: true),
            CreatedBy               = t.Column<Guid>(nullable: true),
            UpdatedBy               = t.Column<Guid>(nullable: true),
            RowVersion              = t.Column<byte[]>(rowVersion: true, nullable: true),
        }, constraints: t => t.PrimaryKey("PK_Fornitori", x => x.Id));

        m.CreateIndex("UX_Fornitori_CodiceAnabase", "Fornitori", "CodiceAnabase",
            schema: "dbo", unique: true,
            filter: "[CodiceAnabase] > 0");
        m.CreateIndex("IX_Fornitori_Tipo",   "Fornitori", "Tipo",    schema: "dbo");
        m.CreateIndex("IX_Fornitori_Attivi", "Fornitori", "IsAttivo", schema: "dbo");

        // ── Operatori ─────────────────────────────────────────────────────────
        m.CreateTable("Operatori", schema: "dbo", columns: t => new
        {
            Id                    = t.Column<Guid>(nullable: false),
            Login                 = t.Column<string>(maxLength: 50, nullable: false),
            NomeCognome           = t.Column<string>(maxLength: 100, nullable: false),
            PasswordHash          = t.Column<string>(maxLength: 256, nullable: false),
            Badge                 = t.Column<string>(maxLength: 50, nullable: true),
            AutorizzazioniLegacy  = t.Column<string>(maxLength: 50, defaultValue: new string('0', 50)),
            IsAttivo              = t.Column<bool>(defaultValue: true),
            IsAmministratore      = t.Column<bool>(defaultValue: false),
            CreatedAt             = t.Column<DateTime>(nullable: false),
            UpdatedAt             = t.Column<DateTime>(nullable: true),
            CreatedBy             = t.Column<Guid>(nullable: true),
            UpdatedBy             = t.Column<Guid>(nullable: true),
        }, constraints: t => t.PrimaryKey("PK_Operatori", x => x.Id));

        m.CreateIndex("UX_Operatori_Login", "Operatori", "Login",
            schema: "dbo", unique: true);
        m.CreateIndex("UX_Operatori_Badge", "Operatori", "Badge",
            schema: "dbo", unique: true, filter: "[Badge] IS NOT NULL");

        // ── Farmacia ──────────────────────────────────────────────────────────
        m.CreateTable("Farmacia", schema: "dbo", columns: t => new
        {
            Id                  = t.Column<Guid>(nullable: false),
            Nome                = t.Column<string>(maxLength: 150, nullable: false),
            RagioneSociale      = t.Column<string>(maxLength: 150, nullable: true),
            PartitaIVA          = t.Column<string>(maxLength: 16, nullable: true),
            CodiceFiscale       = t.Column<string>(maxLength: 16, nullable: true),
            CodiceFarmaciaAsl   = t.Column<string>(maxLength: 20, nullable: true),
            CodiceAsl           = t.Column<string>(maxLength: 20, nullable: true),
            NomeAsl             = t.Column<string>(maxLength: 80, nullable: true),
            RegioneFarmacia     = t.Column<string>(maxLength: 50, nullable: true),
            TitolareFarmacia    = t.Column<string>(maxLength: 100, nullable: true),
            HaMagazzino         = t.Column<bool>(defaultValue: false),
            Indirizzo           = t.Column<string>(maxLength: 150, nullable: true),
            CAP                 = t.Column<string>(maxLength: 10, nullable: true),
            Localita            = t.Column<string>(maxLength: 80, nullable: true),
            Provincia           = t.Column<string>(maxLength: 2, nullable: true),
            Telefono            = t.Column<string>(maxLength: 30, nullable: true),
            Fax                 = t.Column<string>(maxLength: 30, nullable: true),
            Email               = t.Column<string>(maxLength: 100, nullable: true),
            SitoWeb             = t.Column<string>(maxLength: 200, nullable: true),
            CreatedAt           = t.Column<DateTime>(nullable: false),
            UpdatedAt           = t.Column<DateTime>(nullable: true),
        }, constraints: t => t.PrimaryKey("PK_Farmacia", x => x.Id));

        // ── ConfigurazioniEmissione ───────────────────────────────────────────
        m.CreateTable("ConfigurazioniEmissione", schema: "dbo", columns: t => new
        {
            Id                         = t.Column<Guid>(nullable: false),
            Nome                       = t.Column<string>(maxLength: 100, nullable: false),
            Descrizione                = t.Column<string>(maxLength: 300, nullable: true),
            IsAttiva                   = t.Column<bool>(defaultValue: true),
            DaArchivio                 = t.Column<bool>(defaultValue: true),
            DaNecessita                = t.Column<bool>(defaultValue: true),
            DaPrenotati                = t.Column<bool>(defaultValue: false),
            DaSospesi                  = t.Column<bool>(defaultValue: false),
            OrdineLiberoDitta          = t.Column<bool>(defaultValue: false),
            RicalcolaNecessita         = t.Column<bool>(defaultValue: false),
            TipoIndiceVendita          = t.Column<int>(defaultValue: 0),
            GiorniCopertura            = t.Column<int>(defaultValue: 7),
            IndiceVenditaMin           = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m),
            IndiceVenditaMax           = t.Column<decimal>(type: "decimal(10,4)", defaultValue: 0m),
            SottraiGiacenze            = t.Column<bool>(defaultValue: false),
            TipoRipristinoScorta       = t.Column<int>(defaultValue: 0),
            ConsideraEntroLeScorte     = t.Column<bool>(defaultValue: false),
            ConsideraScortaDiSicurezza = t.Column<bool>(defaultValue: false),
            TipoOrdinamento            = t.Column<int>(defaultValue: 0),
            CreatedAt                  = t.Column<DateTime>(nullable: false),
            UpdatedAt                  = t.Column<DateTime>(nullable: true),
            CreatedBy                  = t.Column<Guid>(nullable: true),
            UpdatedBy                  = t.Column<Guid>(nullable: true),
        }, constraints: t => t.PrimaryKey("PK_ConfigurazioniEmissione", x => x.Id));

        m.CreateIndex("IX_Configurazioni_Nome",   "ConfigurazioniEmissione",
            "Nome", schema: "dbo");
        m.CreateIndex("IX_Configurazioni_Attive", "ConfigurazioniEmissione",
            "IsAttiva", schema: "dbo");

        // ── ConfigurazioniEmissioneFornitori ──────────────────────────────────
        m.CreateTable("ConfigurazioniEmissioneFornitori", schema: "dbo",
            columns: t => new
            {
                Id                      = t.Column<Guid>(nullable: false),
                ConfigurazioneId        = t.Column<Guid>(nullable: false),
                FornitoreId             = t.Column<Guid>(nullable: false),
                OrdineIndice            = t.Column<int>(defaultValue: 1),
                PercentualeRipartizione = t.Column<int>(defaultValue: 100),
                IsAbilitato             = t.Column<bool>(defaultValue: true),
                CreatedAt               = t.Column<DateTime>(nullable: false),
                UpdatedAt               = t.Column<DateTime>(nullable: true),
            },
            constraints: t =>
            {
                t.PrimaryKey("PK_ConfigurazioniFornitori", x => x.Id);
                t.ForeignKey("FK_ConfigForn_Configurazioni",
                    x => x.ConfigurazioneId,
                    principalSchema: "dbo",
                    principalTable: "ConfigurazioniEmissione",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                t.ForeignKey("FK_ConfigForn_Fornitori",
                    x => x.FornitoreId,
                    principalSchema: "dbo",
                    principalTable: "Fornitori",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        m.CreateIndex("IX_ConfigurazioniFornitori_Config",
            "ConfigurazioniEmissioneFornitori", "ConfigurazioneId", schema: "dbo");
        m.CreateIndex("UX_ConfigurazioniFornitori_Pair",
            "ConfigurazioniEmissioneFornitori",
            new[] { "ConfigurazioneId", "FornitoreId" },
            schema: "dbo", unique: true);
    }

    protected override void Down(MigrationBuilder m)
    {
        m.DropTable("ConfigurazioniEmissioneFornitori", schema: "dbo");
        m.DropTable("ConfigurazioniEmissione",          schema: "dbo");
        m.DropTable("Farmacia",                         schema: "dbo");
        m.DropTable("Operatori",                        schema: "dbo");
        m.DropTable("Fornitori",                        schema: "dbo");
    }
}
