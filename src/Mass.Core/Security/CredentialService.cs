using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mass.Core.Security;

public class CredentialService : ICredentialService
{
    private readonly string _credentialPath;
    private readonly ILogger<CredentialService>? _logger;
    private List<StoredCredential> _credentials = [];

    public CredentialService(ILogger<CredentialService>? logger = null)
    {
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load credentials from {Path}. Starting with empty credential store.", _credentialPath);
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
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save credentials to {Path}", _credentialPath);
        }
    }

    private static string EncryptString(string plainText)
    {
        if (OperatingSystem.IsWindows())
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        
        // Cross-platform: Use AES-256 with machine-derived key
        return EncryptWithAes(plainText);
    }

    private static string DecryptString(string encryptedText)
    {
        if (OperatingSystem.IsWindows())
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        
        // Cross-platform: Use AES-256 with machine-derived key
        return DecryptWithAes(encryptedText);
    }

    private static byte[] GetMachineKey()
    {
        // Derive a machine-specific key from hostname + user
        var machineInfo = $"{Environment.MachineName}|{Environment.UserName}|MassSuite-v1";
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo));
    }

    private static string EncryptWithAes(string plainText)
    {
        var key = GetMachineKey();
        var nonce = RandomNumberGenerator.GetBytes(AesGcm.NonceByteSizes.MaxSize); // 12 bytes
        var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = new byte[plainBytes.Length];

        using var aesGcm = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Format: nonce (12) | tag (16) | ciphertext
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string DecryptWithAes(string encryptedText)
    {
        var key = GetMachineKey();
        var fullData = Convert.FromBase64String(encryptedText);

        const int nonceSize = 12; // AesGcm.NonceByteSizes.MaxSize
        const int tagSize = 16;   // AesGcm.TagByteSizes.MaxSize

        var nonce = fullData.AsSpan(0, nonceSize);
        var tag = fullData.AsSpan(nonceSize, tagSize);
        var cipherBytes = fullData.AsSpan(nonceSize + tagSize);
        var plainBytes = new byte[cipherBytes.Length];

        using var aesGcm = new AesGcm(key, tagSize);
        aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
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
