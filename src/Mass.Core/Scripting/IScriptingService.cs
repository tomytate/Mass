using System.Threading.Tasks;

namespace Mass.Core.Scripting;

public interface IScriptingService
{
    void RegisterObject(string name, object obj);
    Task<object?> ExecuteAsync(string code);
    void Reset();
}
