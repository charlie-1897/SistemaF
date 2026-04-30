namespace SistemaF.Domain.Entities.Ordine;

// ═══════════════════════════════════════════════════════════════════════════════
//  ENUMERAZIONI — Modulo Ordine / CSFOrdEmissione
//
//  Sorgenti VB6 di riferimento:
//    Definizioni.bas            → eTipoStatoOrdine, eTipoFornitore
//    EmissioneOrdine.cls        → eFonteAggiunta, eTipoPrioritaOrdinamento
//    InterfacciaEmissione.cls   → eArchivioAggiunta
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Stato del ciclo di vita di un ordine.
/// Migrazione di eTipoStatoOrdine in Definizioni.bas.
/// </summary>
public enum StatoOrdine
{
    Occupato              = 0,   // SO_Occupato — proposta in lavorazione
    Salvato               = 1,   // SO_Salvato
    Emesso                = 2,   // SO_Emesso — ordine confermato
    Trasmesso             = 3,   // SO_Trasmesso — inviato al fornitore
    Ricevuto              = 4,   // SO_Ricevuto — merce arrivata
    RicaricatoParzialmente = 5,  // SO_RicaricatoParzialmente
    Ricaricato            = 6,   // SO_Ricaricato — completamente scaricato a magazzino
    FatturatoParzialmente = 7,   // SO_FatturatoParzialmente
    Fatturato             = 8    // SO_Fatturato
}

/// <summary>
/// Tipo anagrafico del fornitore dell'ordine.
/// Migrazione di eTipoFornitore in Definizioni.bas.
/// </summary>
public enum TipoFornitore
{
    Grossista   = 0,   // TF_Grossista — distributore intermedio
    Ditta       = 1,   // TF_Ditta — produttore/titolare linea
    Magazzino   = 2,   // TF_Magazzino — magazzino interno (no acquisto)
    Web         = 3,   // TF_Web — piattaforma web
    Sconosciuto = 4    // TF_Sconosciuto
}

/// <summary>
/// Fonte da cui è stato aggiunto il prodotto alla proposta.
/// Migrazione di eFonteAggiunta in EmissioneOrdine.cls.
/// Determina quale campo quantità viene incrementato nella ValutazioneOrdine2.
/// </summary>
public enum FonteAggiunta
{
    Necessita    = 0,  // TFA_Necessita    — da calcolo scorte/fabbisogno
    Prenotati    = 1,  // TFA_Prenotati    — da ArchivioPrenotati
    Mancanti     = 2,  // TFA_Mancanti     — prodotti mancanti da ricevimento
    Sospesi      = 3,  // TFA_Sospesi      — da ArchivioSospesi
    Parcheggiati = 4   // TFA_Parcheggiati — prodotti parcheggiati (sospesi temporaneamente)
}

/// <summary>
/// Tipo di ripristino scorte usato nella pipeline di emissione.
/// Migrazione di tRipristinoScorta.TipoRipristino in EmissioneOrdine.cls.
/// Determina la formula CalcolaPerRipristinoScorte (4 rami del Select Case).
/// </summary>
public enum TipoRipristinoScorta
{
    ScortaMinimaEsposizione  = 0,  // Ordina fino a ripristinare la scorta min esposizione
    ScortaMassimaEsposizione = 1,  // Ordina fino a ripristinare la scorta max esposizione
    ScortaMinimaMagazzino    = 2,  // Ordina fino a ripristinare la scorta min magazzino
    ScortaMassimaMagazzino   = 3   // Ordina fino a ripristinare la scorta max magazzino
}

/// <summary>
/// Tipo di indice di vendita usato per calcolare la quantità da ordinare.
/// Migrazione di mIndiceDiVendita.TipoIndiceVendita in EmissioneOrdine.cls
/// (5 case nel Select Case di CalcolaPerIndiciDiVendita).
/// </summary>
public enum TipoIndiceVendita
{
    Tendenziale      = 0,  // rolling 7 giorni
    AnnualeCorrente  = 1,  // venduto anno corrente
    MesePrecedente   = 2,  // venduto mese scorso
    Periodo          = 3,  // venduto in un range di date
    MediaAritmetica  = 4   // media aritmetica storica
}

/// <summary>
/// Priorità di ordinamento dei prodotti nella proposta.
/// Migrazione di eTipoPrioritaOrdinamento in EmissioneOrdine.cls.
/// Usata quando cParametri.Emissione.Ordinamento = 4.
/// </summary>
public enum PrioritaOrdinamentoRiga
{
    FarmacoNormale     = 0,  // TPO_NormaleFarmaco    — farmaco settore A non OTC/SOP
    OtcSopNormale      = 1,  // TPO_NormaleOtcSop     — farmaco OTC/SOP
    ParafarmacoNormale = 2,  // TPO_NormaleParafarmaco — settore diverso da A
    Aggiunto           = 3,  // TPO_Aggiunto          — aggiunto manualmente dall'operatore
    SospesoPrenotato   = 4,  // TPO_SospesoPrenotato
    Mancante           = 5   // TPO_Mancante
}

/// <summary>
/// Tipo di calcolo sconto/condizioni da sconti&condizioni fornitore.
/// Migrazione di eTipoCalcolo usato in RecuperaScontiCondizioniFornitore.
/// </summary>
public enum TipoCalcoloSconto
{
    Imponibile   = 0,  // "I" — % su imponibile del prezzo listino
    PrezzoVendita = 1, // "P" — % sul prezzo al pubblico
    PrezzoListino = 2  // "L" — % sul prezzo di listino lordo
}
