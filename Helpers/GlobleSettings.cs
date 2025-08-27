using System.IO;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global

namespace Tools.Helpers;

public static class GlobleSettings {
    public const bool IsDebug =
#if DEBUG
        true;
#else
        false;
#endif
    public static string FullVersion { get; } = Assembly.GetEntryAssembly()
        ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion!;

    public static string Version => FullVersion.Split('+').First();

    public static string BaseDirectory => AppContext.BaseDirectory;

    public static string LogDirectory => Path.Combine(BaseDirectory, "Log");

    public static string AppDataDirectory => Path.Combine(BaseDirectory, "AppData");
}