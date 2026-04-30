using System.Runtime.CompilerServices;

namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  GUARD CLAUSES
//
//  Nel VB6 la validazione dei parametri era spesso assente o ripetuta
//  con pattern inconsistenti:
//    If sValore = "" Then MsgBox "Campo obbligatorio": Exit Sub
//    If nQty < 0 Then Exit Function
//
//  Le Guard Clauses centralizzano questi controlli e li rendono leggibili.
//  La classe Guard usa CallerArgumentExpression per includere automaticamente
//  il nome del parametro nel messaggio di errore — senza doverlo ripetere.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Collezione di guard clause per validare precondizioni nei metodi di dominio.
/// Lancia DomainException o ArgumentException secondo il tipo di violazione.
/// </summary>
public static class Guard
{
    // ── Null / vuoto ──────────────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che il valore non sia null.
    /// </summary>
    public static T AgainstNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : class
    {
        if (value is null)
            throw new DomainException(
                $"Il parametro '{paramName}' non può essere null.",
                "NULL_VALUE");
        return value;
    }

    /// <summary>
    /// Garantisce che la stringa non sia null o vuota.
    /// </summary>
    public static string AgainstNullOrEmpty(
        string? value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(
                $"Il parametro '{paramName}' non può essere vuoto.",
                "EMPTY_VALUE");
        return value.Trim();
    }

    /// <summary>
    /// Garantisce che la stringa abbia una lunghezza massima.
    /// </summary>
    public static string AgainstTooLong(
        string value,
        int maxLength,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        AgainstNullOrEmpty(value, paramName);
        if (value.Length > maxLength)
            throw new DomainException(
                $"Il parametro '{paramName}' supera la lunghezza massima di {maxLength} caratteri " +
                $"(lunghezza attuale: {value.Length}).",
                "TOO_LONG");
        return value;
    }

    // ── Intervalli numerici ───────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che il valore numerico sia positivo (> 0).
    /// </summary>
    public static T AgainstNonPositive<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) <= 0)
            throw new DomainException(
                $"Il parametro '{paramName}' deve essere maggiore di zero " +
                $"(valore ricevuto: {value}).",
                "NON_POSITIVE");
        return value;
    }

    /// <summary>
    /// Garantisce che il valore numerico non sia negativo (>= 0).
    /// </summary>
    public static T AgainstNegative<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : IComparable<T>
    {
        if (value.CompareTo(default!) < 0)
            throw new DomainException(
                $"Il parametro '{paramName}' non può essere negativo " +
                $"(valore ricevuto: {value}).",
                "NEGATIVE_VALUE");
        return value;
    }

    /// <summary>
    /// Garantisce che il valore rientri nell'intervallo [min, max].
    /// </summary>
    public static T AgainstOutOfRange<T>(
        T value,
        T min,
        T max,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new DomainException(
                $"Il parametro '{paramName}' deve essere compreso tra {min} e {max} " +
                $"(valore ricevuto: {value}).",
                "OUT_OF_RANGE");
        return value;
    }

    // ── Guid ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che il Guid non sia Empty.
    /// </summary>
    public static Guid AgainstEmptyGuid(
        Guid value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
    {
        if (value == Guid.Empty)
            throw new DomainException(
                $"Il parametro '{paramName}' non può essere un Guid vuoto.",
                "EMPTY_GUID");
        return value;
    }

    // ── Enum ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che il valore dell'enum sia definito (evita cast errati).
    /// </summary>
    public static T AgainstUndefinedEnum<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string paramName = "")
        where T : struct, Enum
    {
        if (!Enum.IsDefined(typeof(T), value))
            throw new DomainException(
                $"Il valore '{value}' non è definito nell'enumerazione '{typeof(T).Name}' " +
                $"(parametro '{paramName}').",
                "UNDEFINED_ENUM");
        return value;
    }

    // ── Stato macchina ────────────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che una condizione booleana sia vera.
    /// Usare per invarianti di business complesse.
    /// </summary>
    public static void AgainstFalse(
        bool condition,
        string regola,
        string dettaglio)
    {
        if (!condition)
            throw new BusinessRuleViolationException(regola, dettaglio);
    }

    /// <summary>
    /// Garantisce che un aggregate sia nello stato atteso prima di un'operazione.
    /// </summary>
    public static void AgainstInvalidState<TStato>(
        TStato statoAttuale,
        TStato statoAtteso,
        string tipoEntita,
        [CallerArgumentExpression(nameof(statoAttuale))] string paramName = "")
        where TStato : struct, Enum
    {
        if (!statoAttuale.Equals(statoAtteso))
            throw new InvalidStatoException(tipoEntita, statoAttuale, statoAtteso);
    }

    /// <summary>
    /// Garantisce che un aggregate non sia in uno degli stati proibiti.
    /// </summary>
    public static void AgainstStates<TStato>(
        TStato statoAttuale,
        string tipoEntita,
        string operazione,
        params TStato[] statiProibiti)
        where TStato : struct, Enum
    {
        if (statiProibiti.Contains(statoAttuale))
            throw new DomainException(
                $"Impossibile eseguire '{operazione}' su {tipoEntita} in stato '{statoAttuale}'.",
                "INVALID_STATE");
    }

    // ── Collezioni ────────────────────────────────────────────────────────────

    /// <summary>
    /// Garantisce che la collezione non sia null e non vuota.
    /// </summary>
    public static IEnumerable<T> AgainstEmpty<T>(
        IEnumerable<T>? collection,
        [CallerArgumentExpression(nameof(collection))] string paramName = "")
    {
        var list = collection?.ToList()
            ?? throw new DomainException(
                $"La collezione '{paramName}' non può essere null.", "NULL_VALUE");

        if (list.Count == 0)
            throw new DomainException(
                $"La collezione '{paramName}' non può essere vuota.", "EMPTY_COLLECTION");

        return list;
    }
}
