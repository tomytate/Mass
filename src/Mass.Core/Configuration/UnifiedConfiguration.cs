namespace Mass.Core.Configuration;

public class UnifiedConfiguration
{
    public AppSettings App { get; set; } = new();
    public PxeSettings Pxe { get; set; } = new();
    public UsbSettings Usb { get; set; } = new();
    public WorkflowSettings Workflows { get; set; } = new();
}

public class PxeSettings
{
    public string RootPath { get; set; } = "C:\\Mass\\PXE";
    public int TftpPort { get; set; } = 69;
    public int HttpPort { get; set; } = 8080;
    public bool EnableDhcp { get; set; } = true;
}

public class UsbSettings
{
    public bool VerifyAfterBurn { get; set; } = true;
    public bool EjectAfterBurn { get; set; } = false;
    public string DefaultPartitionScheme { get; set; } = "GPT";
}

public class WorkflowSettings
{
    public string ScriptsPath { get; set; } = "C:\\Mass\\Scripts";
    public bool AutoRetry { get; set; } = false;
}
