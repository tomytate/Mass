using Mass.Core.Abstractions;
using Mass.Spec.Contracts.Plugins;

namespace Mass.Core.Plugins;

public interface IPluginLoader
{
    IPlugin? LoadPlugin(string pluginPath, PluginManifest manifest);
    void UnloadPlugin(string pluginId);
}
