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

        // 1. Try standard Type.GetType (for same assembly)
        var type = Type.GetType(name);

        // 2. If not found, search all loaded assemblies (for plugins/ProUSB)
        if (type == null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(name);
                if (type != null) break;
            }
        }

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
