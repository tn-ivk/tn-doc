using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using TN.DocData;
using TN_Doc.Services.Validators;
using TN_DocGeneral.Services;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для работы с конфигурацией приложения
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IAppConfigService _appConfigService;
    private readonly IConfigurationCacheService _configCacheService;
    private readonly IWebHostEnvironment _environment;
    private readonly OpcConfigValidator _opcValidator;
    private readonly DbConfigValidator _dbValidator;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IAppConfigService appConfigService,
        IConfigurationCacheService configCacheService,
        IWebHostEnvironment environment,
        ILogger<ConfigurationService> logger)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _configCacheService = configCacheService ?? throw new ArgumentNullException(nameof(configCacheService));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _opcValidator = new OpcConfigValidator();
        _dbValidator = new DbConfigValidator();
    }

    /// <summary>
    /// Получить текущую конфигурацию приложения
    /// </summary>
    public CfgApp GetConfiguration()
    {
        _logger.LogDebug("Получение текущей конфигурации приложения");
        return _appConfigService.GetAppCfg();
    }

    /// <summary>
    /// Сохранить конфигурацию приложения с логированием изменений
    /// </summary>
    public async Task<bool> SaveConfigurationAsync(CfgApp config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            // Получаем текущую конфигурацию для сравнения
            var currentConfig = _appConfigService.GetAppCfg();

            // Логируем изменения
            await LogConfigurationChangesAsync(currentConfig, config);

            // Копируем изменения в текущую конфигурацию
            UpdateConfiguration(currentConfig, config);

            // Сохраняем через AppConfigService
            var result = _appConfigService.SetAppCfg();

            if (result)
            {
                _logger.LogInformation("Конфигурация успешно сохранена");
            }
            else
            {
                _logger.LogError("Не удалось сохранить конфигурацию");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении конфигурации: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Валидация конфигурации приложения
    /// </summary>
    public Task<ValidationResult> ValidateConfigurationAsync(CfgApp config)
    {
        var result = new ValidationResult { IsValid = true };

        if (config == null)
        {
            result.AddError("Конфигурация не может быть пустой");
            return Task.FromResult(result);
        }

        // Валидация обязательных полей
        if (config.ExportDoc == null || string.IsNullOrWhiteSpace(config.ExportDoc.Path))
        {
            result.AddError("Не указан путь экспорта документов (ExportDoc.Path)");
        }

        // Валидация глобальных настроек OPC
        if (config.ArmOpcConnectionSettings != null)
        {
            _opcValidator.Validate(config.ArmOpcConnectionSettings, result, "Глобальные настройки OPC: ");
        }

        // Валидация устройств
        if (config.Devices != null && config.Devices.Any())
        {
            for (int i = 0; i < config.Devices.Count; i++)
            {
                var device = config.Devices[i];
                ValidateDevice(device, result, $"Устройство '{device.Name}': ");
            }
        }
        else
        {
            result.AddWarning("Не определены устройства");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Валидация отдельного устройства
    /// </summary>
    private void ValidateDevice(Device device, ValidationResult result, string prefix)
    {
        if (device == null)
        {
            return;
        }

        // Валидация имени устройства
        if (string.IsNullOrWhiteSpace(device.Name))
        {
            result.AddError($"{prefix}Не указано имя устройства");
        }

        // Валидация подключений к БД
        if (device.DBConnectionStrings != null && device.DBConnectionStrings.Any())
        {
            _dbValidator.Validate(device.DBConnectionStrings, result, prefix);
        }

        // Валидация настроек OPC
        if (device.OpcConnectionSettings != null)
        {
            _opcValidator.Validate(device.OpcConnectionSettings, result, prefix);
        }
    }

    /// <summary>
    /// Логирование изменений конфигурации
    /// </summary>
    private Task LogConfigurationChangesAsync(CfgApp oldConfig, CfgApp newConfig)
    {
        try
        {
            // Сравниваем общие настройки
            if (oldConfig.UseSecurityFeatures != newConfig.UseSecurityFeatures)
            {
                _logger.LogInformation("Изменено: UseSecurityFeatures: {Old} -> {New}",
                    oldConfig.UseSecurityFeatures, newConfig.UseSecurityFeatures);
            }

            if (oldConfig.ExportDoc?.Path != newConfig.ExportDoc?.Path)
            {
                _logger.LogInformation("Изменено: ExportDoc.Path: '{Old}' -> '{New}'",
                    oldConfig.ExportDoc?.Path ?? "null", newConfig.ExportDoc?.Path ?? "null");
            }

            // Логируем изменения в устройствах
            LogDeviceChanges(oldConfig.Devices, newConfig.Devices);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при логировании изменений конфигурации");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Логирование изменений в устройствах
    /// </summary>
    private void LogDeviceChanges(System.Collections.Generic.List<Device> oldDevices, System.Collections.Generic.List<Device> newDevices)
    {
        if (oldDevices == null || newDevices == null)
        {
            return;
        }

        foreach (var newDevice in newDevices)
        {
            var oldDevice = oldDevices.FirstOrDefault(d => d.IdDevice == newDevice.IdDevice);
            if (oldDevice == null)
            {
                _logger.LogInformation("Добавлено новое устройство: {DeviceName} (ID: {DeviceId})",
                    newDevice.Name, newDevice.IdDevice);
                continue;
            }

            // Сравниваем свойства устройства
            if (oldDevice.Use != newDevice.Use)
            {
                _logger.LogInformation("Устройство '{DeviceName}': Use изменено с {Old} на {New}",
                    newDevice.Name, oldDevice.Use, newDevice.Use);
            }

            if (oldDevice.Name != newDevice.Name)
            {
                _logger.LogInformation("Устройство ID {DeviceId}: Name изменено с '{Old}' на '{New}'",
                    newDevice.IdDevice, oldDevice.Name, newDevice.Name);
            }
        }

        // Проверяем удаленные устройства
        foreach (var oldDevice in oldDevices)
        {
            if (!newDevices.Any(d => d.IdDevice == oldDevice.IdDevice))
            {
                _logger.LogInformation("Удалено устройство: {DeviceName} (ID: {DeviceId})",
                    oldDevice.Name, oldDevice.IdDevice);
            }
        }
    }

    /// <summary>
    /// Обновление текущей конфигурации новыми значениями
    /// </summary>
    private void UpdateConfiguration(CfgApp current, CfgApp updated)
    {
        // Обновляем общие настройки
        current.UseSecurityFeatures = updated.UseSecurityFeatures;

        if (updated.ExportDoc != null)
        {
            current.ExportDoc = current.ExportDoc ?? new ExportDoc();
            current.ExportDoc.Path = updated.ExportDoc.Path;
        }

        if (updated.PrintSettings != null)
        {
            current.PrintSettings = current.PrintSettings ?? new PrintSettings();
            current.PrintSettings.LastSelectedPrinter = updated.PrintSettings.LastSelectedPrinter;
        }

        // Обновляем настройки OPC
        current.ArmOpcConnectionSettings = updated.ArmOpcConnectionSettings;

        // Обновляем настройки ELIS
        current.Elis = updated.Elis;

        // Обновляем устройства
        current.Devices = updated.Devices;
    }

    /// <summary>
    /// Загрузить содержимое конфигурационного файла документа
    /// </summary>
    public async Task<string> LoadDocumentConfigAsync(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentException("Путь к конфигурационному файлу не может быть пустым", nameof(configPath));
        }

        try
        {
            // Нормализуем путь (удаляем начальный слеш, если есть)
            var normalizedPath = configPath.TrimStart('/');

            // Получаем полный путь к файлу относительно корня приложения
            var fullPath = Path.Combine(_environment.ContentRootPath, normalizedPath);

            _logger.LogDebug("Загрузка конфигурации документа: {FilePath}", fullPath);

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Файл конфигурации не найден: {FilePath}", fullPath);
                throw new FileNotFoundException($"Файл конфигурации не найден: {configPath}");
            }

            // Читаем файл
            var content = await File.ReadAllTextAsync(fullPath, Encoding.UTF8);
            _logger.LogDebug("Конфигурация документа успешно загружена: {FilePath}", fullPath);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке конфигурации документа: {FilePath}", configPath);
            throw;
        }
    }

    /// <summary>
    /// Сохранить содержимое конфигурационного файла документа
    /// </summary>
    public async Task<bool> SaveDocumentConfigAsync(string configPath, string content)
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            throw new ArgumentException("Путь к конфигурационному файлу не может быть пустым", nameof(configPath));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Содержимое файла не может быть пустым", nameof(content));
        }

        try
        {
            // Нормализуем путь (удаляем начальный слеш, если есть)
            var normalizedPath = configPath.TrimStart('/');

            // Получаем полный путь к файлу относительно корня приложения
            var fullPath = Path.Combine(_environment.ContentRootPath, normalizedPath);

            _logger.LogInformation("Сохранение конфигурации документа: {FilePath}", fullPath);

            // Проверяем, существует ли директория
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogDebug("Создана директория: {Directory}", directory);
            }

            // Сохраняем файл
            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);

            // Очищаем кэш для этого файла
            _configCacheService.ClearCache(fullPath);
            _logger.LogDebug("Кэш конфигурации очищен для файла: {FilePath}", fullPath);

            _logger.LogInformation("Конфигурация документа успешно сохранена: {FilePath}", fullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении конфигурации документа: {FilePath}", configPath);
            return false;
        }
    }
}
