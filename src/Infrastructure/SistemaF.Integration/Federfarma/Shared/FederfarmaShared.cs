using System.Security.Cryptography;
using System.Text;

namespace SistemaF.Integration.Federfarma.Shared;

// ═══════════════════════════════════════════════════════════════════════════════
//  SHARED — Federfarma Integration
//
//  Sorgenti VB.NET di riferimento:
//    CSFWebDpcLombardia.vb   — getWEBDPC_Distinta (WS-Security UsernameToken)
//    CSFWebCareLombardia.vb  — getWEBCARE_Competenza (WS-Security UsernameToken)
//
//  Nel VB.NET originale l'autenticazione usava:
//    Microsoft.Web.Services2.Security.Tokens.UsernameToken
//    PasswordOption.SendPlainText
//    myContext.Security.Tokens.Add(Token)
//    myContext.Security.MustUnderstand = False
//
//  In .NET 8 sostituiamo con HttpClient + SOAP envelope manuale
//  che include il WS-Security header con UsernameToken PlainText.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Credenziali di accesso Federfarma Lombardia.
/// Migrazione dei parametri sUserName/sPIN in entrambe le classi VB.NET.
/// </summary>
public sealed record FederfarmaCredenziali(
    string Username,   // sUserName — es. "SO00073"
    string Pin)        // sPIN — es. "andrea58"
{
    public static FederfarmaCredenziali Da(string username, string pin)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username obbligatorio.", nameof(username));
        if (string.IsNullOrWhiteSpace(pin))      throw new ArgumentException("PIN obbligatorio.", nameof(pin));
        return new FederfarmaCredenziali(username.Trim(), pin.Trim());
    }
}

/// <summary>
/// Parametri della richiesta FUR (Farmaco Uso Ricorrente) verso Federfarma.
/// Corrisponde alla struttura FurRequest in entrambi i WSDL.
/// </summary>
public sealed record FederfarmaRichiestaFur(
    string CodiceAsl,           // sCodiceAsl — es. "030313"
    string CodiceFarmaciaAsl,   // sCodiceFarmaciaASL — es. "00073"
    string Mese,                // sMese — "01".."12"
    string Anno)                // sAnno — "2024"
{
    public static FederfarmaRichiestaFur Da(
        string codiceAsl, string codiceFarmacia, int mese, int anno)
    {
        if (string.IsNullOrWhiteSpace(codiceAsl))       throw new ArgumentException("Codice ASL obbligatorio.");
        if (string.IsNullOrWhiteSpace(codiceFarmacia))  throw new ArgumentException("Codice farmacia obbligatorio.");
        if (mese < 1 || mese > 12)                      throw new ArgumentOutOfRangeException(nameof(mese), "Mese deve essere 1..12.");
        if (anno < 2000 || anno > 2099)                 throw new ArgumentOutOfRangeException(nameof(anno));
        return new FederfarmaRichiestaFur(
            codiceAsl.Trim(),
            codiceFarmacia.Trim(),
            mese.ToString("D2"),
            anno.ToString());
    }
}

/// <summary>
/// Costruisce l'envelope SOAP 1.1 con WS-Security UsernameToken PlainText.
/// Migrazione del blocco Microsoft.Web.Services2 in entrambe le classi VB.NET.
///
/// Struttura dell'header generato (equivalente a WSE2 PasswordOption.SendPlainText):
///   &lt;wsse:Security&gt;
///     &lt;wsse:UsernameToken&gt;
///       &lt;wsse:Username&gt;...&lt;/wsse:Username&gt;
///       &lt;wsse:Password Type="...PasswordText"&gt;...&lt;/wsse:Password&gt;
///       &lt;wsu:Nonce&gt;...&lt;/wsu:Nonce&gt;
///       &lt;wsu:Created&gt;...&lt;/wsu:Created&gt;
///     &lt;/wsse:UsernameToken&gt;
///   &lt;/wsse:Security&gt;
/// </summary>
public static class SoapEnvelopeBuilder
{
    private const string WsseNs   = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string WsuNs    = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string PwdType  = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText";

    /// <summary>
    /// Costruisce la SOAP envelope per la chiamata GetFUR del servizio DPC.
    /// Migrazione di getWEBDPC_Distinta in CSFWebDpcLombardia.vb.
    /// </summary>
    public static string BuildDpcGetFurEnvelope(
        FederfarmaCredenziali cred,
        FederfarmaRichiestaFur req)
    {
        var (nonce, created) = GeneraCredenziali();
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope
                xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                xmlns:wsse="{WsseNs}"
                xmlns:wsu="{WsuNs}"
                xmlns:tns="https://webdpc.federfarma.lombardia.it/DpcServices/FurService">
              <soap:Header>
                <wsse:Security soap:mustUnderstand="0">
                  <wsse:UsernameToken>
                    <wsse:Username>{Escape(cred.Username)}</wsse:Username>
                    <wsse:Password Type="{PwdType}">{Escape(cred.Pin)}</wsse:Password>
                    <wsu:Nonce>{nonce}</wsu:Nonce>
                    <wsu:Created>{created}</wsu:Created>
                  </wsse:UsernameToken>
                </wsse:Security>
              </soap:Header>
              <soap:Body>
                <tns:GetFUR>
                  <tns:request>
                    <tns:CodiceAsl>{Escape(req.CodiceAsl)}</tns:CodiceAsl>
                    <tns:CodiceFarmaciaASL>{Escape(req.CodiceFarmaciaAsl)}</tns:CodiceFarmaciaASL>
                    <tns:Mese>{Escape(req.Mese)}</tns:Mese>
                    <tns:Anno>{Escape(req.Anno)}</tns:Anno>
                  </tns:request>
                </tns:GetFUR>
              </soap:Body>
            </soap:Envelope>
            """;
    }

    /// <summary>
    /// Costruisce la SOAP envelope per la chiamata GetCompetenza del servizio WebCare.
    /// Migrazione di getWEBCARE_Competenza in CSFWebCareLombardia.vb.
    /// Nota: il VB.NET originale passava anche username/PIN come parametri del body
    ///       oltre che nell'header WS-Security (per compatibilità ASL legacy).
    /// </summary>
    public static string BuildWebCareGetCompetenzaEnvelope(
        FederfarmaCredenziali cred,
        FederfarmaRichiestaFur req)
    {
        var (nonce, created) = GeneraCredenziali();
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soap:Envelope
                xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"
                xmlns:wsse="{WsseNs}"
                xmlns:wsu="{WsuNs}"
                xmlns:tns="http://tempuri.org/">
              <soap:Header>
                <wsse:Security soap:mustUnderstand="0">
                  <wsse:UsernameToken>
                    <wsse:Username>{Escape(cred.Username)}</wsse:Username>
                    <wsse:Password Type="{PwdType}">{Escape(cred.Pin)}</wsse:Password>
                    <wsu:Nonce>{nonce}</wsu:Nonce>
                    <wsu:Created>{created}</wsu:Created>
                  </wsse:UsernameToken>
                </wsse:Security>
              </soap:Header>
              <soap:Body>
                <tns:GetCompetenza>
                  <tns:login>{Escape(cred.Username)}</tns:login>
                  <tns:PIN>{Escape(cred.Pin)}</tns:PIN>
                  <tns:request>
                    <tns:codiceAsl>{Escape(req.CodiceAsl)}</tns:codiceAsl>
                    <tns:codiceFarmacia>{Escape(req.CodiceFarmaciaAsl)}</tns:codiceFarmacia>
                    <tns:meseCompetenza>{Escape(req.Mese)}</tns:meseCompetenza>
                    <tns:annoCompetenza>{Escape(req.Anno)}</tns:annoCompetenza>
                  </tns:request>
                </tns:GetCompetenza>
              </soap:Body>
            </soap:Envelope>
            """;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (string nonce, string created) GeneraCredenziali()
    {
        var nonceBytes = RandomNumberGenerator.GetBytes(16);
        var nonce      = Convert.ToBase64String(nonceBytes);
        var created    = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        return (nonce, created);
    }

    private static string Escape(string s) => s
        .Replace("&",  "&amp;")
        .Replace("<",  "&lt;")
        .Replace(">",  "&gt;")
        .Replace("\"", "&quot;")
        .Replace("'",  "&apos;");
}

/// <summary>
/// Configurazione centralizzata degli endpoint Federfarma.
/// Legge i valori da appsettings.json (sezione "Federfarma").
/// </summary>
public sealed class FederfarmaConfiguration
{
    public string DpcEndpointUrl      { get; init; } =
        "https://webdpc.federfarma.lombardia.it/DpcServices/FurService.svc/base";

    public string DpcSoapAction       { get; init; } =
        "https://webdpc.federfarma.lombardia.it/DpcServices/FurService/IFurService/GetFUR";

    /// <summary>
    /// URL base WebCare: viene completato con il nome ASL.
    /// Il VB.NET originale: new ServiceWse(nomeAsl) — il nome ASL veniva usato
    /// per selezionare l'endpoint corretto.
    /// </summary>
    public string WebCareEndpointBase { get; init; } =
        "https://webcare.federfarma.lombardia.it/{nomeAsl}/Service.svc";

    public string WebCareSoapAction   { get; init; } =
        "http://tempuri.org/IService/GetCompetenza";

    public int TimeoutSecondi         { get; init; } = 100;   // 100s = default VB6 (100000ms)

    public string WebCareEndpointUrl(string nomeAsl) =>
        WebCareEndpointBase.Replace("{nomeAsl}", nomeAsl.ToLowerInvariant().Trim());
}

/// <summary>
/// Eccezione SOAP da Federfarma (migrazione del catch SoapException in VB.NET).
/// Nel VB.NET: ex.Detail → DisplayNode → Code + Message.
/// </summary>
public sealed class FederfarmaFaultException(string codice, string messaggio)
    : Exception($"[{codice}] {messaggio}")
{
    public string Codice    { get; } = codice;
    public string Dettaglio { get; } = messaggio;
}
