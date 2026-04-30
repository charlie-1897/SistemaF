using Microsoft.EntityFrameworkCore;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;
using SistemaF.Infrastructure.Persistence;

namespace SistemaF.Infrastructure.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(SistemaFDbContext db, CancellationToken ct = default)
    {
        if (await db.Prodotti.AnyAsync(ct)) return;
        await db.Prodotti.AddRangeAsync(CreaProdotti(), ct);
        await db.SaveChangesAsync(ct);
    }

    private static List<Prodotto> CreaProdotti()
    {
        var lista = new List<Prodotto>();

        // Classe A — rimborsabili SSN
        lista.Add(Crea("023569287", "AMOXICILLINA EG 1G 12 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 5.50m, 4, 8, 24));
        lista.Add(Crea("026974015", "AUGMENTIN 1G 12 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 11.20m, 10, 6, 18));
        lista.Add(Crea("034512367", "CARDIOASPIRINA 100MG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 3.80m, 10, 20, 60));
        lista.Add(Crea("024654213", "LANSOX 30MG 28 CPS", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 7.90m, 10, 10, 30));
        lista.Add(Crea("038712589", "ATORVASTATINA EG 20MG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 4.20m, 10, 12, 36));
        lista.Add(Crea("027314826", "METFORMINA MG 500MG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 2.60m, 10, 15, 45));
        lista.Add(Crea("031456892", "RAMIPRIL EG 5MG 28 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 3.40m, 10, 10, 30));
        lista.Add(Crea("035621478", "AMLODIPINA EG 5MG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 4.10m, 10, 8, 24));
        lista.Add(Crea("029874536", "FUROSEMIDE 25MG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 2.90m, 10, 10, 20));
        lista.Add(Crea("033145678", "OMEPRAZOLO DOC 20MG 14 CPS", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 3.70m, 10, 12, 36));
        lista.Add(Crea("041236985", "LEVOTIROXINA 100MCG 30 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaNonRipetibile, 3.50m, 10, 8, 20));
        lista.Add(Crea("036978512", "PANTOPRAZOLO EG 40MG 14 CPR", ClasseFarmaco.A,
            CategoriaRicetta.RicettaRipetibile, 4.60m, 10, 10, 30));

        // Classe C — non rimborsabili
        lista.Add(Crea("023145698", "TACHIPIRINA 1000MG 16 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 6.50m, 10, 30, 90));
        lista.Add(Crea("026489751", "BRUFEN 600MG 20 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 5.90m, 10, 20, 60));
        lista.Add(Crea("031289456", "ASPIRINA 500MG 20 CPR", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 3.20m, 10, 25, 75));
        lista.Add(Crea("028745632", "BUSCOPAN 10MG 30 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 8.40m, 10, 15, 45));
        lista.Add(Crea("034562178", "BENTELAN 0.5MG 20 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 4.90m, 10, 8, 20));
        lista.Add(Crea("025634189", "MOMENT 200MG 12 CPR", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 4.60m, 10, 20, 60));
        lista.Add(Crea("037896541", "BIAXIN 500MG 14 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 14.20m, 10, 5, 15));
        lista.Add(Crea("039845126", "MODURETIC 5MG 30 CPR", ClasseFarmaco.C,
            CategoriaRicetta.RicettaRipetibile, 9.80m, 10, 6, 18));

        // SOP / OTC
        lista.Add(Crea("023987456", "GAVISCON MENTA 24 BUST", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 7.80m, 10, 18, 54));
        lista.Add(Crea("031764892", "ENTEROGERMINA 2MLD 12 FL", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 9.40m, 10, 15, 45));
        lista.Add(Crea("028963741", "RIOPAN GEL 250ML", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 8.90m, 10, 10, 20));
        lista.Add(Crea("036741289", "NUROFEN 200MG 12 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 5.20m, 10, 20, 60));
        lista.Add(Crea("041598263", "VOLTAREN EMULGEL 100G", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 11.90m, 10, 12, 36));
        lista.Add(Crea("027483619", "BISOLVON 10ML SCIROPPO", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 6.40m, 10, 10, 30));
        lista.Add(Crea("033896214", "MUCOSOLVAN 30MG 20 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 7.60m, 10, 8, 24));
        lista.Add(Crea("039127486", "AERIUS 5MG 7 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 8.30m, 10, 10, 30));
        lista.Add(Crea("024698513", "LORATADINA 10MG 7 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 4.90m, 10, 12, 36));
        lista.Add(Crea("030784512", "STREPSILS FRAGOLA 24 PAST", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 6.90m, 10, 20, 60));
        lista.Add(Crea("028314756", "VICKS SINEX SPRAY 15ML", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 7.30m, 10, 15, 45));
        lista.Add(Crea("034127896", "ANTIBIOTICO CREMA 30G", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 8.50m, 10, 8, 16));
        lista.Add(Crea("022369874", "MULTIVIT COMPLETO 30 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 12.40m, 10, 10, 30));
        lista.Add(Crea("037456198", "FENISTIL GEL 30G", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 9.10m, 10, 8, 24));
        lista.Add(Crea("031987456", "CALMABEN 25MG 10 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 5.80m, 10, 6, 18));
        lista.Add(Crea("026741389", "ACTIFED 12 CPR", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 7.20m, 10, 10, 30));
        lista.Add(Crea("043217896", "REKAMERON TOSSE 10 PAST", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 5.40m, 10, 15, 45));
        lista.Add(Crea("029654718", "MAGNESIA BISURATA 30 BUST", ClasseFarmaco.SopOtc,
            CategoriaRicetta.NessunObbligo, 8.70m, 10, 12, 36));

        // Dispositivi / Parafarmaci
        lista.Add(Crea("041236478", "COLLIRIO VISINE 15ML", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 8.20m, 10, 10, 20));
        lista.Add(Crea("036987412", "GLUCOSIO STRIP 50 PZ", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 18.90m, 10, 5, 15));
        lista.Add(Crea("028743196", "BENDE ELASTICHE CM10", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 3.40m, 10, 20, 40));
        lista.Add(Crea("034198756", "CEROTTO CICATRENE 12 PZ", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 5.90m, 10, 15, 30));
        lista.Add(Crea("039874123", "BETADINE 125ML", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 6.80m, 10, 12, 24));
        lista.Add(Crea("027896534", "GARZA STERILE 12 PZ", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 4.20m, 10, 20, 40));
        lista.Add(Crea("033574218", "SIRINGHE INSULINA 100 PZ", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 9.60m, 10, 8, 16));

        // Integratori
        lista.Add(Crea("040219876", "OMEGA 3 1000MG 30 CPS", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 14.50m, 10, 8, 20));
        lista.Add(Crea("035698124", "VITAMINA D3 1000UI 60 CPR", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 11.80m, 10, 10, 30));
        lista.Add(Crea("028147963", "MAGNESIO SUPREMO 300G", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 18.40m, 10, 6, 18));
        lista.Add(Crea("043681259", "PROBIOTICI BIFIDUS 10 BUST", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 13.60m, 10, 8, 20));
        lista.Add(Crea("031459826", "COLLAGENE MARINO 60 CPR", ClasseFarmaco.C,
            CategoriaRicetta.NessunObbligo, 22.90m, 10, 5, 15));

        return lista;
    }

    private static Prodotto Crea(
        string codice, string descrizione,
        ClasseFarmaco classe, CategoriaRicetta categoria,
        decimal prezzoVendita, int iva,
        int qtaExp, int qtaMag)
    {
        var p = Prodotto.Crea(
            CodiceProdotto.Da(codice),
            descrizione,
            classe,
            categoria,
            Prezzo.Da(prezzoVendita, iva));

        // Giacenza esposizione
        p.VariaGiacenzaEsposizione(
            ModalitaVariazioneGiacenza.Sostituzione,
            qtaExp, TipoAzioneRettifica.Incremento,
            TipoCosaRettifica.GiacenzaEsposizione,
            TipoModuloRettifica.ManualeSistema);

        // Scorte esposizione
        p.ImpostaScorteEsposizione(3, qtaExp + 5);

        // Giacenza magazzino
        p.VariaGiacenzaMagazzino(
            ModalitaVariazioneGiacenza.Sostituzione,
            qtaMag, TipoAzioneRettifica.Incremento,
            TipoCosaRettifica.GiacenzaMagazzino,
            TipoModuloRettifica.ManualeSistema);

        p.ClearDomainEvents();
        return p;
    }
}
