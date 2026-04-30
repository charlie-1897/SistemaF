namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  RESULT PATTERN
//
//  Non tutti gli errori sono eccezioni. Nel VB6 era comune restituire
//  valori speciali per indicare il fallimento:
//    Function Login(sUser, sPwd) As Boolean  → True = OK, False = fallito
//    Function CalcolaPrezzoSSN() As Currency → -1 = prodotto non mutuabile
//
//  Il Result<T> è un tipo discriminato che porta esplicitamente sia il
//  valore di successo sia il dettaglio dell'errore, senza usare eccezioni
//  per percorsi "attesi" (es. validazione form, prodotto non trovato).
//
//  Usare DomainException per violazioni di invarianti.
//  Usare Result<T> per operazioni che possono legittimamente fallire.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Rappresenta il risultato di un'operazione che può riuscire o fallire.
/// </summary>
public sealed class Result
{
    public bool     IsSuccess { get; }
    public bool     IsFailure => !IsSuccess;
    public string   ErrorCode { get; }
    public string   ErrorMessage { get; }

    private Result(bool success, string errorCode = "", string errorMessage = "")
    {
        IsSuccess    = success;
        ErrorCode    = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result Ok()
        => new(true);

    public static Result Fail(string errorMessage, string errorCode = "FAILURE")
        => new(false, errorCode, errorMessage);

    public static Result Fail(DomainException ex)
        => new(false, ex.ErrorCode, ex.Message);

    /// <summary>
    /// Esegue actionOnSuccess se il risultato è successo,
    /// oppure actionOnFailure se è fallimento.
    /// </summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess() : onFailure(ErrorMessage);

    public override string ToString()
        => IsSuccess ? "Ok" : $"Fail({ErrorCode}: {ErrorMessage})";
}

/// <summary>
/// Rappresenta il risultato di un'operazione che produce un valore in caso di successo.
/// </summary>
public sealed class Result<T>
{
    public bool    IsSuccess { get; }
    public bool    IsFailure => !IsSuccess;
    public string  ErrorCode { get; }
    public string  ErrorMessage { get; }

    private readonly T? _value;

    /// <summary>
    /// Il valore in caso di successo.
    /// Lancia InvalidOperationException se il risultato è fallimento.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Impossibile accedere a Value su un Result fallito: {ErrorMessage}");

    private Result(bool success, T? value, string errorCode, string errorMessage)
    {
        IsSuccess    = success;
        _value       = value;
        ErrorCode    = errorCode;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Ok(T value)
        => new(true, value, string.Empty, string.Empty);

    public static Result<T> Fail(string errorMessage, string errorCode = "FAILURE")
        => new(false, default, errorCode, errorMessage);

    public static Result<T> Fail(DomainException ex)
        => new(false, default, ex.ErrorCode, ex.Message);

    /// <summary>
    /// Applica una trasformazione al valore se il risultato è successo.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        => IsSuccess
            ? Result<TOut>.Ok(mapper(Value))
            : Result<TOut>.Fail(ErrorMessage, ErrorCode);

    /// <summary>
    /// Catena un'altra operazione che restituisce Result<TOut>.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        => IsSuccess ? binder(Value) : Result<TOut>.Fail(ErrorMessage, ErrorCode);

    /// <summary>
    /// Esegue actionOnSuccess o actionOnFailure e restituisce il valore corrispondente.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(ErrorMessage);

    /// <summary>
    /// Conversione implicita dal valore T a Result&lt;T&gt; di successo.
    /// Permette di scrivere: return valore; invece di return Result.Ok(valore);
    /// </summary>
    public static implicit operator Result<T>(T value) => Ok(value);

    public override string ToString()
        => IsSuccess ? $"Ok({_value})" : $"Fail({ErrorCode}: {ErrorMessage})";
}
