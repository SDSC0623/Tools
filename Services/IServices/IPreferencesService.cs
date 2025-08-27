// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace Tools.Services.IServices;

public interface IPreferencesService {
    T? Get<T>(string key, T? defaultValue = default);
    Task Set<T>(string key, T? value);
    bool Contains(string key);
    Task Remove(string key);
    Task Save();
}