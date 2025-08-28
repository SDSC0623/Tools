// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

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