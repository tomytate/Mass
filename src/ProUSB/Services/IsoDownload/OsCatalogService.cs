using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ProUSB.Domain;
using ProUSB.Services.Logging;

namespace ProUSB.Services.IsoDownload;

public class OsCatalogService {
    private readonly FileLogger _logger;
    private OsCatalog? _catalog;
    private readonly JsonSerializerOptions _jsonOptions;

    public OsCatalogService(FileLogger logger) {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
    }

    public async Task<List<OsInfo>> GetAllOperatingSystemsAsync() {
        if (_catalog == null) {
            await LoadCatalogAsync();
        }
        return _catalog?.OperatingSystems ?? new List<OsInfo>();
    }

    public async Task<List<OsInfo>> GetOperatingSystemsByCategoryAsync(string category) {
        var allOS = await GetAllOperatingSystemsAsync();
        return allOS.Where(os => os.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public async Task<List<OsInfo>> SearchOperatingSystemsAsync(string query) {
        var allOS = await GetAllOperatingSystemsAsync();
        return allOS.Where(os => 
            os.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            os.Vendor.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            os.Version.Contains(query, StringComparison.OrdinalIgnoreCase)
        ).ToList();
    }

    public async Task<OsInfo?> GetOperatingSystemByIdAsync(string id) {
        var allOS = await GetAllOperatingSystemsAsync();
        return allOS.FirstOrDefault(os => os.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public List<string> GetCategories() {
        return new List<string> { "All", "Windows", "Linux", "BSD" };
    }

    private async Task LoadCatalogAsync() {
        try {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ProUSBMediaSuite.Resources.os_catalog.json";

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            
            if (stream == null) {
                
                var catalogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "os_catalog.json");
                if (File.Exists(catalogPath)) {
                    await using var fileStream = File.OpenRead(catalogPath);
                    _catalog = await JsonSerializer.DeserializeAsync<OsCatalog>(fileStream, _jsonOptions);
                } else {
                    _logger.Error($"OS catalog not found: {resourceName} or {catalogPath}");
                    _catalog = new OsCatalog { OperatingSystems = new List<OsInfo>() };
                    return;
                }
            } else {
                _catalog = await JsonSerializer.DeserializeAsync<OsCatalog>(stream, _jsonOptions);
            }

            _logger.Info($"Loaded OS catalog with {_catalog?.OperatingSystems.Count ?? 0} operating systems");
        } catch (Exception ex) {
            _logger.Error($"Failed to load OS catalog: {ex.Message}", ex);
            _catalog = new OsCatalog { OperatingSystems = new List<OsInfo>() };
        }
    }
}



