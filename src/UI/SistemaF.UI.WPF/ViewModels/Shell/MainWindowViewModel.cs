using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SistemaF.UI.WPF.ViewModels.Prodotti;

namespace SistemaF.UI.WPF.ViewModels.Shell;

/// <summary>
/// ViewModel della finestra principale.
/// Gestisce la navigazione tra le sezioni e il titolo corrente.
/// Sostituisce la logica di navigazione dei menu del VB6.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly RicercaProdottoViewModel _ricercaVm;

    public MainWindowViewModel(RicercaProdottoViewModel ricercaVm)
    {
        _ricercaVm     = ricercaVm;
        _paginaCorrente = _ricercaVm;
        _titoloPagina  = "Ricerca Prodotto";
    }

    // ── Navigazione ───────────────────────────────────────────────────────────

    [ObservableProperty] private object _paginaCorrente;
    [ObservableProperty] private string _titoloPagina = string.Empty;
    [ObservableProperty] private string _paginaAttivaId = "Ricerca";

    // ── Sezioni sidebar ───────────────────────────────────────────────────────

    [RelayCommand]
    private void NavigaARicerca()
    {
        PaginaCorrente  = _ricercaVm;
        TitoloPagina    = "Ricerca Prodotto";
        PaginaAttivaId  = "Ricerca";
    }

    // Placeholder per le sezioni che verranno completate nelle Wave successive
    [RelayCommand] private void NavigaAOrdini()    => SetPlaceholder("Ordini", "Ordini");
    [RelayCommand] private void NavigaAMagazzino() => SetPlaceholder("Magazzino", "Magazzino");
    [RelayCommand] private void NavigaAVendita()   => SetPlaceholder("Vendita", "Vendita");
    [RelayCommand] private void NavigaAReports()   => SetPlaceholder("Report", "Report");
    [RelayCommand] private void NavigaAImpostazioni() => SetPlaceholder("Impostazioni", "Impostazioni");

    private void SetPlaceholder(string id, string titolo)
    {
        PaginaCorrente = new PlaceholderViewModel(titolo);
        TitoloPagina   = titolo;
        PaginaAttivaId = id;
    }

    // ── Info farmacia (header) ────────────────────────────────────────────────

    public string NomeFarmacia   { get; } = "Farmacia Demo";
    public string DataOraOggi    => DateTime.Now.ToString("dddd d MMMM yyyy",
        System.Globalization.CultureInfo.GetCultureInfo("it-IT"));
    public string VersioneApp    { get; } = "SistemaF v2.0 — .NET 8";
}

/// <summary>ViewModel placeholder per le sezioni non ancora implementate.</summary>
public sealed class PlaceholderViewModel(string sezione)
{
    public string Sezione      { get; } = sezione;
    public string Messaggio    { get; } = $"Il modulo '{sezione}' sarà disponibile nelle prossime sessioni di migrazione.";
}
