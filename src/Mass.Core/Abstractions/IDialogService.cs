using System.Threading.Tasks;

namespace Mass.Core.Abstractions;

public interface IDialogService
{
    Task ShowErrorDialogAsync(string title, string message, bool canRetry = false);
    Task ShowMessageDialogAsync(string title, string message);
}
