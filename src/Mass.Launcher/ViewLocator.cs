using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Mass.Core.UI;

namespace Mass.Launcher;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2057:Type.GetType", Justification = "ViewModel to View mapping is dynamic.")]
        Type? GetViewType(string typeName) => Type.GetType(typeName);

        var type = GetViewType(name);

        if (type != null)
        {
            [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2072:Activator.CreateInstance", Justification = "Views have parameterless constructors by convention.")]
            object? CreateView() => Activator.CreateInstance(type);
            return (Control)CreateView()!;
        }
        
        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
