// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;
using System.Security.Principal;

// ReSharper disable MemberCanBePrivate.Global

namespace Tools.Helpers;

public static class GlobalSettings {
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

    public static bool IsAdmin { get; } = GetElevated();

    private static bool GetElevated() {
        using var identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}