namespace Mass.Core.Devices;

public interface IIsoPatcher
{
    Task<PatchResult> PatchIsoAsync(PatchRequest request, CancellationToken ct = default);
    Task<IsoInfo> InspectIsoAsync(string isoPath, CancellationToken ct = default);
}

public record PatchRequest(
    string SourceIsoPath,
    string OutputIsoPath,
    List<PatchOperation> Operations
);

public record PatchOperation(string Type, Dictionary<string, object> Parameters);

public record PatchResult(bool Success, string OutputPath, List<string> Changes);

public record IsoInfo(
    string Label, 
    long SizeBytes, 
    string Format, 
    bool IsBootable, 
    List<string> BootMethods
);
