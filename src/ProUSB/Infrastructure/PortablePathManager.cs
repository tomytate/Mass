using System;
using System.IO;
using System.Reflection;

namespace ProUSB.Infrastructure;

public class PortablePathManager {
    private readonly bool _isPortableMode;
    private readonly string _baseDataDirectory;

    public PortablePathManager() {
        _isPortableMode = DetectPortableMode();
        _baseDataDirectory = _isPortableMode ? GetPortableDataDirectory() : GetInstalledDataDirectory();
    }

    public bool IsPortableMode() => _isPortableMode;

    public string GetDataDirectory() => _baseDataDirectory;

    public string GetLogsDirectory() => Path.Combine(_baseDataDirectory, "Logs");

    public string GetProfilesDirectory() => Path.Combine(_baseDataDirectory, "Profiles");

    public string GetConfigDirectory() => Path.Combine(_baseDataDirectory, "Config");

    public void EnsureDirectoriesExist() {
        Directory.CreateDirectory(GetLogsDirectory());
        Directory.CreateDirectory(GetProfilesDirectory());
        Directory.CreateDirectory(GetConfigDirectory());
    }

    private bool DetectPortableMode() {
        try {
            string exeDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(exeDir)) {
                return false;
            }

            string portableMarker = Path.Combine(exeDir, "portable.txt");
            return File.Exists(portableMarker);
        } catch {
            return false;
        }
    }

    private string GetPortableDataDirectory() {
        string exeDir = AppContext.BaseDirectory;
        return Path.Combine(exeDir, "Data");
    }

    private string GetInstalledDataDirectory() {
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appDataPath, "ProUSBMediaSuite");
    }
}

