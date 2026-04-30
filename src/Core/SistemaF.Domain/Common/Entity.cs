namespace SistemaF.Domain.Common;

// ═══════════════════════════════════════════════════════════════════════════════
//  BASE ENTITY
//
//  Nel VB6 ogni "oggetto" di dominio era una classe COM (.cls) senza una base
//  comune: ogni modulo duplicava la gestione dell'Id, della connessione DB e
//  degli stati. Qui centralizziamo tutte le responsabilità condivise.
//
//  Regole:
//    • Il costruttore protetto impedisce l'istanziazione diretta dall'esterno.
//    • L'Id è assegnato alla creazione e non può essere cambiato.
//    • Le sottoclassi usano init-only setter o factory method per impostare i dati.
//    • L'uguaglianza è per identità (Id), non per valore delle proprietà.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Classe base per tutte le entità del dominio SistemaF.
/// Un'entità ha identità propria (Id) che persiste nel tempo
/// anche se le sue proprietà cambiano.
/// </summary>
public abstract class Entity
{
    // ── Identità ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Identificatore univoco dell'entità.
    /// Assegnato alla creazione, immutabile per tutta la vita dell'oggetto.
    /// Sostituisce i vecchi campi cpXxx (chiave primaria) delle classi VB6.
    /// </summary>
    public Guid Id { get; private set; }

    // ── Audit trail ───────────────────────────────────────────────────────────

    /// <summary>Data/ora UTC di creazione del record.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Data/ora UTC dell'ultima modifica. Null se mai modificato.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Identificatore dell'operatore che ha creato il record.</summary>
    public Guid? CreatedBy { get; private set; }

    /// <summary>Identificatore dell'operatore che ha eseguito l'ultima modifica.</summary>
    public Guid? UpdatedBy { get; private set; }

    // ── Costruzione ───────────────────────────────────────────────────────────

    /// <summary>
    /// Costruttore usato in fase di creazione di un nuovo oggetto.
    /// </summary>
    protected Entity(Guid? operatoreId = null)
    {
        Id        = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        CreatedBy = operatoreId;
    }

    /// <summary>
    /// Costruttore senza parametri richiesto da EF Core per la ricostruzione
    /// degli oggetti dal database (reflection). Non usare nel codice applicativo.
    /// </summary>
    protected Entity() { }

    // ── Metodi interni di audit ───────────────────────────────────────────────

    /// <summary>
    /// Aggiorna il timestamp e l'autore dell'ultima modifica.
    /// Da chiamare dentro ogni metodo che muta lo stato dell'entità.
    /// </summary>
    protected internal void SetUpdated(Guid? operatoreId = null)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = operatoreId;
    }

    // ── Uguaglianza ──────────────────────────────────────────────────────────

    /// <summary>
    /// Due entità sono uguali se e solo se hanno lo stesso Id e lo stesso tipo.
    /// Il valore delle proprietà non conta per determinare l'identità.
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        if (ReferenceEquals(this, obj)) return true;
        return ((Entity)obj).Id == Id;
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity? left, Entity? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(Entity? left, Entity? right) => !(left == right);

    public override string ToString() => $"{GetType().Name}[{Id:N}]";
}

/// <summary>
/// Classe base generica per entità con Id fortemente tipizzato.
/// Usare quando si vuole evitare di confondere Guid di entità diverse.
/// Esempio: non si può accidentalmente passare un ProdottoId dove serve un OrdineId.
/// </summary>
public abstract class Entity<TId> : Entity
    where TId : notnull
{
    // EF Core lavora con Id di tipo Guid internamente: questa classe
    // mantiene il Guid base e aggiunge il typed Id per il codice di dominio.
    public new TId Id
    {
        get => TypedId
            ?? throw new InvalidOperationException("L'Id tipizzato non è stato impostato.");
        private protected set => TypedId = value;
    }

    private TId? TypedId { get; set; }

    protected Entity() { }

    protected Entity(TId id, Guid? operatoreId = null) : base(operatoreId)
        => TypedId = id;
}
