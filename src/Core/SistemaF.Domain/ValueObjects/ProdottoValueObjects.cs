namespace SistemaF.Domain.ValueObjects;

// ═══════════════════════════════════════════════════════════════════════════════
//  VALUE OBJECTS — Modulo Prodotto
//
//  Nel VB6 tutti questi concetti erano semplici String o Currency:
//    Dim sCodice As String       → CodiceProdotto
//    Dim sEAN As String          → CodiceEAN
//    Dim dPrezzo As Currency     → Prezzo
//    Dim sLotto As String        → CodiceLotto
//  Nulla impediva di confondere i tipi. Qui ogni concetto è un tipo distinto.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Codice ministeriale farmaceutico a 9 cifre.
/// Migrazione di: Public Const CSFMINISTERIALE As Byte = 1 (CSFDichiarazioni.bas)
/// e del campo "Ministeriale As String" usato ovunque nel VB6.
/// </summary>
public sealed class CodiceProdotto : SingleValueObject<string>
{
    private CodiceProdotto(string valore) : base(valore) { }

    public static CodiceProdotto Da(string valore)
    {
        var v = Guard.AgainstNullOrEmpty(valore).PadLeft(9, '0');
        if (v.Length != 9 || !v.All(char.IsDigit))
            throw new DomainException(
                $"Codice farmaco non valido: '{valore}'. Deve essere 9 cifre numeriche.",
                "CODICE_PRODOTTO_INVALIDO");
        return new CodiceProdotto(v);
    }

    /// <summary>Prova a creare senza lanciare eccezione.</summary>
    public static Result<CodiceProdotto> TryCreate(string valore)
    {
        try   { return Result<CodiceProdotto>.Ok(Da(valore)); }
        catch (DomainException ex) { return Result<CodiceProdotto>.Fail(ex); }
    }
}

/// <summary>
/// Codice EAN-13 con validazione check digit.
/// Migrazione di: Public Const CSFCODICEEAN As Byte = 15 (CSFDichiarazioni.bas).
/// </summary>
public sealed class CodiceEAN : SingleValueObject<string>
{
    private CodiceEAN(string valore) : base(valore) { }

    public static CodiceEAN Da(string valore)
    {
        var v = Guard.AgainstNullOrEmpty(valore).Trim();
        if (v.Length != 13 || !v.All(char.IsDigit))
            throw new DomainException(
                $"Codice EAN non valido: '{valore}'. Deve essere 13 cifre.",
                "EAN_INVALIDO");
        if (!ValidaCheckDigit(v))
            throw new DomainException(
                $"Check digit EAN non valido: '{valore}'.",
                "EAN_CHECK_DIGIT_INVALIDO");
        return new CodiceEAN(v);
    }

    public static Result<CodiceEAN> TryCreate(string valore)
    {
        try   { return Result<CodiceEAN>.Ok(Da(valore)); }
        catch (DomainException ex) { return Result<CodiceEAN>.Fail(ex); }
    }

    private static bool ValidaCheckDigit(string ean)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (ean[i] - '0') * (i % 2 == 0 ? 1 : 3);
        return (10 - sum % 10) % 10 == (ean[12] - '0');
    }
}

/// <summary>
/// Prezzo monetario con IVA.
/// Sostituisce i campi Currency del VB6 (attualePubblico, attualeListino).
/// Aliquote IVA ammesse in farmacia: 4%, 10%, 22%.
/// </summary>
public sealed class Prezzo : ValueObject
{
    public decimal Importo    { get; }
    public int     AliquotaIVA { get; }   // percentuale intera: 4, 10 oppure 22

    private static readonly HashSet<int> _aliquoteAmmesse = [4, 10, 22];

    private Prezzo(decimal importo, int aliquota)
    {
        Importo     = importo;
        AliquotaIVA = aliquota;
    }

    public static Prezzo Di(decimal importo, int aliquotaIVA = 10)
    {
        Guard.AgainstNegative(importo, nameof(importo));
        if (!_aliquoteAmmesse.Contains(aliquotaIVA))
            throw new DomainException(
                $"Aliquota IVA {aliquotaIVA}% non ammessa. Valori: 4, 10, 22.",
                "ALIQUOTA_IVA_INVALIDA");
        return new Prezzo(Math.Round(importo, 2), aliquotaIVA);
    }

    public static Prezzo Zero(int aliquotaIVA = 10) => Di(0m, aliquotaIVA);

    /// <summary>Importo comprensivo di IVA.</summary>
    public decimal ImportoIVAInclusa => Math.Round(Importo * (1 + AliquotaIVA / 100m), 2);

    /// <summary>Solo la quota IVA.</summary>
    public decimal QuotaIVA => ImportoIVAInclusa - Importo;

    /// <summary>Applica uno sconto percentuale e restituisce il nuovo Prezzo.</summary>
    public Prezzo ApplicaSconto(decimal percentuale)
    {
        Guard.AgainstOutOfRange(percentuale, 0m, 100m, nameof(percentuale));
        return Di(Math.Round(Importo * (1 - percentuale / 100m), 2), AliquotaIVA);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Importo;
        yield return AliquotaIVA;
    }

    public override string ToString() => $"{Importo:C2} (IVA {AliquotaIVA}%)";
}

/// <summary>
/// Codice lotto di produzione farmaceutico.
/// Migrazione del campo "Lotto As String" in clsRegMovimentiLotto.cls.
/// </summary>
public sealed class CodiceLotto : SingleValueObject<string>
{
    private CodiceLotto(string v) : base(v) { }

    public static CodiceLotto Da(string valore)
    {
        var v = Guard.AgainstNullOrEmpty(valore).Trim().ToUpperInvariant();
        Guard.AgainstTooLong(v, 30, nameof(valore));
        return new CodiceLotto(v);
    }

    public static Result<CodiceLotto> TryCreate(string valore)
    {
        try   { return Result<CodiceLotto>.Ok(Da(valore)); }
        catch (DomainException ex) { return Result<CodiceLotto>.Fail(ex); }
    }
}

/// <summary>
/// Codice ATC (Anatomical Therapeutic Chemical).
/// Migrazione di: Public Const CSFATC As Byte = 8 (CSFDichiarazioni.bas).
/// Formato: lettera + 2 cifre + lettera + 2 cifre + 2 cifre (es. N02BE01).
/// </summary>
public sealed class CodiceATC : SingleValueObject<string>
{
    private static readonly System.Text.RegularExpressions.Regex _regex =
        new(@"^[A-Z]\d{2}[A-Z]{2}\d{2}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private CodiceATC(string v) : base(v) { }

    public static CodiceATC Da(string valore)
    {
        var v = Guard.AgainstNullOrEmpty(valore).Trim().ToUpperInvariant();
        if (!_regex.IsMatch(v))
            throw new DomainException(
                $"Codice ATC non valido: '{valore}'. Formato atteso: A00AA00 (es. N02BE01).",
                "ATC_INVALIDO");
        return new CodiceATC(v);
    }

    public static Result<CodiceATC> TryCreate(string valore)
    {
        try   { return Result<CodiceATC>.Ok(Da(valore)); }
        catch (DomainException ex) { return Result<CodiceATC>.Fail(ex); }
    }

    /// <summary>Gruppo anatomico principale (primo carattere).</summary>
    public char GruppoAnatomico => Valore[0];

    /// <summary>Sottogruppo terapeutico (caratteri 1-2).</summary>
    public string SottogruppoTerapeutico => Valore[1..3];
}
