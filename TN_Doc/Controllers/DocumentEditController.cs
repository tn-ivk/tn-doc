using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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

            // Сохраняем документ
            bool success;

            // Проверяем, реализует ли документ IDocumentEditor (новый формат)
            if (doc is IDocumentEditor editor)
            {
                _logger.Trace($"Использование IDocumentEditor.SaveDocument для {docType}");

                // Десериализуем JSON в словарь
                var values = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data.GetRawText());
                if (values == null)
                {
                    _logger.Error("Не удалось десериализовать данные документа");
                    return BadRequest(new { error = "Invalid document data" });
                }

                success = editor.SaveDocument(id, values);
            }
            else
            {
                _logger.Trace($"Использование старого SaveDoc для {docType}");
                // Используем старый метод для обратной совместимости
                var jsonString = data.GetRawText();
                success = doc.SaveDoc(jsonString);
            }

            if (success)
            {
                _logger.Info($"Документ успешно сохранён: {docType} (id={id}, deviceId={deviceId})");
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

            // Проверяем, реализует ли документ IDocumentEditor
            if (doc is not IDocumentEditor editor)
            {
                _logger.Error($"Документ типа {docType} не поддерживает редактирование через API");
                return BadRequest(new { error = $"Document type '{docType}' does not support Vue editor" });
            }

            // Десериализуем JSON в словарь
            var values = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data.GetRawText());
            if (values == null)
            {
                _logger.Error("Не удалось десериализовать данные документа");
                return BadRequest(new { error = "Invalid document data" });
            }

            _logger.Trace($"Обновление паспорта через SaveDocument (после подтверждения от ИВК), количество полей: {values.Count}");

            // Используем SaveDocument для обновления - он правильно обрабатывает плоский формат данных
            var success = editor.SaveDocument(id, values);

            if (success)
            {
                _logger.Info($"Документ успешно обновлен: {docType} (id={id}, deviceId={deviceId})");
                return Ok(new { success = true, message = "Document updated successfully" });
            }
            else
            {
                _logger.Warn($"Не удалось обновить документ: {docType} (id={id}, deviceId={deviceId})");
                return StatusCode(500, new { error = "Failed to update document" });
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"Ошибка при обновлении документа: deviceId={deviceId}, docType={docType}, id={id}");
            return StatusCode(500, new { error = "Internal server error", message = ex.Message });
        }
    }
}
