namespace SistemaF.Domain.Entities.Prodotto;

// ═══════════════════════════════════════════════════════════════════════════════
//  GIACENZA MAGAZZINO — Value Object
//
//  Migrazione del tipo tGiacenzaProdotto in GiacenzaProdotto.cls:
//    Public Type tGiacenzaProdotto
//        Giacenza As Long
//        ScortaMinima As Long
//        ScortaMassima As Long
//    End Type
//
//  E degli enum:
//    eTipoMagazzino     → TipoDepositoMagazzino
//    eTipoModalitaAggiunta → ModalitaVariazioneGiacenza
//    TipoAzione (Rettifiche.cls) → TipoAzioneRettifica
//    TipoCosa (Rettifiche.cls)   → TipoCosaRettifica
//    TipoModulo (Rettifiche.cls) → TipoModuloRettifica
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Giacenza di un prodotto in un deposito (esposizione o magazzino retro).
/// Immutabile: ogni operazione restituisce una nuova istanza.
/// </summary>
public sealed class GiacenzaMagazzino : ValueObject
{
    public int Giacenza    { get; }
    public int ScortaMinima { get; }
    public int ScortaMassima { get; }

    /// <summary>True se le scorte (min+max) sono state configurate.</summary>
    public bool IsScorteConfigurate => ScortaMinima + ScortaMassima > 0;

    public static readonly GiacenzaMagazzino Zero = new(0, 0, 0);

    private GiacenzaMagazzino(int giacenza, int scortaMin, int scortaMax)
    {
        Giacenza     = giacenza;
        ScortaMinima = scortaMin;
        ScortaMassima = scortaMax;
    }

    public static GiacenzaMagazzino Crea(int giacenza, int scortaMin = 0, int scortaMax = 0)
    {
        Guard.AgainstNegative(giacenza, nameof(giacenza));
        Guard.AgainstNegative(scortaMin, nameof(scortaMin));
        Guard.AgainstNegative(scortaMax, nameof(scortaMax));
        return new GiacenzaMagazzino(giacenza, scortaMin, scortaMax);
    }

    /// <summary>
    /// Applica una variazione di giacenza secondo la modalità indicata.
    /// Migrazione del Select Case TipoModalitaAggiunta in GiacenzaProdotto.cls.
    /// </summary>
    public GiacenzaMagazzino Applica(ModalitaVariazioneGiacenza modalita, int valore)
    {
        var nuova = modalita switch
        {
            ModalitaVariazioneGiacenza.Sostituzione => valore,
            ModalitaVariazioneGiacenza.Aggiunta     => Giacenza + valore,
            ModalitaVariazioneGiacenza.Sottrazione  => Giacenza - valore,
            _ => throw new DomainException($"Modalità variazione sconosciuta: {modalita}.")
        };

        if (nuova < 0)
            throw new BusinessRuleViolationException("GiacenzaNonNegativa",
                $"La giacenza risultante ({nuova}) sarebbe negativa.");

        return new GiacenzaMagazzino(nuova, ScortaMinima, ScortaMassima);
    }

    /// <summary>Restituisce una nuova istanza con le scorte aggiornate.</summary>
    public GiacenzaMagazzino ConScorte(int scortaMin, int scortaMax)
        => new(Giacenza, scortaMin, scortaMax);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Giacenza;
        yield return ScortaMinima;
        yield return ScortaMassima;
    }

    public override string ToString() =>
        $"Giacenza={Giacenza}, Scorte=[{ScortaMinima}..{ScortaMassima}]";
}

// ── Enumerazioni (ex enum VB6 in GiacenzaProdotto.cls e Rettifiche.cls) ──────

/// <summary>
/// Tipo di deposito. Migrazione di eTipoMagazzino in GiacenzaProdotto.cls.
/// </summary>
public enum TipoDepositoMagazzino
{
    Esposizione = 0,   // ex TM_Esposizione — banco vendita
    Magazzino   = 1    // ex TM_Magazzino   — magazzino retro
}

/// <summary>
/// Modalità di variazione della giacenza.
/// Migrazione di eTipoModalitaAggiunta in GiacenzaProdotto.cls.
/// </summary>
public enum ModalitaVariazioneGiacenza
{
    Sostituzione = 0,  // ex TMA_Sostituzione — sostituisce il valore
    Aggiunta     = 1,  // ex TMA_Aggiunta     — aggiunge al valore
    Sottrazione  = 2   // ex TMA_Sottrazione  — sottrae dal valore
}

/// <summary>
/// Tipo di azione nel registro rettifiche.
/// Migrazione di TipoAzione in Rettifiche.cls.
/// </summary>
public enum TipoAzioneRettifica
{
    Aggiunta    = 1,   // ex TA_Aggiunta
    Modifica    = 2,   // ex TA_Modifica
    Eliminazione = 3   // ex TA_Eliminazione
}

/// <summary>
/// Tipo di campo modificato nel registro rettifiche.
/// Migrazione di TipoCosa in Rettifiche.cls — 19 valori originali.
/// </summary>
public enum TipoCosaRettifica
{
    GiacenzaEsposizione       = 1,   // TC_GiacenzaEsposizione
    GiacenzaMagazzino         = 2,   // TC_GiacenzaMagazzino
    ScortaMinimaEsposizione   = 3,   // TC_ScortaMinimaEsposizione
    ScortaMassimaEsposizione  = 4,   // TC_ScortaMassimaEsposizione
    ScortaMinimaMagazzino     = 5,   // TC_ScortaMinimaMagazzino
    ScortaMassimaMagazzino    = 6,   // TC_ScortaMassimaMagazzino
    Costo                     = 7,   // TC_Costo
    PrezzoPubblico            = 8,   // TC_PrezzoPubblico
    PrezzoListino             = 9,   // TC_PrezzoListino
    Prodotto                  = 10,  // TC_Prodotto (eliminazione anagrafica)
    GiacenzaInvendibile       = 11,  // TC_Giacenzainvendibile
    ProgressivoInvendibile    = 12,  // TC_Progressivoinvendibile
    PrezzoFarmacia            = 13,  // TC_PrezzoFarmacia
    Congelato                 = 14,  // TC_Congelato
    StampaEtichette           = 15,  // TC_StampaEtichette
    CampoLibero               = 16,  // TC_CampoLibero
    ProdottoWeb               = 17,  // TC_ProdottoWEB
    EtichetteElettroniche     = 18,  // TC_QtichetteElettroniche
    QuantitaSpuntata          = 19   // TC_QuantitaSpuntata
}

/// <summary>
/// Modulo applicativo che ha generato la rettifica.
/// Migrazione di TipoModulo in Rettifiche.cls.
/// </summary>
public enum TipoModuloRettifica
{
    Magazzino         = 1,   // TM_Magazzino
    OrdineEmissione   = 2,   // TM_OrdineEmissione
    OrdineRicarico    = 3,   // TM_OrdineRicarico
    Vendita           = 4,   // TM_Vendita
    Fatturazione      = 5,   // TM_Fatturazione
    Tariffazione      = 6,   // TM_Tariffazione
    Scadenzario       = 7,   // TM_Scadenzario
    Invendibili       = 8,   // TM_Invendibili
    OrdineFatturazione = 9   // TM_OrdineFatturazione
}
