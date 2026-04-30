using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using SistemaF.Domain.Common;

namespace SistemaF.Application.Common.Behaviors;

// ── Validation Behavior ───────────────────────────────────────────────────────

/// <summary>
/// Esegue la validazione FluentValidation prima che il comando/query
/// raggiunga il suo handler. Se fallisce, restituisce Result.Fail
/// invece di lanciare eccezione (compatibile con tutti gli handler del progetto).
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        if (!validators.Any()) return await next();

        var context  = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0) return await next();

        var errori = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // Se TResponse è Result<T> o Result, restituiamo un errore tipizzato
        if (TryBuildFailResult(errori, out var failResult))
            return failResult!;

        throw new ValidationException(failures);
    }

    private static bool TryBuildFailResult(string errori, out TResponse? result)
    {
        var type = typeof(TResponse);

        if (type == typeof(Result))
        {
            result = (TResponse)(object)Result.Fail(errori, "VALIDATION_ERROR");
            return true;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var method = type.GetMethod("Fail",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                null, [typeof(string), typeof(string)], null);
            result = (TResponse)method!.Invoke(null, [errori, "VALIDATION_ERROR"])!;
            return true;
        }

        result = default;
        return false;
    }
}

// ── Logging Behavior ──────────────────────────────────────────────────────────

/// <summary>
/// Logga inizio/fine (con durata) di ogni comando che modifica dati.
/// Le query non vengono loggiate a livello Information per non inquinare i log.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest  : IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest                          request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken                 ct)
    {
        var name = typeof(TRequest).Name;

        // Logga solo i Command (contengono "Command" nel nome), non le Query
        var isCommand = name.EndsWith("Command", StringComparison.Ordinal);

        if (isCommand)
            logger.LogInformation("→ Inizio {Command}", name);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();

            if (isCommand)
                logger.LogInformation(
                    "← Fine {Command} [{Ms}ms]", name, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex,
                "✗ Errore in {Command} [{Ms}ms]", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
