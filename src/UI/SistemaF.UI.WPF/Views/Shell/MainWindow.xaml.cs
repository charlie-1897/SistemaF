using SistemaF.UI.WPF.ViewModels.Shell;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SistemaF.UI.WPF.Views.Shell;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        InitializeComponent();
        Resources["NavStyleConverter"] = new NavStyleConverter(this);
        DataContext = vm;
    }
}

/// <summary>
/// Restituisce NavButtonActiveStyle o NavButtonStyle a seconda della
/// pagina attiva corrente. Usato nel binding Style dei bottoni sidebar.
/// </summary>
internal sealed class NavStyleConverter : IValueConverter
{
    private readonly FrameworkElement _owner;

    public NavStyleConverter(FrameworkElement owner)
        => _owner = owner;

    public object? Convert(object? value, Type t, object? parameter, CultureInfo c)
    {
        var attiva  = value     as string ?? "";
        var target  = parameter as string ?? "";
        var isActive = attiva.Equals(target, StringComparison.OrdinalIgnoreCase);

        var chiave = isActive ? "NavButtonActiveStyle" : "NavButtonStyle";
        return _owner.TryFindResource(chiave) ?? new Style();
    }

    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
