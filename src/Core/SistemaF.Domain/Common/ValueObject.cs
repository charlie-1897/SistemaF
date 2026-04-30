namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  VALUE OBJECT
//
//  Un Value Object è immutabile e viene confrontato per valore (non per identità).
//  Non ha Id, non ha ciclo di vita proprio.
//  Due Value Object con gli stessi valori sono IDENTICI — come i numeri in matematica.
//
//  Nel VB6 i concetti espressi come Value Object erano semplici String o Currency:
//    Dim sCodice As String   → CodiceProdotto
//    Dim dPrezzo As Currency → Prezzo
//    Dim sEAN As String      → CodiceEAN
//
//  Il problema era che nulla impediva di usare un codice EAN dove serviva
//  un codice ministeriale, o di sommare prezzi con IVA diverse.
//  I Value Object risolvono questo a compile-time.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Classe base per tutti i Value Object del dominio.
/// Implementa automaticamente Equals, GetHashCode e gli operatori == / !=
/// basandosi sui componenti restituiti da GetEqualityComponents().
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Restituisce i valori che determinano l'uguaglianza.
    /// Implementare in ogni sottoclasse con yield return su ogni proprietà.
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public bool Equals(ValueObject? other)
    {
        if (other is null) return false;
        if (GetType() != other.GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(other.GetEqualityComponents());
    }

    public override bool Equals(object? obj)
        => obj is ValueObject vo && Equals(vo);

    public override int GetHashCode()
        => GetEqualityComponents()
            .Aggregate(
                seed: 17,
                (hash, component) => HashCode.Combine(hash, component));

    public static bool operator ==(ValueObject? left, ValueObject? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(ValueObject? left, ValueObject? right)
        => !(left == right);
}

/// <summary>
/// Value Object a campo singolo: semplifica i casi più comuni
/// dove il value object wrappa un solo valore primitivo.
/// Esempio d'uso:
///   public sealed class CodiceFiscale : SingleValueObject&lt;string&gt; { ... }
/// </summary>
public abstract class SingleValueObject<T> : ValueObject
    where T : notnull
{
    public T Valore { get; }

    protected SingleValueObject(T valore) => Valore = valore;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Valore;
    }

    public override string ToString() => Valore.ToString() ?? string.Empty;

    // Conversione implicita al tipo sottostante per comodità negli accessi.
    public static implicit operator T(SingleValueObject<T> vo) => vo.Valore;
}
