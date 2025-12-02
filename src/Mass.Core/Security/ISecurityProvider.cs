using System.Security.Claims;

namespace Mass.Core.Security;

public interface ISecurityProvider
{
    Task<ClaimsPrincipal?> GetCurrentUserAsync();
    Task<bool> LoginAsync(string username, string password);
    Task LogoutAsync();
    Task<bool> HasPermissionAsync(string permission);
}
