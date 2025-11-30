using System.Reflection;
using System.Runtime.Loader;
using Mass.Core.Abstractions;

namespace Mass.Core.Plugins;

public class PluginLoader
{
    private readonly Dictionary<string, AssemblyLoadContext> _loadContexts = new();

    public IModule? LoadPlugin(string pluginPath, PluginManifest manifest)
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

        if (!typeof(IModule).IsAssignableFrom(pluginType))
        {
            throw new InvalidOperationException($"Type {manifest.EntryType} does not implement IModule");
        }

        return Activator.CreateInstance(pluginType) as IModule;
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
