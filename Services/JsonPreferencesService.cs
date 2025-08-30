// Copyright (c) 2025 SDSC0623. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Tools.Helpers;
using Tools.Services.IServices;

namespace Tools.Services;

public class PreferencesException(string message) : Exception(message);

public class JsonPreferencesService : IPreferencesService {
    // 配置文件储存路径
    private static readonly string PrefsFilePath = Path.Combine(GlobalSettings.AppDataDirectory, "Preferences.json");

    // 配置信息字典
    private static readonly Dictionary<string, object?> Preferences = new();

    // 锁对象
    private static readonly object Lock = new();

    // 提示信息
    private readonly SnackbarServiceHelper _snackbarService;

    // Logger
    private readonly ILogger _logger;

    public JsonPreferencesService(SnackbarServiceHelper snackbarService, ILogger logger) {
        _snackbarService = snackbarService;
        _logger = logger;
        Init();
    }

    private void Init() {
        EnsureDirectoryExists();
        LoadPreferences();
    }

    private void EnsureDirectoryExists() {
        try {
            var dir = Path.GetDirectoryName(PrefsFilePath);
            if (dir != null && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        } catch (Exception ex) {
            _logger.Error("数据加载错误。创建目录失败: {ExMessage}", ex.Message);
            _snackbarService.ShowError("数据加载错误", $"创建目录失败: {ex.Message}");
        }
    }

    private void LoadPreferences() {
        lock (Lock) {
            try {
                if (!File.Exists(PrefsFilePath)) {
                    Preferences.Clear();
                    return;
                }

                var json = File.ReadAllText(PrefsFilePath);

                var jObject = JsonConvert.DeserializeObject<JObject>(json);
                if (jObject is null) {
                    _snackbarService.ShowError("数据加载错误", "配置文件内容为空");
                    _logger.Error("数据加载错误。配置文件内容为空");
                    return;
                }

                Preferences.Clear();
                foreach (var property in jObject.Properties()) {
                    Preferences[property.Name] = property.Value.ToObject<object>()!;
                }
            } catch (Exception ex) {
                _logger.Error("数据加载错误加载偏好设置失败: {ExMessage}", ex.Message);
            }
        }
    }

    public T? Get<T>(string key, T? defaultValue = default) {
        lock (Lock) {
            if (!Preferences.TryGetValue(key, out var value)) {
                return defaultValue;
            }

            try {
                return HandleValueConversion<T>(value);
            } catch (Exception ex) {
                _logger.Error(ex.Message);
                throw new PreferencesException(ex.Message);
            }
        }
    }

    private T? HandleValueConversion<T>(object? value) {
        // 1. 处理 null 值
        if (value == null) {
            return default;
        }

        // 2. 如果类型恰好完全匹配，这是最快捷的路径
        if (value is T typedValue) {
            return typedValue;
        }

        // 3. 如果存储的值本身就是 JToken（从文件加载后就是），
        // 直接使用最有效的 ToObject<T>() 方法。
        if (value is JToken jToken) {
            try {
                return jToken.ToObject<T>();
            } catch (JsonException ex) {
                throw new PreferencesException($"Json 转换失败，目标类型为: {typeof(T).Name}，错误信息: {ex.Message}");
            }
        }

        // 4. 万能后备方案：通过 JSON 序列化再反序列化进行转换
        try {
            // 先将原始值序列化为 JSON 字符串
            var serializedValue = JsonConvert.SerializeObject(value);
            // 再将 JSON 字符串反序列化为目标类型 T
            return JsonConvert.DeserializeObject<T>(serializedValue);
        } catch (JsonException ex) {
            throw new PreferencesException(
                $"Json 转换失败，数据类型为: {value.GetType().Name}，目标类型为: {typeof(T).Name}，错误信息: {ex.Message}");
        }
    }

    public async Task Set<T>(string key, T? value) {
        lock (Lock) {
            Preferences[key] = value;
        }

        await Save();
    }

    public bool Contains(string key) {
        lock (Lock) {
            return Preferences.ContainsKey(key);
        }
    }

    public async Task Remove(string key) {
        lock (Lock) {
            Preferences.Remove(key);
        }

        await Save();
    }

    public async Task Save() {
        await Task.Run(() => {
            lock (Lock) {
                try {
                    // 直接在锁内完成序列化和写入
                    var json = JsonConvert.SerializeObject(Preferences, Formatting.Indented);

                    // 使用原子性写入方法
                    WriteAllTextAtomic(PrefsFilePath, json);
                } catch (Exception ex) {
                    _logger.Error("保存偏好设置失败: {ExMessage}", ex.Message);
                    throw new PreferencesException($"保存文件时发生错误: {ex.Message}");
                }
            }
        });
    }

    private void WriteAllTextAtomic(string path, string contents) {
        var directory = Path.GetDirectoryName(path);
        var tempFileName = $"{Path.GetFileNameWithoutExtension(path)}.{Guid.NewGuid()}.tmp";
        var tempFilePath = Path.Combine(directory!, tempFileName);

        try {
            // 写入临时文件
            File.WriteAllText(tempFilePath, contents);
            // 原子性替换
            File.Move(tempFilePath, path, true);
        } catch (Exception ex) {
            _logger.Error("写入文件失败: {ExMessage}", ex.Message);
            throw new PreferencesException($"保存文件时发生错误: {ex.Message}");
        } finally {
            // 清理临时文件（如果存在）
            if (File.Exists(tempFilePath)) {
                try {
                    File.Delete(tempFilePath);
                } catch (Exception ex) {
                    _logger.Error("删除临时文件失败: {ExMessage}", ex.Message);
                }
            }
        }
    }
}