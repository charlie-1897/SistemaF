using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SistemaF.Application.Prodotti.Queries;
using System.Collections.ObjectModel;

namespace SistemaF.UI.WPF.ViewModels.Prodotti;

/// <summary>
/// ViewModel per la ricerca e visualizzazione prodotti farmaceutici.
///
/// Sostituisce il vecchio ElencoAnagrafiche.xaml.vb (VB6/.NET prototipo)
/// e la logica di ricerca dispersa in CSFRicerca.dll.
///
/// Pattern: MVVM con CommunityToolkit.Mvvm + MediatR per i query.
/// </summary>
public sealed partial class ProdottiViewModel(IMediator mediator) : ObservableObject
{
    // --- Proprietà osservabili -----------------------------------------------

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CercaCommand))]
    private string _termineRicerca = string.Empty;

    [ObservableProperty]
    private ProdottoDto? _prodottoSelezionato;

    [ObservableProperty]
    private bool _isCaricamento;

    [ObservableProperty]
    private string? _messaggioErrore;

    public ObservableCollection<ProdottoDto> Prodotti { get; } = [];

    // --- Comandi -------------------------------------------------------------

    [RelayCommand(CanExecute = nameof(CanCerca))]
    private async Task CercaAsync(CancellationToken ct)
    {
        IsCaricamento = true;
        MessaggioErrore = null;
        Prodotti.Clear();

        try
        {
            var risultati = await mediator.Send(
                new CercaProdottiQuery(TermineRicerca, Limit: 50), ct);

            foreach (var p in risultati.Prodotti)
                Prodotti.Add(p);

            if (!Prodotti.Any())
                MessaggioErrore = "Nessun prodotto trovato.";
        }
        catch (OperationCanceledException)
        {
            // ricerca annullata dall'utente — ignorare
        }
        catch (Exception ex)
        {
            MessaggioErrore = $"Errore durante la ricerca: {ex.Message}";
        }
        finally
        {
            IsCaricamento = false;
        }
    }

    private bool CanCerca() =>
        !string.IsNullOrWhiteSpace(TermineRicerca) && TermineRicerca.Length >= 2;

    [RelayCommand]
    private void AzzeraRicerca()
    {
        TermineRicerca = string.Empty;
        ProdottoSelezionato = null;
        MessaggioErrore = null;
        Prodotti.Clear();
    }

    // --- Proprietà calcolate sulla selezione ---------------------------------

    public bool HasProdottoSelezionato => ProdottoSelezionato is not null;

    partial void OnProdottoSelezionatoChanged(ProdottoDto? value) =>
        OnPropertyChanged(nameof(HasProdottoSelezionato));
}
