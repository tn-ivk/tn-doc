using System;
using System.Collections.Generic;
using System.Linq;
using TN.DocData;

namespace TN_Doc.Services.Validators;

/// <summary>
/// Валидатор конфигурации базы данных
/// </summary>
public class DbConfigValidator
{
    /// <summary>
    /// Валидация строки подключения к базе данных
    /// </summary>
    /// <param name="connectionStrings">Список строк подключения</param>
    /// <param name="result">Результат валидации</param>
    /// <param name="prefix">Префикс для сообщений об ошибках</param>
    public void Validate(List<DBConnectionString> connectionStrings, ValidationResult result, string prefix = "")
    {
        if (connectionStrings == null || !connectionStrings.Any())
        {
            result.AddWarning($"{prefix}Не указаны строки подключения к БД");
            return;
        }

        foreach (var connStr in connectionStrings)
        {
            ValidateConnectionString(connStr, result, prefix);
        }
    }

    private void ValidateConnectionString(DBConnectionString connStr, ValidationResult result, string prefix)
    {
        if (connStr == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(connStr.Server))
        {
            result.AddError($"{prefix}Не указан сервер БД");
        }

        if (string.IsNullOrWhiteSpace(connStr.Database))
        {
            result.AddError($"{prefix}Не указано имя базы данных");
        }

        if (connStr.ConnectionTimeout < 0)
        {
            result.AddError($"{prefix}Таймаут БД не может быть отрицательным");
        }
    }
}
