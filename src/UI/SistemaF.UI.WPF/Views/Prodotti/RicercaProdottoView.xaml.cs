using System.Windows.Controls;

namespace SistemaF.UI.WPF.Views.Prodotti;

public partial class RicercaProdottoView : UserControl
{
    public RicercaProdottoView()
    {
        InitializeComponent();
        // Focus automatico sulla casella di ricerca all'apertura
        Loaded += (_, _) => SearchBox?.Focus();
    }
}
