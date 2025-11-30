using System.Security;

namespace Mass.Core.Security;

public class StoredCredential
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EncryptedPassword { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? LastUsed { get; set; }
}

public interface ICredentialService
{
    void StoreCredential(string id, string name, string username, SecureString password);
    StoredCredential? GetCredential(string id);
    SecureString? GetPassword(string id);
    IEnumerable<StoredCredential> GetAllCredentials();
    void DeleteCredential(string id);
    void ClearAll();
}
