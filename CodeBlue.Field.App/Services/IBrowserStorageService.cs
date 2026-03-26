namespace CodeBlue.Field.App.Services;

public interface IBrowserStorageService
{
    ValueTask<string?> GetStringAsync(string key);
    ValueTask SetStringAsync(string key, string value);
    ValueTask RemoveStringAsync(string key);
}
