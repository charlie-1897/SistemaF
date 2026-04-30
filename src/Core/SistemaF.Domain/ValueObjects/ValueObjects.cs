using SistemaF.Domain.Common;

namespace SistemaF.Domain.ValueObjects;

/// <summary>
/// Value Object per prezzi monetari in euro.
/// Sostituisce i campi Currency e l'uso diretto di Double del VB6.
/// </summary>
public sealed class Prezzo : ValueObject
{
    public decimal Valore { get; }
    public int AliquotaIVA { get; }   // 4, 10, 22

    private Prezzo() { }

    public static Prezzo Di(decimal valore, int aliquotaIVA = 10)
    {
        if (valore < 0) throw new DomainException("Il prezzo non può essere negativo.");
        if (aliquotaIVA is not (4 or 10 or 22))
            throw new DomainException($"Aliquota IVA non valida: {aliquotaIVA}%.");

        return new Prezzo { Valore = Math.Round(valore, 2) };
    }

    public decimal PrezzoIVAInclusa => Valore * (1 + AliquotaIVA / 100m);
    public decimal ImportoIVA => PrezzoIVAInclusa - Valore;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valore;
        yield return AliquotaIVA;
    }

    public override string ToString() => $"{Valore:C2}";
}

/// <summary>
/// Codice ministeriale a 9 cifre (ex Codice Farmaco).
/// Corrisponde a CSFMINISTERIALE in CSFDichiarazioni.bas.
/// </summary>
public sealed class CodiceProdotto : ValueObject
{
    public string Valore { get; }

    private CodiceProdotto() { }

    public static CodiceProdotto Da(string valore)
    {
        var normalizzato = valore?.Trim() ?? string.Empty;
        if (normalizzato.Length != 9 || !normalizzato.All(char.IsDigit))
            throw new DomainException($"Codice farmaco non valido: '{valore}'. Deve essere 9 cifre.");

        return new CodiceProdotto { Valore = normalizzato };
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Valore; }
    public override string ToString() => Valore;
}

/// <summary>
/// Codice EAN-13.
/// Corrisponde a CSFCODICEEAN in CSFDichiarazioni.bas.
/// </summary>
public sealed class CodiceEAN : ValueObject
{
    public string Valore { get; }

    private CodiceEAN() { }

    public static CodiceEAN Da(string valore)
    {
        var v = valore?.Trim() ?? string.Empty;
        if (v.Length != 13 || !v.All(char.IsDigit))
            throw new DomainException($"Codice EAN non valido: '{valore}'.");
        if (!ValidaCheckDigit(v))
            throw new DomainException($"Check digit EAN non valido: '{valore}'.");

        return new CodiceEAN { Valore = v };
    }

    private static bool ValidaCheckDigit(string ean)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
            sum += (ean[i] - '0') * (i % 2 == 0 ? 1 : 3);
        var checkDigit = (10 - sum % 10) % 10;
        return checkDigit == (ean[12] - '0');
    }

    protected override IEnumerable<object?> GetEqualityComponents() { yield return Valore; }
    public override string ToString() => Valore;
}

/// <summary>
/// Sessione operatore: traduce CSessione e modSessione.bas del VB6.
/// </summary>
public sealed class SessioneOperatore : ValueObject
{
    public Guid SessionId { get; }
    public int CodiceOperatore { get; }
    public string LoginOperatore { get; }
    public int CodiceTerminale { get; }
    public IReadOnlyList<string> Autorizzazioni { get; }
    public DateTime DataLogin { get; }

    public SessioneOperatore(int codiceOperatore, string login, int terminale, IEnumerable<string> autorizzazioni)
    {
        SessionId        = Guid.NewGuid();
        CodiceOperatore  = codiceOperatore;
        LoginOperatore   = login;
        CodiceTerminale  = terminale;
        Autorizzazioni   = autorizzazioni.ToList().AsReadOnly();
        DataLogin        = DateTime.UtcNow;
    }

    public bool HasAutorizzazione(string codice) =>
        Autorizzazioni.Contains(codice, StringComparer.OrdinalIgnoreCase);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SessionId;
        yield return CodiceOperatore;
    }
}

/// <summary>Eccezione per violazioni di regole di dominio.</summary>
public sealed class DomainException(string message) : Exception(message);
