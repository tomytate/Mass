using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
namespace ProUSB.UI.Views;
public partial class ConfirmationDialog : Window {
    public ConfirmationDialog(){InitializeComponent();}
    private void InitializeComponent()=>AvaloniaXamlLoader.Load(this);
    public static ConfirmationDialog Create(string n, long s, string id){
        var d=new ConfirmationDialog();
        d.FindControl<TextBlock>("N")!.Text=n;
        d.FindControl<TextBlock>("S")!.Text=$"{s/1024.0/1024.0/1024.0:F2} GB";
        return d;
    }
    private void OnConfirmClick(object s,RoutedEventArgs e)=>Close(true);
    private void OnCancelClick(object s,RoutedEventArgs e)=>Close(false);
}
