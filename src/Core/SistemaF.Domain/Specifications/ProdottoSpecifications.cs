using System.Linq.Expressions;
using SistemaF.Domain.Entities.Prodotto;
using SistemaF.Domain.ValueObjects;

namespace SistemaF.Domain.Specifications;

// ═══════════════════════════════════════════════════════════════════════════════
//  SPECIFICATION — Prodotto
//
//  Ogni Specification sostituisce un filtro SQL hardcoded del VB6.
//  Si compongono con .And(), .Or(), .Not() senza scrivere LINQ o SQL.
//  Vengono usate nei Repository per costruire le query EF Core.
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Prodotti attivi (IsAttivo = true, IsDeleted = false).</summary>
public sealed class ProdottiAttiviSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && !p.IsDeleted;
}

/// <summary>
/// Prodotti sottoscorta nell'esposizione.
/// Migrazione della query usata in frmMancanti e lista Mancanti.ctl del VB6.
/// </summary>
public sealed class ProdottiSottoscortaSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo
             && !p.IsDeleted
             && p.GiacenzaEsposizione.ScortaMinima > 0
             && p.GiacenzaEsposizione.Giacenza < p.GiacenzaEsposizione.ScortaMinima;
}

/// <summary>Prodotti mutuabili SSN (classe A o H).</summary>
public sealed class ProdottiMutuabiliSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo
             && (p.Classe == ClasseFarmaco.A || p.Classe == ClasseFarmaco.H);
}

/// <summary>Prodotti stupefacenti.</summary>
public sealed class ProdottiStupefacentiSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && p.IsStupefacente;
}

/// <summary>Prodotti congelati.</summary>
public sealed class ProdottiCongelatiSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && p.IsCongelato;
}

/// <summary>Prodotti veterinari.</summary>
public sealed class ProdottiVeterinariSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && p.IsVeterinario;
}

/// <summary>
/// Ricerca per termine libero su codice, descrizione, EAN, ATC.
/// Migrazione della ricerca multi-tipo di CSFRicerca.dll.
/// </summary>
public sealed class ProdottoCercaSpec : Specification<Prodotto>
{
    private readonly string _termine;

    public ProdottoCercaSpec(string termine)
        => _termine = (termine ?? string.Empty).Trim().ToUpperInvariant();

    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && (
            p.CodiceFarmaco.Valore.StartsWith(_termine) ||
            p.Descrizione.Contains(_termine) ||
            (p.CodiceEAN != null && p.CodiceEAN.Valore == _termine) ||
            (p.CodiceATC != null && p.CodiceATC.Valore.StartsWith(_termine)) ||
            (p.Sostanza   != null && p.Sostanza.Contains(_termine))   ||
            (p.Gruppo     != null && p.Gruppo.Contains(_termine))     ||
            (p.LineaDitta != null && p.LineaDitta.Contains(_termine)));
}

/// <summary>Filtra per tipo di ricerca specifico (CSFMINISTERIALE, CSFATC ecc.).</summary>
public sealed class ProdottoRicercaTipoSpec : Specification<Prodotto>
{
    private readonly TipoRicercaProdotto _tipo;
    private readonly string             _valore;

    public ProdottoRicercaTipoSpec(TipoRicercaProdotto tipo, string valore)
    {
        _tipo   = tipo;
        _valore = (valore ?? string.Empty).Trim().ToUpperInvariant();
    }

    public override Expression<Func<Prodotto, bool>> ToExpression() => _tipo.Id switch
    {
        1  => p => p.CodiceFarmaco.Valore.StartsWith(_valore),               // Ministeriale
        3  => p => p.Sostanza    != null && p.Sostanza.Contains(_valore),    // Sostanza
        4  => p => p.Descrizione.Contains(_valore),                           // Descrizione
        5  => p => p.Gruppo      != null && p.Gruppo.Contains(_valore),      // Gruppo
        6  => p => p.CodiceDitta != null && p.CodiceDitta.Contains(_valore), // Ditta
        7  => p => p.IsPluriPrescrizione,                                     // Pluriprescrizione
        8  => p => p.CodiceATC   != null && p.CodiceATC.Valore.StartsWith(_valore), // ATC
        9  => p => p.CategoriaRicetta.Id == int.Parse(_valore),              // Cat.Ricetta
        14 => p => p.IsVeterinario,                                           // Veterinario
        15 => p => p.CodiceEAN   != null && p.CodiceEAN.Valore == _valore,   // EAN
        16 => p => p.Classe.CodiceBreve == _valore,                          // Classe
        17 => p => p.IsCongelato,                                             // Congelati
        18 => p => p.IsStupefacente,                                          // Stupefacenti
        19 => p => p.IsTrattato,                                              // Trattati
        21 => p => p.LineaDitta  != null && p.LineaDitta.Contains(_valore),  // Linea ditta
        24 => p => p.FormaBiotica!= null && p.FormaBiotica.Contains(_valore),// Forma biotica
        _  => p => false
    };
}

/// <summary>Prodotti invendibili.</summary>
public sealed class ProdottiInvendibiliSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsInvendibile && !p.IsDeleted;
}

/// <summary>Prodotti segnalati.</summary>
public sealed class ProdottiSegnalatiSpec : Specification<Prodotto>
{
    public override Expression<Func<Prodotto, bool>> ToExpression()
        => p => p.IsAttivo && p.IsSegnalato;
}
