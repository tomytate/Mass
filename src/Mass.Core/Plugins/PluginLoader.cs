using System.Reflection;
using System.Runtime.Loader;
using Mass.Core.Abstractions;
using Mass.Spec.Contracts.Plugins;

namespace Mass.Core.Plugins;

public class PluginLoader : IPluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = new();

    public IPlugin? LoadPlugin(string pluginPath, PluginManifest manifest)
    {
        var assemblyPath = Path.Combine(pluginPath, manifest.EntryAssembly);
        
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}");
        }

        var loadContext = new PluginLoadContext(assemblyPath);
        _loadContexts[manifest.Id] = loadContext;

        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        var pluginType = assembly.GetType(manifest.EntryType);

        if (pluginType == null)
        {
            throw new TypeLoadException($"Plugin type not found: {manifest.EntryType}");
        }

        if (!typeof(IPlugin).IsAssignableFrom(pluginType))
        {
            throw new InvalidOperationException($"Type {manifest.EntryType} does not implement IPlugin");
        }

        var plugin = Activator.CreateInstance(pluginType) as IPlugin;
        // We can't set the Manifest property on the interface if it's read-only without a setter or constructor injection.
        // Assuming the plugin implementation handles its own manifest or we need to pass it.
        // For now, let's assume the plugin implementation might need to be initialized with it, 
        // but the interface only has a getter. 
        // Let's modify the interface or the loader to handle this.
        // Actually, usually the plugin knows its own metadata, OR we inject it.
        // Given the previous code, let's assume we just return the instance.
        
        return plugin;
    }

    public void UnloadPlugin(string pluginId)
    {
        if (_loadContexts.TryGetValue(pluginId, out var context))
        {
            context.Unload();
            _loadContexts.Remove(pluginId);
        }
    }

    private class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
