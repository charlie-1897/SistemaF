using SistemaF.Integration.Federfarma.Dpc.Models;
using SistemaF.Integration.Federfarma.Shared;
using SistemaF.Integration.Federfarma.WebCare.Models;

namespace SistemaF.Integration.Federfarma;

// ═══════════════════════════════════════════════════════════════════════════════
//  INTERFACCE — Federfarma Integration
//
//  Migrazione delle firme pubbliche di:
//    CSFWebDpcLombardia  → IFederfarmaWebDpcClient
//    CSFWebCareLombardia → IFederfarmaWebCareClient
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Client per il servizio DPC (Distribuzione Per Conto) Federfarma Lombardia.
///
/// Migrazione di CSFWebDpcLombardia con i suoi due metodi pubblici:
///   - getXMLDPC_Distinta  → ParseXmlAsync   (parser locale)
///   - getWEBDPC_Distinta  → GetDistintaAsync (chiamata WS)
/// </summary>
public interface IFederfarmaWebDpcClient
{
    /// <summary>
    /// Recupera la distinta DPC dal WebService Federfarma.
    /// Migrazione di getWEBDPC_Distinta: FurDpcServiceWse.GetFUR(FurRequest).
    /// </summary>
    /// <param name="credenziali">Username + PIN per WS-Security.</param>
    /// <param name="richiesta">ASL, farmacia, mese, anno.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>DistintaDpc popolata, o eccezione se il WS risponde con fault.</returns>
    Task<DistintaDpc> GetDistintaAsync(
        FederfarmaCredenziali  credenziali,
        FederfarmaRichiestaFur richiesta,
        CancellationToken      ct = default);

    /// <summary>
    /// Legge la distinta DPC da un file XML locale.
    /// Migrazione di getXMLDPC_Distinta: XmlTextReader su sPathFileXML.
    /// Utile per test e per l'import da file pre-scaricati.
    /// </summary>
    Task<DistintaDpc> ParseXmlAsync(
        string            pathFileXml,
        CancellationToken ct = default);
}

/// <summary>
/// Client per il servizio WebCare (FUR) Federfarma Lombardia.
///
/// Migrazione di CSFWebCareLombardia con i suoi due metodi pubblici:
///   - getXMLCARE_Competenza  → ParseXmlAsync    (parser locale)
///   - getWEBCARE_Competenza  → GetCompetenzaAsync (chiamata WS)
/// </summary>
public interface IFederfarmaWebCareClient
{
    /// <summary>
    /// Recupera la competenza FUR dal WebService Federfarma WebCare.
    /// Migrazione di getWEBCARE_Competenza: ServiceWse(nomeAsl).GetCompetenza(...).
    /// </summary>
    /// <param name="credenziali">Username + PIN per WS-Security.</param>
    /// <param name="richiesta">ASL, farmacia, mese, anno.</param>
    /// <param name="nomeAsl">Nome ASL usato per selezionare l'endpoint (es. "ASST_Lecco").</param>
    /// <param name="ct">Cancellation token.</param>
    Task<CompetenzaWebCare> GetCompetenzaAsync(
        FederfarmaCredenziali  credenziali,
        FederfarmaRichiestaFur richiesta,
        string                 nomeAsl,
        CancellationToken      ct = default);

    /// <summary>
    /// Legge la competenza WebCare da un file XML locale.
    /// Migrazione di getXMLCARE_Competenza.
    /// Nota: nel VB.NET l'XML parsing si fermava al tag &lt;RiepiloghiIVA&gt;
    /// (leggeva solo i dati di testata). Questa implementazione legge tutto.
    /// </summary>
    Task<CompetenzaWebCare> ParseXmlAsync(
        string            pathFileXml,
        CancellationToken ct = default);
}
