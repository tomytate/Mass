using System;
using System.Threading.Tasks;
using NLua;

namespace Mass.Core.Scripting;

public class LuaScriptingService : IScriptingService, IDisposable
{
    private Lua _lua;

    public LuaScriptingService()
    {
        _lua = new Lua();
        _lua.LoadCLRPackage();
    }

    public void RegisterObject(string name, object obj)
    {
        _lua[name] = obj;
    }

    public Task<object?> ExecuteAsync(string code)
    {
        return Task.Run(() =>
        {
            try
            {
                var result = _lua.DoString(code);
                if (result != null && result.Length > 0)
                {
                    return result[0];
                }
                return null;
            }
            catch (Exception ex)
            {
                return (object?)ex.Message;
            }
        });
    }

    public void Reset()
    {
        _lua.Dispose();
        _lua = new Lua();
        _lua.LoadCLRPackage();
    }

    public void Dispose()
    {
        _lua?.Dispose();
    }
}
