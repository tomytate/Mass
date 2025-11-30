using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Mass.Launcher.Views;

public partial class PluginsView : UserControl
{
    public PluginsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
