using System.Threading.Tasks;
using TN.DocData;

namespace TN_Doc.Services;

/// <summary>
/// Сервис для работы с конфигурацией приложения
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Получить текущую конфигурацию приложения
    /// </summary>
    /// <returns>Конфигурация приложения</returns>
    CfgApp GetConfiguration();

    /// <summary>
    /// Сохранить конфигурацию приложения
    /// </summary>
    /// <param name="config">Конфигурация для сохранения</param>
    /// <returns>true если сохранение успешно, иначе false</returns>
    Task<bool> SaveConfigurationAsync(CfgApp config);

    /// <summary>
    /// Валидация конфигурации приложения
    /// </summary>
    /// <param name="config">Конфигурация для валидации</param>
    /// <returns>Результат валидации</returns>
    Task<ValidationResult> ValidateConfigurationAsync(CfgApp config);

    /// <summary>
    /// Загрузить содержимое конфигурационного файла документа
    /// </summary>
    /// <param name="configPath">Путь к файлу конфигурации (относительный от корня приложения)</param>
    /// <returns>Содержимое файла в формате JSON</returns>
    Task<string> LoadDocumentConfigAsync(string configPath);

    /// <summary>
    /// Сохранить содержимое конфигурационного файла документа
    /// </summary>
    /// <param name="configPath">Путь к файлу конфигурации (относительный от корня приложения)</param>
    /// <param name="content">Содержимое в формате JSON</param>
    /// <returns>true если сохранение успешно, иначе false</returns>
    Task<bool> SaveDocumentConfigAsync(string configPath, string content);
}
