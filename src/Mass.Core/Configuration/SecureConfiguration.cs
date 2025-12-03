namespace Mass.Core.Configuration;

public static class SecureConfiguration
{
    public static string GetSecret(string key, string? fallback = null)
    {
        var envKey = key.Replace(":", "__").Replace(".", "__").ToUpperInvariant();
        
        var value = Environment.GetEnvironmentVariable(envKey);
        if (!string.IsNullOrEmpty(value))
            return value;
        
        var massEnvKey = $"MASS_{envKey}";
        value = Environment.GetEnvironmentVariable(massEnvKey);
        if (!string.IsNullOrEmpty(value))
            return value;
        
        if (fallback != null)
            return fallback;
        
        throw new InvalidOperationException(
            $"Required secret '{key}' not found. Set environment variable '{massEnvKey}' or '{envKey}'.");
    }

    public static string? GetSecretOrDefault(string key, string? defaultValue = null)
    {
        try
        {
            return GetSecret(key, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }

    public static bool IsProduction => 
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production" ||
        Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Production";

    public static void ValidateProductionSecrets(params string[] requiredKeys)
    {
        if (!IsProduction) return;
        
        var missing = requiredKeys
            .Where(k => string.IsNullOrEmpty(GetSecretOrDefault(k)))
            .ToList();
        
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Production deployment requires these secrets: {string.Join(", ", missing)}");
        }
    }
}
