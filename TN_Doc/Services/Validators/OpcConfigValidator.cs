using System;
using TN.DocData;

namespace TN_Doc.Services.Validators;

/// <summary>
/// Валидатор конфигурации OPC
/// </summary>
public class OpcConfigValidator
{
    /// <summary>
    /// Валидация настроек OPC подключения
    /// </summary>
    /// <param name="opcSettings">Настройки OPC</param>
    /// <param name="result">Результат валидации</param>
    /// <param name="prefix">Префикс для сообщений об ошибках</param>
    public void Validate(OpcConnectionSettings opcSettings, ValidationResult result, string prefix = "")
    {
        if (opcSettings == null)
        {
            return;
        }

        switch (opcSettings.Type)
        {
            case OpcType.DA:
                ValidateDaSettings(opcSettings.DaSettings, result, prefix);
                break;
            case OpcType.UA:
                ValidateUaSettings(opcSettings.UaSettings, result, prefix);
                break;
            default:
                result.AddError($"{prefix}Неизвестный тип OPC: {opcSettings.Type}");
                break;
        }
    }

    private void ValidateDaSettings(OpcDaSettings daSettings, ValidationResult result, string prefix)
    {
        if (daSettings == null)
        {
            result.AddError($"{prefix}Не указаны настройки OPC DA");
            return;
        }

        if (string.IsNullOrWhiteSpace(daSettings.Host))
        {
            result.AddError($"{prefix}Не указан хост OPC DA");
        }

        if (string.IsNullOrWhiteSpace(daSettings.ProgId))
        {
            result.AddError($"{prefix}Не указан ProgId OPC DA");
        }

        if (daSettings.UpdateRate <= 0)
        {
            result.AddError($"{prefix}UpdateRate OPC DA должен быть больше 0");
        }
    }

    private void ValidateUaSettings(OpcUaSettings uaSettings, ValidationResult result, string prefix)
    {
        if (uaSettings == null)
        {
            result.AddError($"{prefix}Не указаны настройки OPC UA");
            return;
        }

        if (string.IsNullOrWhiteSpace(uaSettings.ConfigFilename))
        {
            result.AddError($"{prefix}Не указан файл конфигурации OPC UA");
        }

        if (uaSettings.UpdateRate <= 0)
        {
            result.AddError($"{prefix}UpdateRate OPC UA должен быть больше 0");
        }
    }
}
