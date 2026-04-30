using System.Linq.Expressions;

namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  SPECIFICATION PATTERN
//
//  Nel VB6 i criteri di ricerca erano hardcoded nelle query SQL inline:
//    "SELECT * FROM Prodotti WHERE Classe = 'A' AND Giacenza < ScortaMin"
//  Impossibile da riutilizzare, testare o comporre.
//
//  Una Specification incapsula un criterio di selezione come oggetto.
//  È esprimibile sia come Expression<Func<T, bool>> (per EF Core / SQL)
//  sia come Func<T, bool> (per filtrare liste in-memory nei test).
//  Le specification si compongono con And, Or, Not senza scrivere SQL.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Interfaccia per una Specification su tipo T.
/// </summary>
public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();

    bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);
}

/// <summary>
/// Classe base per le Specification: fornisce operatori And, Or, Not.
/// </summary>
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity) => ToExpression().Compile()(entity);

    /// <summary>Combina due specification con AND.</summary>
    public Specification<T> And(Specification<T> other)
        => new AndSpecification<T>(this, other);

    /// <summary>Combina due specification con OR.</summary>
    public Specification<T> Or(Specification<T> other)
        => new OrSpecification<T>(this, other);

    /// <summary>Nega la specification.</summary>
    public Specification<T> Not()
        => new NotSpecification<T>(this);
}

// ── Combinatori interni ────────────────────────────────────────────────────────

internal sealed class AndSpecification<T>(
    Specification<T> left,
    Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr  = left.ToExpression();
        var rightExpr = right.ToExpression();
        var param     = Expression.Parameter(typeof(T), "x");
        var body      = Expression.AndAlso(
            Expression.Invoke(leftExpr,  param),
            Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal sealed class OrSpecification<T>(
    Specification<T> left,
    Specification<T> right) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr  = left.ToExpression();
        var rightExpr = right.ToExpression();
        var param     = Expression.Parameter(typeof(T), "x");
        var body      = Expression.OrElse(
            Expression.Invoke(leftExpr,  param),
            Expression.Invoke(rightExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}

internal sealed class NotSpecification<T>(Specification<T> inner) : Specification<T>
{
    public override Expression<Func<T, bool>> ToExpression()
    {
        var innerExpr = inner.ToExpression();
        var param     = Expression.Parameter(typeof(T), "x");
        var body      = Expression.Not(Expression.Invoke(innerExpr, param));
        return Expression.Lambda<Func<T, bool>>(body, param);
    }
}
