using System.Collections.Generic;

namespace TN_Doc.Services;

/// <summary>
/// Результат валидации конфигурации
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Флаг успешной валидации
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Список ошибок валидации
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Список предупреждений
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Создать успешный результат валидации
    /// </summary>
    public static ValidationResult Success()
    {
        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// Создать неуспешный результат валидации с ошибками
    /// </summary>
    public static ValidationResult Failure(params string[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = new List<string>(errors)
        };
    }

    /// <summary>
    /// Добавить ошибку
    /// </summary>
    public void AddError(string error)
    {
        Errors.Add(error);
        IsValid = false;
    }

    /// <summary>
    /// Добавить предупреждение
    /// </summary>
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}
