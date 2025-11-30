using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Mass.Launcher.Views;

public partial class ConsentDialogView : UserControl
{
    public ConsentDialogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
