using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TN_DocGeneral.Interfaces;
using TN_DocGeneral.Services;
using TN.Doc;
using TN.DocData;

namespace TN_Doc.Controllers;

/// <summary>
/// API контроллер для редактирования документов через Vue SPA
/// </summary>
[ApiController]
[Route("api/documents")]
public class DocumentEditController : ControllerBase
{
    private readonly IAppConfigService _appConfig;
    private readonly IDocModuleLoader _docModuleLoader;
    private readonly DbContextOptions<DocGeneral> _options;
    private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public DocumentEditController(
        IAppConfigService appConfig,
        IDocModuleLoader docModuleLoader,
        DbContextOptions<DocGeneral> options)
    {
        _appConfig = appConfig;
        _docModuleLoader = docModuleLoader;
        _options = options;
    }

    /// <summary>
    /// Health check endpoint для проверки доступности API
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            status = "healthy",
            service = "DocumentEditAPI",
            timestamp = DateTime.UtcNow.ToString("O")
        });
    }

    /// <summary>
    /// Получить конфигурацию формы редактирования документа
    /// </summary>
    /// <param name="deviceId">ID устройства (целое число)</param>
    /// <param name="docType">Тип документа (Report, Act, Passport и т.д.)</param>
    /// <param name="id">ID документа</param>
    [HttpGet("{deviceId}/{docType}/edit/{id}")]
    public IActionResult GetEditConfig(int deviceId, string docType, int id)
    {
        _logger.Trace($"API запрос конфигурации редактирования: deviceId={deviceId}, docType={docType}, id={id}");

        try
        {
            // Получаем IdDoc из строкового типа документа
            if (!Enum.TryParse<IdDoc>(docType, ignoreCase: true, out var idDoc))
            {
                _logger.Warn($"Неизвестный тип документа: {docType}");
                return BadRequest(new { error = $"Unknown document type: {docType}" });
            }

            // Загружаем модуль документа
            var doc = _docModuleLoader.LoadDocsModule(_options, deviceId, idDoc, AppContext.BaseDirectory);
            if (doc == null)
            {
                _logger.Error($"Не удалось загрузить DLL для документа {idDoc}");
                return StatusCode(500, new { error = "Failed to load document module" });
            }

            // Проверяем, что документ реализует IDocumentEditor
            if (doc is not IDocumentEditor editor)
            {
                _logger.Warn($"Документ типа {docType} не поддерживает редактирование через API");
                return BadRequest(new { error = $"Document type '{docType}' does not support Vue editor" });
            }

            // Получаем конфигурацию
            var config = editor.GetEditConfig(id);

            _logger.Info($"Конфигурация редактирования успешно получена: {docType} (id={id}, deviceId={deviceId})");
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при получении конфигурации редактирования: deviceId={deviceId}, docType={docType}, id={id}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Сохранить изменения документа
    /// </summary>
    /// <param name="deviceId">ID устройства (целое число)</param>
    /// <param name="docType">Тип документа</param>
    /// <param name="id">ID документа</param>
    /// <param name="data">JSON данные документа</param>
    [HttpPost("{deviceId}/{docType}/save/{id}")]
    public IActionResult SaveDocument(int deviceId, string docType, int id, [FromBody] JsonElement data)
    {
        _logger.Trace($"API запрос сохранения документа: deviceId={deviceId}, docType={docType}, id={id}");

        try
        {
            // Получаем IdDoc
            if (!Enum.TryParse<IdDoc>(docType, ignoreCase: true, out var idDoc))
            {
                _logger.Warn($"Неизвестный тип документа: {docType}");
                return BadRequest(new { error = $"Unknown document type: {docType}" });
            }

            // Загружаем модуль документа
            var doc = _docModuleLoader.LoadDocsModule(_options, deviceId, idDoc, AppContext.BaseDirectory);
            if (doc == null)
            {
                _logger.Error($"Не удалось загрузить DLL для документа {idDoc}");
                return StatusCode(500, new { error = "Failed to load document module" });
            }

            // Проверяем, что документ реализует IDocumentEditor
            if (doc is not IDocumentEditor editor)
            {
                _logger.Error($"Документ типа {docType} не реализует IDocumentEditor");
                return StatusCode(500, new { error = "Document type does not support the editor interface" });
            }

            _logger.Trace($"Использование IDocumentEditor.SaveDocument для {docType}");

            // Десериализуем JSON в словарь
            var values = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data.GetRawText());
            if (values == null)
            {
                _logger.Error("Не удалось десериализовать данные документа");
                return BadRequest(new { error = "Invalid document data" });
            }

            bool success = editor.SaveDocument(id, values);

            if (success)
            {
                _logger.Debug($"Документ успешно сохранён: {docType} (id={id}, deviceId={deviceId})");
                return Ok(new { success = true, message = "Document saved successfully" });
            }
            else
            {
                _logger.Warn($"Не удалось сохранить документ: {docType} (id={id}, deviceId={deviceId})");
                return StatusCode(500, new { error = "Failed to save document" });
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при сохранении документа: deviceId={deviceId}, docType={docType}, id={id}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }

    /// <summary>
    /// Обновить документ после успешной записи в OPC тег
    /// (используется для паспортов после подтверждения от ИВК)
    /// </summary>
    /// <param name="deviceId">ID устройства (целое число)</param>
    /// <param name="docType">Тип документа</param>
    /// <param name="id">ID документа</param>
    /// <param name="data">JSON данные документа (с объединенными данными из localStorage)</param>
    [HttpPost("{deviceId}/{docType}/update/{id}")]
    public IActionResult UpdateDocument(int deviceId, string docType, int id, [FromBody] JsonElement data)
    {
        _logger.Trace($"API запрос обновления документа: deviceId={deviceId}, docType={docType}, id={id}");

        try
        {
            // Получаем IdDoc
            if (!Enum.TryParse<IdDoc>(docType, ignoreCase: true, out var idDoc))
            {
                _logger.Warn($"Неизвестный тип документа: {docType}");
                return BadRequest(new { error = $"Unknown document type: {docType}" });
            }

            // Проверяем, что обновление применяется только для паспортов
            if (idDoc != IdDoc.Passport)
            {
                _logger.Warn($"Обновление данных не применяется для документов типа {idDoc}");
                return BadRequest(new { error = $"Update operation is only supported for Passport documents" });
            }

            // Загружаем модуль документа
            var doc = _docModuleLoader.LoadDocsModule(_options, deviceId, idDoc, AppContext.BaseDirectory);
            if (doc == null)
            {
                _logger.Error($"Не удалось загрузить DLL для документа {idDoc}");
                return StatusCode(500, new { error = "Failed to load document module" });
            }

            // Проверяем, реализует ли документ IDocUpdater
            if (doc is not IDocUpdater docUpdater)
            {
                _logger.Error($"Документ типа {docType} не поддерживает обновление через IDocUpdater");
                return StatusCode(500, new { error = "Document type does not support update operation" });
            }

            // Десериализуем JSON в словарь
            var values = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data.GetRawText());
            if (values == null)
            {
                _logger.Error("Не удалось десериализовать данные документа");
                return BadRequest(new { error = "Invalid document data" });
            }

            _logger.Trace($"Обновление паспорта через DocUpdate (после подтверждения от ИВК), количество полей: {values.Count}");

            // ДИАГНОСТИКА: Логируем ключи value.* для проверки наличия SulfurCorrection
            _logger.Debug($"[UpdateDocument ДИАГНОСТИКА] Ключи value.* в values: {string.Join(", ", values.Keys.Where(k => k.StartsWith("value.")))}");
            _logger.Debug($"[UpdateDocument ДИАГНОСТИКА] values содержит 'value.SulfurCorrection' = {values.ContainsKey("value.SulfurCorrection")}");
            if (values.ContainsKey("value.SulfurCorrection"))
            {
                _logger.Debug($"[UpdateDocument ДИАГНОСТИКА] value.SulfurCorrection = '{values["value.SulfurCorrection"]}'");
            }

            // Логируем ключи method.* для диагностики
            foreach (var kvp in values.Where(k => k.Key.StartsWith("method.")))
            {
                var valueStr = kvp.Value?.ToString() ?? "null";
                var valueType = kvp.Value?.GetType().Name ?? "null";
                _logger.Debug($"UpdateDocument: Key={kvp.Key}, Type={valueType}, Value={valueStr.Substring(0, Math.Min(200, valueStr.Length))}");
            }

            // Используем DocUpdate с плоским объектом - метод сам преобразует в CorrectionData
            docUpdater.DocUpdate(id, values);

            _logger.Info($"Документ успешно обновлен: {docType} (id={id}, deviceId={deviceId})");
            return Ok(new { success = true, message = "Document updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при обновлении документа: deviceId={deviceId}, docType={docType}, id={id}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
