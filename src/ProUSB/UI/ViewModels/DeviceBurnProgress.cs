using CommunityToolkit.Mvvm.ComponentModel;

namespace ProUSB.UI.ViewModels;

public partial class DeviceBurnProgress : ObservableObject {
    [ObservableProperty] string deviceName = "";
    [ObservableProperty] string deviceId = "";
    [ObservableProperty] double progress;
    [ObservableProperty] string status = "Queued";
    [ObservableProperty] bool isComplete;
    [ObservableProperty] bool hasFailed;
}

