using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace VetaleBrowser.Export;

/// <summary>Тільки дизайн. Жодної логіки браузера.</summary>
public partial class VetaleDesignWindow : Window
{
    public VetaleDesignWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
