using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SistemaF.Application.Ricerca.Queries;
using SistemaF.Domain.Entities.Ricerca;
using System.Collections.ObjectModel;

namespace SistemaF.UI.WPF.ViewModels.Prodotti;

/// <summary>
/// ViewModel per la ricerca prodotto.
/// Sostituisce frmRicerca.frm + ClasseRicerca.cls del VB6.
///
/// Funzionalità:
///   - Ricerca live mentre l'utente digita (debounce 300ms)
///   - Autodetect tipo ricerca (codice/EAN/ATC/descrizione)
///   - Selezione prodotto con dettaglio giacenze e lotti
///   - Cancellazione e reset ricerca
/// </summary>
public sealed partial class RicercaProdottoViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _debounceCts;

    public RicercaProdottoViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Stato ricerca ─────────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTermine))]
    [NotifyCanExecuteChangedFor(nameof(CercaCommand))]
    private string _termineRicerca = string.Empty;

    [ObservableProperty] private bool   _isCaricamento;
    [ObservableProperty] private string _messaggioStato = "Digita per cercare…";
    [ObservableProperty] private bool   _hasErrore;
    [ObservableProperty] private string _tipoRicercaRilevata = string.Empty;

    public bool HasTermine => TermineRicerca.Length >= 2;

    // ── Risultati ─────────────────────────────────────────────────────────────

    public ObservableCollection<RisultatoRicerca> Risultati { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelezione))]
    private RisultatoRicerca? _prodottoSelezionato;

    public bool HasSelezione      => ProdottoSelezionato is not null;
    public bool HasRisultati      => Risultati.Count > 0;
    public bool MostraStatoVuoto  => !IsCaricamento && !HasRisultati && TermineRicerca.Length >= 2;

    // ── Ricerca live (si attiva ogni volta che l'utente digita) ───────────────

    partial void OnTermineRicercaChanged(string value)
    {
        OnPropertyChanged(nameof(MostraStatoVuoto));

        if (value.Length < 2)
        {
            AzzeraRisultati();
            MessaggioStato    = "Digita almeno 2 caratteri…";
            TipoRicercaRilevata = string.Empty;
            return;
        }

        // Debounce: aspetta 300ms prima di cercare (evita ricerche ad ogni tasto)
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Delay(300, token).ContinueWith(
            async t =>
            {
                if (!t.IsCanceled)
                    await EseguiRicercaAsync(value, token);
            },
            TaskScheduler.Default);

        // Aggiorna il tipo rilevato in tempo reale (prima della ricerca)
        if (value.Length >= 2)
        {
            var criterio = CriterioRicerca.Rileva(value);
            TipoRicercaRilevata = criterio.Tipo switch
            {
                TipoRicercaProdotto.CodiceMinistriale => "🏷 Codice ministeriale",
                TipoRicercaProdotto.CodiceEAN         => "📊 Codice EAN",
                TipoRicercaProdotto.CodiceATC         => "⚕ Codice ATC",
                TipoRicercaProdotto.CodiceGTIN        => "📦 GTIN",
                _                                     => "🔤 Descrizione"
            };
        }
    }

    // ── Ricerca esplicita (bottone Cerca o Invio) ─────────────────────────────

    [RelayCommand(CanExecute = nameof(HasTermine))]
    private async Task CercaAsync(CancellationToken ct)
        => await EseguiRicercaAsync(TermineRicerca, ct);

    private async Task EseguiRicercaAsync(string termine, CancellationToken ct)
    {
        if (termine.Length < 2) return;

        // Esegue sul thread UI tramite Dispatcher (siamo su un task background)
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            IsCaricamento = true;
            HasErrore     = false;
            MessaggioStato = "Ricerca in corso…";
            Risultati.Clear();
            ProdottoSelezionato = null;
            OnPropertyChanged(nameof(HasRisultati));
            OnPropertyChanged(nameof(MostraStatoVuoto));

            try
            {
                var lista = await _mediator.Send(
                    new CercaProdottiQuery(termine, MaxRisultati: 100), ct);

                foreach (var r in lista)
                    Risultati.Add(r);

                MessaggioStato = Risultati.Count switch
                {
                    0    => "Nessun prodotto trovato",
                    1    => "1 prodotto trovato",
                    100  => "100+ prodotti trovati (raffina la ricerca)",
                    var n => $"{n} prodotti trovati"
                };
            }
            catch (OperationCanceledException)
            {
                // Ricerca annullata dal debounce — normale
            }
            catch (Exception ex)
            {
                HasErrore     = true;
                MessaggioStato = $"Errore: {ex.Message}";
            }
            finally
            {
                IsCaricamento = false;
                OnPropertyChanged(nameof(HasRisultati));
                OnPropertyChanged(nameof(MostraStatoVuoto));
            }
        });
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private void Azzera()
    {
        _debounceCts?.Cancel();
        TermineRicerca      = string.Empty;
        ProdottoSelezionato = null;
        HasErrore           = false;
        TipoRicercaRilevata = string.Empty;
        MessaggioStato      = "Digita per cercare…";
        AzzeraRisultati();
    }

    private void AzzeraRisultati()
    {
        Risultati.Clear();
        OnPropertyChanged(nameof(HasRisultati));
        OnPropertyChanged(nameof(MostraStatoVuoto));
    }

    // ── Selezione ─────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SelezionaProdotto(RisultatoRicerca? prodotto)
    {
        ProdottoSelezionato = prodotto;
        OnPropertyChanged(nameof(HasSelezione));
    }
}
