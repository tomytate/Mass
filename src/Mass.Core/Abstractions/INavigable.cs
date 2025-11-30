namespace Mass.Core.Abstractions;

public interface INavigable
{
    void OnNavigatedTo(object? parameter);
    void OnNavigatedFrom();
}
