using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.ComponentModel;
namespace ProUSB.UI;
public class ViewLocator : IDataTemplate {
    public Control? Build(object? d) {
        if (d is null) return null;
        var t = Type.GetType(d.GetType().FullName!.Replace("ViewModel", "View"));
        return t!=null ? (Control)Activator.CreateInstance(t)! : new TextBlock { Text="Missing View" };
    }
    public bool Match(object? d) => d is ObservableObject;
}
