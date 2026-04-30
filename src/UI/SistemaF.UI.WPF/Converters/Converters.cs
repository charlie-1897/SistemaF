using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SistemaF.UI.WPF.Converters;

// ── BoolToVisibilityConverter ─────────────────────────────────────────────────
[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => v is Visibility.Visible;
}

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => v is not Visibility.Visible;
}

// ── NullToVisibilityConverter ─────────────────────────────────────────────────
[ValueConversion(typeof(object), typeof(Visibility))]
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── GiacenzaToColorConverter — semaforo disponibilità ─────────────────────────
// Verde: disponibile, Arancio: sotto scorta minima, Rosso: esaurito
[ValueConversion(typeof(int), typeof(Brush))]
public sealed class GiacenzaToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        if (v is not int giacenza) return Brushes.Gray;
        return giacenza switch
        {
            > 10 => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),   // verde
            > 0  => new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),   // arancio
            _    => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26))    // rosso
        };
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── GiacenzaToIconConverter ───────────────────────────────────────────────────
[ValueConversion(typeof(int), typeof(string))]
public sealed class GiacenzaToIconConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is int g ? g switch
        {
            > 10 => "●",   // disponibile
            > 0  => "◐",   // bassa giacenza
            _    => "○"    // esaurito
        } : "○";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── ClasseToColorConverter — colore per classe SSN ───────────────────────────
[ValueConversion(typeof(string), typeof(Brush))]
public sealed class ClasseToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        if (v is not string classe) return Brushes.Gray;
        return classe switch
        {
            "A"   => new SolidColorBrush(Color.FromRgb(0x16, 0xA3, 0x4A)),  // verde SSN
            "C"   => new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8)),  // blu
            "H"   => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),  // rosso ospedaliero
            _     => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))   // grigio OTC/SOP
        };
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── PrezzoConverter — formatta il prezzo in euro ──────────────────────────────
[ValueConversion(typeof(decimal), typeof(string))]
public sealed class PrezzoConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
        => v is decimal d ? d.ToString("€ #,##0.00", CultureInfo.GetCultureInfo("it-IT")) : "—";
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}

// ── DataScadenzaToColorConverter ──────────────────────────────────────────────
[ValueConversion(typeof(DateOnly), typeof(Brush))]
public sealed class DataScadenzaToColorConverter : IValueConverter
{
    public object Convert(object? v, Type t, object? p, CultureInfo c)
    {
        if (v is not DateOnly data) return Brushes.Transparent;
        var giorni = (data.ToDateTime(TimeOnly.MinValue) - DateTime.Today).TotalDays;
        return giorni switch
        {
            < 0   => new SolidColorBrush(Color.FromRgb(0xDC, 0x26, 0x26)),  // scaduto
            < 90  => new SolidColorBrush(Color.FromRgb(0xD9, 0x77, 0x06)),  // in scadenza
            _     => Brushes.Transparent
        };
    }
    public object ConvertBack(object? v, Type t, object? p, CultureInfo c)
        => throw new NotSupportedException();
}
