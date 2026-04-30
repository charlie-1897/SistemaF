using System.Reflection;

namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  ENUMERATION (Smart Enum)
//
//  I plain enum C# hanno limiti importanti per il dominio:
//    • Non possono avere metodi (es. "questo stato può passare a X?")
//    • Il ToString() restituisce il nome tecnico, non un'etichetta leggibile
//    • Non si possono aggiungere proprietà (es. colore, icona, descrizione)
//    • Il valore numerico può cambiare per refactoring
//
//  La classe Enumeration è una alternativa ispirata al pattern DDD di Eric Evans.
//  Ogni valore è un'istanza statica — i benefici di enum senza i limiti.
//
//  Nel VB6 i valori enumerativi erano costanti Public Const nei moduli .bas:
//    Public Const CSFMINISTERIALE As Byte = 1
//    Public Const CSFATC As Byte = 8
//  Questi vanno migrati come Enumeration dove hanno logica associata,
//  o come semplici enum C# dove sono solo tag numerici.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base per Smart Enum: combina la semplicità degli enum con la ricchezza degli oggetti.
/// </summary>
public abstract class Enumeration : IEquatable<Enumeration>, IComparable<Enumeration>
{
    public int    Id   { get; }
    public string Nome { get; }

    protected Enumeration(int id, string nome)
    {
        Id   = id;
        Nome = nome;
    }

    // ── Lookup ────────────────────────────────────────────────────────────────

    public static IEnumerable<T> GetAll<T>() where T : Enumeration
        => typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(T))
            .Select(f => (T)f.GetValue(null)!);

    public static T? FromId<T>(int id) where T : Enumeration
        => GetAll<T>().FirstOrDefault(e => e.Id == id);

    public static T? FromNome<T>(string nome) where T : Enumeration
        => GetAll<T>().FirstOrDefault(e =>
            string.Equals(e.Nome, nome, StringComparison.OrdinalIgnoreCase));

    public static T FromIdOrThrow<T>(int id) where T : Enumeration
        => FromId<T>(id)
            ?? throw new DomainException(
                $"Valore {id} non trovato in {typeof(T).Name}.", "INVALID_ENUM_ID");

    // ── Confronto ─────────────────────────────────────────────────────────────

    public bool Equals(Enumeration? other)
        => other is not null && GetType() == other.GetType() && Id == other.Id;

    public override bool Equals(object? obj)
        => obj is Enumeration e && Equals(e);

    public override int GetHashCode() => Id.GetHashCode();

    public int CompareTo(Enumeration? other) => Id.CompareTo(other?.Id);

    public static bool operator ==(Enumeration? left, Enumeration? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Enumeration? left, Enumeration? right)
        => !(left == right);

    public override string ToString() => Nome;
}

// ─────────────────────────────────────────────────────────────────────────────
//  ESEMPI DI ENUMERATION USATI IN SISTEMAF
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Tipo di ricerca prodotto.
/// Migrazione delle costanti CSFxxx in CSFDichiarazioni.bas.
/// </summary>
public sealed class TipoRicercaProdotto : Enumeration
{
    public static readonly TipoRicercaProdotto Ministeriale      = new(1,  "Codice ministeriale");
    public static readonly TipoRicercaProdotto Patologia         = new(2,  "Patologia");
    public static readonly TipoRicercaProdotto Sostanza          = new(3,  "Sostanza attiva");
    public static readonly TipoRicercaProdotto Descrizione       = new(4,  "Descrizione");
    public static readonly TipoRicercaProdotto Gruppo            = new(5,  "Gruppo");
    public static readonly TipoRicercaProdotto Ditta             = new(6,  "Ditta");
    public static readonly TipoRicercaProdotto PluriPrescrizione = new(7,  "Pluriprescrizione");
    public static readonly TipoRicercaProdotto ATC               = new(8,  "Codice ATC");
    public static readonly TipoRicercaProdotto CategoriaRicetta  = new(9,  "Categoria ricetta");
    public static readonly TipoRicercaProdotto Segnalazione      = new(11, "Segnalazione");
    public static readonly TipoRicercaProdotto CodiceCSFData     = new(12, "Codice CSF + data");
    public static readonly TipoRicercaProdotto SettoreInventario = new(13, "Settore inventario");
    public static readonly TipoRicercaProdotto Veterinario       = new(14, "Uso veterinario");
    public static readonly TipoRicercaProdotto EAN               = new(15, "Codice EAN");
    public static readonly TipoRicercaProdotto Classe            = new(16, "Classe rimborsabilità");
    public static readonly TipoRicercaProdotto PrezzoRiferimento = new(22, "Prezzo di riferimento");
    public static readonly TipoRicercaProdotto FormaBiotica      = new(24, "Forma farmaceutica");

    private TipoRicercaProdotto(int id, string nome) : base(id, nome) { }
}

/// <summary>
/// Classe di rimborsabilità SSN.
/// Migrazione di CSFCLASSE (byte) in CSFDichiarazioni.bas,
/// arricchita con proprietà di comportamento.
/// </summary>
public sealed class ClasseFarmaco : Enumeration
{
    public static readonly ClasseFarmaco A        = new(1, "A",  true,  "Mutuabile SSN");
    public static readonly ClasseFarmaco C        = new(2, "C",  false, "Non mutuabile");
    public static readonly ClasseFarmaco H        = new(3, "H",  true,  "Uso ospedaliero");
    public static readonly ClasseFarmaco OTC      = new(4, "OTC",false, "Senza ricetta");
    public static readonly ClasseFarmaco SOP      = new(5, "SOP",false, "Senza obbligo prescrizione");
    public static readonly ClasseFarmaco Galenico = new(6, "G",  false, "Galenico");

    /// <summary>True se il farmaco è rimborsato dal SSN.</summary>
    public bool IsMutuabile { get; }

    /// <summary>Etichetta estesa per stampa e report.</summary>
    public string Descrizione { get; }

    private ClasseFarmaco(int id, string nome, bool mutuabile, string descrizione)
        : base(id, nome)
    {
        IsMutuabile  = mutuabile;
        Descrizione  = descrizione;
    }

    /// <summary>Restituisce la stringa breve per le intestazioni (A, C, H…).</summary>
    public string CodiceBreve => Nome;
}

/// <summary>
/// Categoria di ricetta medica richiesta per la vendita.
/// Migrazione di CSFCATEGORIARICETTA in CSFDichiarazioni.bas.
/// </summary>
public sealed class CategoriaRicetta : Enumeration
{
    public static readonly CategoriaRicetta NessunObbligo       = new(0, "Nessun obbligo");
    public static readonly CategoriaRicetta RRipetibile         = new(1, "Ricetta ripetibile");
    public static readonly CategoriaRicetta RNonRipetibile      = new(2, "Ricetta non ripetibile");
    public static readonly CategoriaRicetta RLimitativa         = new(3, "Ricetta limitativa");
    public static readonly CategoriaRicetta Stupefacente        = new(4, "Stupefacente");

    /// <summary>True se è obbligatoria una ricetta medica per vendere il prodotto.</summary>
    public bool RichiedeRicetta => Id > 0;

    private CategoriaRicetta(int id, string nome) : base(id, nome) { }
}
