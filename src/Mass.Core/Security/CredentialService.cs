using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mass.Core.Security;

public class CredentialService : ICredentialService
{
    private readonly string _credentialPath;
    private List<StoredCredential> _credentials = new();

    public CredentialService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MassSuite");
        Directory.CreateDirectory(appData);
        _credentialPath = Path.Combine(appData, "credentials.dat");
        LoadCredentials();
    }

    public void StoreCredential(string id, string name, string username, SecureString password)
    {
        var plainPassword = SecureStringToString(password);
        var encryptedPassword = EncryptString(plainPassword);

        var existing = _credentials.FirstOrDefault(c => c.Id == id);
        if (existing != null)
        {
            existing.Name = name;
            existing.Username = username;
            existing.EncryptedPassword = encryptedPassword;
        }
        else
        {
            _credentials.Add(new StoredCredential
            {
                Id = id,
                Name = name,
                Username = username,
                EncryptedPassword = encryptedPassword,
                CreatedAt = DateTime.Now
            });
        }

        SaveCredentials();
    }

    public StoredCredential? GetCredential(string id)
    {
        var credential = _credentials.FirstOrDefault(c => c.Id == id);
        if (credential != null)
        {
            credential.LastUsed = DateTime.Now;
            SaveCredentials();
        }
        return credential;
    }

    public SecureString? GetPassword(string id)
    {
        var credential = GetCredential(id);
        if (credential == null)
            return null;

        try
        {
            var decrypted = DecryptString(credential.EncryptedPassword);
            return StringToSecureString(decrypted);
        }
        catch
        {
            return null;
        }
    }

    public IEnumerable<StoredCredential> GetAllCredentials()
    {
        return _credentials.Select(c => new StoredCredential
        {
            Id = c.Id,
            Name = c.Name,
            Username = c.Username,
            CreatedAt = c.CreatedAt,
            LastUsed = c.LastUsed
        });
    }

    public void DeleteCredential(string id)
    {
        _credentials.RemoveAll(c => c.Id == id);
        SaveCredentials();
    }

    public void ClearAll()
    {
        _credentials.Clear();
        SaveCredentials();
    }

    private void LoadCredentials()
    {
        try
        {
            if (File.Exists(_credentialPath))
            {
                var json = File.ReadAllText(_credentialPath);
                _credentials = JsonSerializer.Deserialize<List<StoredCredential>>(json) ?? new List<StoredCredential>();
            }
        }
        catch
        {
            _credentials = new List<StoredCredential>();
        }
    }

    private void SaveCredentials()
    {
        try
        {
            var json = JsonSerializer.Serialize(_credentials);
            File.WriteAllText(_credentialPath, json);
        }
        catch { }
    }

    private static string EncryptString(string plainText)
    {
        if (OperatingSystem.IsWindows())
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
    }

    private static string DecryptString(string encryptedText)
    {
        if (OperatingSystem.IsWindows())
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
    }

    private static string SecureStringToString(SecureString secureString)
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.SecureStringToGlobalAllocUnicode(secureString);
            return Marshal.PtrToStringUni(ptr) ?? string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero)
                Marshal.ZeroFreeGlobalAllocUnicode(ptr);
        }
    }

    private static SecureString StringToSecureString(string str)
    {
        var secureString = new SecureString();
        foreach (char c in str)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        return secureString;
    }
}
