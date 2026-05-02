using SistemaF.Domain.Entities.Prodotto;

namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  EMISSIONE ORDINE SERVICE — Domain Service
//
//  Migrazione della pipeline in clsEmissioneOrdine.RecuperaProdotti.
//
//  La pipeline originale VB6 aveva questi stadi in sequenza:
//   1. VerificaPresenzaProdotti          → CheckResiduiPrecedenti
//   2. RecuperaProdotti_ProdBase          → AggiungiDaArchivio
//   3. RecuperaProdotti_Necessita         → AggiungiDaNecessita
//   4. RecuperaProdotti_Prenotati         → AggiungiDaPrenotati
//   5. RecuperaProdotti_Sospesi           → AggiungiDaSospesi
//   6. AzzeraFornitoreErrato              → (internal clean-up)
//   7. RaggruppaProdotti                  → RaggruppaProdotti
//   8. RecuperaAltreInformazioni1         → ArricchisciConPrezziEIndici
//   9. AzzeraValoriNulli                  → (garantito dal value object)
//  10. ApplicaFiltri (per fornitore)      → ApplicaFiltriFornitore
//  11. EliminaProdottiFornitoreEscluso    → EliminaAssegnazioniEscluse
//  12. VerificaAssegnazioni              → AssegnaFornitoriOttimali
//  13. RimuoviMarcaturaSuperflua          → RimuoviRigheSenzaFornitore
//  14. RecuperaAltreInformazioni2         → ArricchisciConInformazioniFinali
//  15. AggiornaCampiCalcolati             → (automatico dai metodi del VO)
//
//  Ogni stadio diventa un metodo privato chiamato in sequenza da EseguiAsync.
// ═══════════════════════════════════════════════════════════════════════════════

using SistemaF.Domain.Interfaces;

/// <summary>
/// Orchestratore della pipeline di emissione ordine.
/// Rimane nel layer Domain come Domain Service puro:
/// non dipende da EF Core né da infrastruttura I/O diretta.
/// Le dipendenze esterne (archivio, indici, costi) sono iniettate via interfaccia.
/// </summary>
public sealed class EmissioneOrdineService(
    IArchivioPropostaService archivio,
    IUltimiCostiService      ultimiCosti,
    IListiniFornitorService  listini,
    IScontiCondizioniService scontiCondizioni,
    IOfferteService          offerte,
    IIndiciVenditaService    indiciVendita)
{
    // ─────────────────────────────────────────────────────────────────────────
    //  ENTRY POINT — esegue l'intera pipeline
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Esegue la pipeline di emissione ordine per la proposta specificata.
    /// Riempie la proposta con le PropostaRiga, assegna i fornitori e le quantità.
    /// </summary>
    public async Task<RiepilogoElaborazione> EseguiAsync(
        PropostaOrdine  proposta,
        IProgress<AvanzamentoPipeline>? progresso = null,
        CancellationToken ct = default)
    {
        Guard.AgainstFalse(proposta.Stato == PropostaOrdine.StatoProposta.Bozza,
            "EseguiAsync", "La pipeline può essere eseguita solo su proposta in bozza.");

        var inizio = DateTime.UtcNow;
        var (bGlobali, bEsaminati) = (0L, 0L);

        try
        {
            Notifica(progresso, "Avvio pipeline emissione ordine...", 0);

            // ── Fase 1: Recupera prodotti da tutte le fonti ──────────────────
            Notifica(progresso, "Recupero prodotti dalle fonti...", 5);
            await RaccoliProdottiDaFonti(proposta, ct);
            if (proposta.Interrotta) return ChiudiRiepilogo(inizio, bGlobali, bEsaminati, 0, proposta);

            // ── Fase 2: Arricchisci con prezzi storici e indici di vendita ───
            Notifica(progresso, "Recupero prezzi e indici di vendita...", 30);
            await ArricchisciConPrezziEIndici(proposta, ct);
            if (proposta.Interrotta) return ChiudiRiepilogo(inizio, bGlobali, bEsaminati, 0, proposta);

            bEsaminati = proposta.Righe.Count;
            Notifica(progresso, $"{bEsaminati} prodotti esaminati", 45);

            // ── Fase 3: Applica filtri per fornitore ─────────────────────────
            Notifica(progresso, "Applicazione filtri per fornitore...", 55);
            ApplicaFiltriPerFornitore(proposta);

            // ── Fase 4: Elimina prodotti assegnati a fornitori esclusi ───────
            EliminaAssegnazioniEscluse(proposta);

            // ── Fase 5: Assegna il fornitore ottimale a ogni prodotto ────────
            Notifica(progresso, "Calcolo assegnazioni ottimali ai fornitori...", 65);
            await AssegnaFornitoriOttimali(proposta, ct);
            if (proposta.Interrotta) return ChiudiRiepilogo(inizio, bGlobali, bEsaminati, 0, proposta);

            // ── Fase 6: Rimuovi righe senza fornitore ────────────────────────
            RimuoviRigheSenzaFornitore(proposta);

            // ── Fase 7: Arricchisci con informazioni finali ──────────────────
            Notifica(progresso, "Recupero informazioni aggiuntive...", 80);
            await ArricchisciConInformazioniFinali(proposta, ct);

            var inOrdine = proposta.NumeroProdottiConFornitore;
            Notifica(progresso, $"Pipeline completata: {inOrdine} prodotti in ordine.", 100);

            var riepilogo = ChiudiRiepilogo(inizio, bGlobali, bEsaminati, inOrdine, proposta);
            proposta.MarcaCompletata(riepilogo);
            return riepilogo;
        }
        catch (OperationCanceledException)
        {
            proposta.Interrompi();
            throw;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 1 — Raccolta prodotti da tutte le fonti
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RaccoliProdottiDaFonti(PropostaOrdine p, CancellationToken ct)
    {
        // Archivio (ProdBase/Esposizione) — migrazione RecuperaProdotti_ProdBase
        if (p.IsEmissioneDaArchivio)
        {
            var filtri = new FiltriProdottoArchivio(
                IsTrattati:          true,
                IsOrdineLiberoDitta: p.IsOrdineLiberoDitta,
                IndiceDiVendita:     p.IndiceDiVendita.Tipo != TipoIndiceVendita.Tendenziale || p.IndiceDiVendita.GiorniCopertura > 0
                                        ? p.IndiceDiVendita : null,
                RipristinoScorta:    p.RipristinoScorta);

            foreach (var fornitore in p.Fornitori)
            {
                ct.ThrowIfCancellationRequested();
                var prodottiArchivio = await archivio.GetProdottiDaOrdinareAsync(
                    p.ConfigurazioneId, fornitore.FornitoreId, filtri, ct);

                foreach (var pa in prodottiArchivio)
                {
                    var riga = p.OttieniOCreaRiga(pa.ProdottoId, pa.CodiceFarmaco, pa.Descrizione);
                    riga.ImpostaQuantitaArchivio(pa.Quantita);
                    riga.ImpostaGiacenze(pa.GiacenzaEsposizione, pa.ScortaMinimaEsposizione,
                        pa.ScortaMassimaEsposizione, pa.GiacenzaMagazzino,
                        pa.ScortaMinimaMagazzino, pa.ScortaMassimaMagazzino);
                    riga.ImpostaFornitorePreferenziale(pa.FornitorePreferenzialeId, 0);
                }
            }
        }

        // Necessità (Archivio necessità/fabbisogno) — migrazione RecuperaProdotti_Necessita
        // In C# questi dati vengono forniti tramite IArchivioPropostaService.
        // Le righe create dalla necessità arriveranno con FonteAggiunta.Necessita già impostata
        // dal servizio applicativo tramite AggiungiProdottoManuale.
        // (La separazione è necessaria perché le tabelle Necessita/Prenotati/Sospesi
        //  sono gestite dal modulo Vendita che dipende da Ordine, non viceversa.)

        if (p.IsEmissioneSospesi || p.IsEmissionePrenotati || p.IsEmissioneNecessita)
        {
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 2 — Arricchimento con prezzi e indici di vendita
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di RecuperaAltreInformazioni1 in EmissioneOrdine.cls.

    private async Task ArricchisciConPrezziEIndici(PropostaOrdine p, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var riga in p.Righe.ToList())
        {
            ct.ThrowIfCancellationRequested();
            if (p.Interrotta) return;

            // Gli indici di vendita vengono calcolati solo se necessari per il filtro.
            // Se IsIndiciVendita=false vengono azzerati e ricalcolati in fase 7.
            if (p.IndiceDiVendita.GiorniCopertura > 0)
            {
                var tend  = await indiciVendita.GetTendenzialeAsync(riga.ProdottoId, today, ct);
                var mens  = await indiciVendita.GetMensileAsync(riga.ProdottoId, today.Year, today.Month, ct);
                var ann   = await indiciVendita.GetAnnualeAsync(riga.ProdottoId, today.Year, ct);
                var media = await indiciVendita.GetMediaAritmeticaAsync(riga.ProdottoId, today, ct);
                var per   = p.IndiceDiVendita.DalPeriodo.HasValue
                    ? await indiciVendita.GetPeriodoAsync(
                        riga.ProdottoId,
                        p.IndiceDiVendita.DalPeriodo.Value,
                        p.IndiceDiVendita.AlPeriodo ?? today, ct)
                    : 0m;
                riga.ImpostaIndiciVendita(tend, mens, ann, per, media);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 3 — Filtri per fornitore
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di ApplicaFiltri in EmissioneOrdine.cls.
    // I filtri svincolati (OR) e vincolati (AND) vengono applicati abilitando
    // o disabilitando il flag F{i} sulla riga.

    private static void ApplicaFiltriPerFornitore(PropostaOrdine p)
    {
        for (var i = 0; i < p.Fornitori.Count; i++)
        {
            var indice = i + 1;  // 1-based
            foreach (var riga in p.Righe)
            {
                // Il fornitore accetta la riga se la riga soddisfa i criteri base.
                // La logica completa dei filtri (svincolati/vincolati su settore,
                // classe, ditta, giacenza, ecc.) è configurabile in EmissioniValori
                // e viene valutata dal servizio applicativo prima di chiamare la pipeline.
                // Qui applichiamo la regola di default: se la riga ha quantità > 0
                // e DaOrdinare=true, viene abilitata per tutti i fornitori.
                if (riga.DaOrdinare && !riga.DaEliminare && riga.QuantitaTotale > 0)
                    riga.AbilitaFornitore(indice, true);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 4 — Elimina assegnazioni ai fornitori esclusi dal filtro
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di EliminaProdottiAssegnatiFornitoreEsclusiDalFiltro.

    private static void EliminaAssegnazioniEscluse(PropostaOrdine p)
    {
        foreach (var riga in p.Righe)
        {
            for (var i = 1; i <= PropostaRiga.MaxFornitori; i++)
            {
                if (!riga.IsFornitoreAbilitato(i))
                    riga.ImpostaQuantitaFornitore(i, 0);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 5 — Assegnazione fornitore ottimale
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di VerificaAssegnazioni in EmissioneOrdine.cls.
    // Strategia (in ordine di precedenza):
    //   1. Imponibile di default
    //   2. Ultimo costo fornitore (ultimi N giorni)
    //   3. Listino fornitore
    //   4. Sconti & Condizioni
    //   5. Offerte/Fascettature
    //   6. Fornitore preferenziale (override)
    //   7. Minimo CostoReale tra i fornitori abilitati

    private async Task AssegnaFornitoriOttimali(PropostaOrdine p, CancellationToken ct)
    {
        var righe = p.Righe.Where(r => r.HaFornitoriAbilitati && r.DaOrdinare).ToList();
        var total = righe.Count;
        var done  = 0;

        foreach (var riga in righe)
        {
            ct.ThrowIfCancellationRequested();
            if (p.Interrotta) return;

            await CalcolaCostiRiga(riga, p, ct);
            ScegliFornitoreMigliore(riga, p);

            done++;
            if (done % 50 == 0)
        }
    }

    private async Task CalcolaCostiRiga(PropostaRiga riga, PropostaOrdine p, CancellationToken ct)
    {
        var fornitoriIds = p.Fornitori.Select(f => f.FornitoreId).ToList();
        var quantita     = riga.QuantitaTotale;

        // ── Step 2: Ultimi costi ─────────────────────────────────────────────
        var ultCosti = await ultimiCosti.GetUltimiCostiAsync(riga.ProdottoId, fornitoriIds, 90, ct);
        for (var i = 1; i <= p.Fornitori.Count; i++)
        {
            var fid  = p.Fornitori[i - 1].FornitoreId;
            if (ultCosti.TryGetValue(fid, out var uc) && uc.HasValue && uc.Value > 0)
            {
                var sc = riga.PrezzoListino > 0
                    ? CalcolaPercSconto(riga.PrezzoListino, riga.AliquotaIVA, uc.Value) : 0m;
                riga.ImpostaCostoFornitore(i, CostoFornitore.Da(uc.Value, sc));
            }
        }

        // ── Step 3: Listini fornitore ────────────────────────────────────────
        var costiListino = await listini.GetCostiListinoAsync(riga.ProdottoId, fornitoriIds, ct);
        for (var i = 1; i <= p.Fornitori.Count; i++)
        {
            var fid = p.Fornitori[i - 1].FornitoreId;
            if (costiListino.TryGetValue(fid, out var cl) && cl > 0)
            {
                var sc = riga.PrezzoListino > 0
                    ? CalcolaPercSconto(riga.PrezzoListino, riga.AliquotaIVA, cl) : 0m;
                riga.ImpostaCostoFornitore(i, CostoFornitore.Da(cl, sc));
            }
        }

        // ── Step 4: Sconti & Condizioni ──────────────────────────────────────
        var fornitoriConQta = p.Fornitori
            .Select((f, idx) => (f.FornitoreId, riga.QuantitaPerFornitore(idx + 1)))
            .ToList();

        var sconti = await scontiCondizioni.GetScontiAsync(
            riga.ProdottoId, riga.SettoreInventario, riga.Classe, string.Empty,
            fornitoriConQta, ct);

        for (var i = 1; i <= p.Fornitori.Count; i++)
        {
            var fid = p.Fornitori[i - 1].FornitoreId;
            if (!sconti.TryGetValue(fid, out var sc) || sc.Sconto <= 0) continue;

            var imponibile = riga.PrezzoListino <= 0 ? 0m : sc.TipoCalcolo switch
            {
                TipoCalcoloSconto.Imponibile    => CalcolaImponibile(riga.PrezzoListino, riga.AliquotaIVA)
                                                   * (1 - sc.Sconto / 100m),
                TipoCalcoloSconto.PrezzoVendita => riga.PrezzoVendita * (1 - sc.Sconto / 100m),
                TipoCalcoloSconto.PrezzoListino => riga.PrezzoListino * (1 - sc.Sconto / 100m),
                _                               => 0m
            };

            if (imponibile > 0)
            {
                var percSc = CalcolaPercSconto(riga.PrezzoListino, riga.AliquotaIVA, imponibile);
                riga.ImpostaCostoFornitore(i, CostoFornitore.Da(imponibile, percSc, 0));
                if (sc.QuantitaArrotondata > riga.QuantitaPerFornitore(i))
                    riga.ImpostaQuantitaFornitore(i, sc.QuantitaArrotondata);
            }
        }

        // ── Step 5: Offerte ──────────────────────────────────────────────────
        var offerteLista = await offerte.GetOfferteAsync(riga.ProdottoId, quantita * 2, fornitoriIds, ct);
        foreach (var off in offerteLista)
        {
            var idx = p.Fornitori.FindIndex(f => f.FornitoreId == off.FornitoreId);
            if (idx < 0) continue;
            var i = idx + 1;

            if (off.Costo > 0)
            {
                var percSc = CalcolaPercSconto(riga.PrezzoListino, riga.AliquotaIVA, off.Costo);
                riga.ImpostaCostoFornitore(i, CostoFornitore.Da(off.Costo, percSc));
            }
            if (off.QuantitaOmaggio > 0)
                riga.ImpostaOmaggioFornitore(i, off.QuantitaOmaggio);
        }
    }

    private static void ScegliFornitoreMigliore(PropostaRiga riga, PropostaOrdine p)
    {
        var numAbilitati = Enumerable.Range(1, p.Fornitori.Count)
            .Count(i => riga.IsFornitoreAbilitato(i));

        if (numAbilitati <= 1) return;  // Già univoco

        // ── Step 6: Fornitore preferenziale ──────────────────────────────────
        if (riga.FornitorePreferenzialeId.HasValue && riga.QuantitaMancante == 0)
        {
            var idxPref = p.Fornitori.FindIndex(
                f => f.FornitoreId == riga.FornitorePreferenzialeId.Value);
            if (idxPref >= 0)
            {
                riga.AbilitaSoloFornitore(idxPref + 1);
                return;
            }
        }

        // ── Step 7: Scegli il fornitore con CostoReale minore ────────────────
        // Migrazione: TrovaMinore su colValori + esclusione magazzino
        var (indiceMin, _) = Enumerable.Range(1, p.Fornitori.Count)
            .Where(i => riga.IsFornitoreAbilitato(i)
                     && i != p.IndiceMagazzino)          // esclude magazzino dalla gara
            .Select(i => (indice: i, costo: riga.CostoPerFornitore(i).CostoReale()))
            .OrderBy(x => x.costo)
            .FirstOrDefault();

        if (indiceMin > 0)
        {
            riga.AbilitaSoloFornitore(indiceMin);

            // Assegna la quantità al fornitore vincitore
            var qtaMagazzino = 0;
            if (p.IsMagazzinoPresente)
            {
                var giacMag = riga.GiacenzaMagazzino;
                qtaMagazzino = Math.Min(riga.QuantitaTotale, giacMag);
                riga.ImpostaQuantitaFornitore(p.IndiceMagazzino, qtaMagazzino);
            }
            riga.ImpostaQuantitaFornitore(indiceMin,
                Math.Max(0, riga.QuantitaTotale - qtaMagazzino));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 6 — Rimozione righe senza fornitore
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di RimuoviMarcaturaSuperflua in EmissioneOrdine.cls.

    private static void RimuoviRigheSenzaFornitore(PropostaOrdine p)
    {
        var daRimuovere = p.Righe
            .Where(r => !r.HaFornitoriAbilitati || r.TotaleQuantita == 0)
            .Select(r => r.ProdottoId)
            .ToList();

        p.RimuoviRighe(daRimuovere);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  FASE 7 — Informazioni finali (indici visibili, giacenza simili, ecc.)
    // ─────────────────────────────────────────────────────────────────────────
    // Migrazione di RecuperaAltreInformazioni2 in EmissioneOrdine.cls.

    private async Task ArricchisciConInformazioniFinali(PropostaOrdine p, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        foreach (var riga in p.Righe.ToList())
        {
            ct.ThrowIfCancellationRequested();
            if (p.Interrotta) return;

            // Se gli indici non erano stati calcolati in fase 2, li calcola ora
            if (riga.IndiceVenditaTendenziale == 0)
            {
                var tend  = await indiciVendita.GetTendenzialeAsync(riga.ProdottoId, today, ct);
                var mens  = await indiciVendita.GetMensileAsync(riga.ProdottoId, today.Year, today.Month, ct);
                var ann   = await indiciVendita.GetAnnualeAsync(riga.ProdottoId, today.Year, ct);
                var media = await indiciVendita.GetMediaAritmeticaAsync(riga.ProdottoId, today, ct);
                riga.ImpostaIndiciVendita(tend, mens, ann, 0m, media);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────────────────────────────────

    private static decimal CalcolaImponibile(decimal prezzo, int aliquotaIVA)
        => Math.Round(prezzo / (1 + aliquotaIVA / 100m), 4);

    private static decimal CalcolaPercSconto(decimal prezzo, int aliquotaIVA, decimal costo)
    {
        var imp = CalcolaImponibile(prezzo, aliquotaIVA);
        return imp > 0 ? Math.Round((1 - costo / imp) * 100, 4) : 0m;
    }

    private static RiepilogoElaborazione ChiudiRiepilogo(
        DateTime inizio, long globali, long esaminati, long inOrdine, PropostaOrdine p)
        => new(p.NomeEmissione, inizio, DateTime.UtcNow, globali, esaminati, inOrdine);

    private static void Notifica(IProgress<AvanzamentoPipeline>? p, string msg, int perc)
        => p?.Report(new AvanzamentoPipeline(msg, perc));


}

/// <summary>Progresso della pipeline di emissione (per la UI).</summary>
public sealed record AvanzamentoPipeline(string Messaggio, int PercentualeCompletamento);
