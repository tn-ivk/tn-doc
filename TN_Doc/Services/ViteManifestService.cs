using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для чтения Vite manifest.json и получения путей к собранным файлам с хэшами.
/// Используется в Razor views для подключения Vue приложений.
/// </summary>
public interface IViteManifestService
{
    /// <summary>
    /// Получает путь к entry файлу из manifest.
    /// </summary>
    /// <param name="appName">Имя приложения (configurator, statusbar, document-editor)</param>
    /// <returns>Относительный путь к JS файлу или null если не найден</returns>
    string? GetEntryFile(string appName);

    /// <summary>
    /// Получает путь к CSS файлу из manifest.
    /// </summary>
    /// <param name="appName">Имя приложения</param>
    /// <returns>Относительный путь к CSS файлу или null если не найден</returns>
    string? GetCssFile(string appName);

    /// <summary>
    /// Перезагружает manifest (для hot reload в development).
    /// </summary>
    void Reload(string appName);
}

public class ViteManifestService : IViteManifestService
{
    private readonly IWebHostEnvironment _env;
    private readonly Dictionary<string, ViteManifest> _manifests = new();
    private readonly object _lock = new();

    public ViteManifestService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public string? GetEntryFile(string appName)
    {
        var manifest = GetManifest(appName);
        if (manifest == null) return null;

        // Vite использует относительный путь к исходному файлу как ключ
        var entryKey = "src/main.ts";
        if (manifest.TryGetValue(entryKey, out var entry))
        {
            return entry.File;
        }

        return null;
    }

    public string? GetCssFile(string appName)
    {
        var manifest = GetManifest(appName);
        if (manifest == null) return null;

        var entryKey = "src/main.ts";
        if (manifest.TryGetValue(entryKey, out var entry) && entry.Css?.Length > 0)
        {
            return entry.Css[0];
        }

        return null;
    }

    public void Reload(string appName)
    {
        lock (_lock)
        {
            _manifests.Remove(appName);
        }
    }

    private ViteManifest? GetManifest(string appName)
    {
        lock (_lock)
        {
            if (_manifests.TryGetValue(appName, out var cached))
            {
                return cached;
            }

            var manifest = LoadManifest(appName);
            _manifests[appName] = manifest;
            return manifest;
        }
    }

    private ViteManifest? LoadManifest(string appName)
    {
        // Путь к manifest: wwwroot/{appName}/.vite/manifest.json
        var manifestPath = Path.Combine(_env.WebRootPath, appName, ".vite", "manifest.json");

        if (!File.Exists(manifestPath))
        {
            // В development режиме manifest может отсутствовать
            return null;
        }

        try
        {
            var json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<ViteManifest>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }
}

/// <summary>
/// Структура manifest.json от Vite
/// </summary>
public class ViteManifest : Dictionary<string, ViteManifestEntry>
{
}

public class ViteManifestEntry
{
    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("src")]
    public string? Src { get; set; }

    [JsonPropertyName("isEntry")]
    public bool IsEntry { get; set; }

    [JsonPropertyName("css")]
    public string[]? Css { get; set; }

    [JsonPropertyName("imports")]
    public string[]? Imports { get; set; }
}
