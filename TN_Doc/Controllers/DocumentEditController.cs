using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TN.Doc;
using TN_DocGeneral.Services;

namespace TN_Doc.Controllers
{
    /// <summary>
    /// API контроллер для редактирования документов
    /// Предоставляет endpoints для получения конфигурации формы и сохранения данных
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public class DocumentEditController : ControllerBase
    {
        private readonly IAppConfigService _appConfig;
        private readonly ILogger<DocumentEditController> _logger;

        public DocumentEditController(
            IAppConfigService appConfig,
            ILogger<DocumentEditController> logger)
        {
            _appConfig = appConfig;
            _logger = logger;
        }

        /// <summary>
        /// Получает конфигурацию формы редактирования для документа
        /// </summary>
        /// <param name="deviceId">ID устройства (GUID)</param>
        /// <param name="docType">Тип документа (IdDoc enum)</param>
        /// <param name="id">ID документа</param>
        /// <returns>JSON конфигурация с данными формы</returns>
        [HttpGet("{deviceId}/{docType}/edit/{id}")]
        public IActionResult GetEditConfig(string deviceId, string docType, int id)
        {
            try
            {
                _logger.LogInformation($"API: Запрос конфигурации редактирования документа {docType} с ID {id} для устройства {deviceId}");

                // Парсим GUID устройства
                if (!Guid.TryParse(deviceId, out var deviceGuid))
                {
                    _logger.LogWarning($"API: Некорректный GUID устройства: {deviceId}");
                    return BadRequest(new { error = "Некорректный ID устройства" });
                }

                // Парсим тип документа
                if (!Enum.TryParse<IdDoc>(docType, true, out var idDoc))
                {
                    _logger.LogWarning($"API: Некорректный тип документа: {docType}");
                    return BadRequest(new { error = "Некорректный тип документа" });
                }

                // Получаем экземпляр класса документа
                var doc = _appConfig.GetDocumentClass(deviceGuid, idDoc);
                if (doc == null)
                {
                    _logger.LogError($"API: Не удалось создать экземпляр документа {docType}");
                    return NotFound(new { error = "Документ не найден" });
                }

                // Проверяем, поддерживает ли документ получение конфигурации через API
                if (doc is not IDocumentEditor editorDoc)
                {
                    _logger.LogWarning($"API: Документ {docType} не поддерживает API редактирование");
                    return BadRequest(new { error = "Документ не поддерживает API редактирование" });
                }

                // Получаем конфигурацию формы
                var config = editorDoc.GetEditConfig(id);
                if (config == null)
                {
                    _logger.LogError($"API: Не удалось получить конфигурацию для документа {id}");
                    return NotFound(new { error = "Конфигурация документа не найдена" });
                }

                _logger.LogInformation($"API: Конфигурация успешно получена для документа {docType} (ID: {id})");
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API: Ошибка при получении конфигурации документа {docType} (ID: {id})");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера", details = ex.Message });
            }
        }

        /// <summary>
        /// Сохраняет данные документа
        /// </summary>
        /// <param name="deviceId">ID устройства (GUID)</param>
        /// <param name="docType">Тип документа (IdDoc enum)</param>
        /// <param name="id">ID документа</param>
        /// <param name="request">Данные для сохранения</param>
        /// <returns>Результат сохранения</returns>
        [HttpPost("{deviceId}/{docType}/save/{id}")]
        public IActionResult SaveDocument(string deviceId, string docType, int id, [FromBody] SaveDocumentRequest request)
        {
            try
            {
                _logger.LogInformation($"API: Сохранение документа {docType} с ID {id} для устройства {deviceId}");

                // Парсим GUID устройства
                if (!Guid.TryParse(deviceId, out var deviceGuid))
                {
                    _logger.LogWarning($"API: Некорректный GUID устройства: {deviceId}");
                    return BadRequest(new { error = "Некорректный ID устройства" });
                }

                // Парсим тип документа
                if (!Enum.TryParse<IdDoc>(docType, true, out var idDoc))
                {
                    _logger.LogWarning($"API: Некорректный тип документа: {docType}");
                    return BadRequest(new { error = "Некорректный тип документа" });
                }

                // Получаем экземпляр класса документа
                var doc = _appConfig.GetDocumentClass(deviceGuid, idDoc);
                if (doc == null)
                {
                    _logger.LogError($"API: Не удалось создать экземпляр документа {docType}");
                    return NotFound(new { error = "Документ не найден" });
                }

                // Сохраняем документ
                var jsonData = System.Text.Json.JsonSerializer.Serialize(request.Data);
                var success = doc.SaveDoc(jsonData);

                if (success)
                {
                    _logger.LogInformation($"API: Документ {docType} (ID: {id}) успешно сохранен");
                    return Ok(new { success = true, message = "Документ успешно сохранен" });
                }
                else
                {
                    _logger.LogWarning($"API: Не удалось сохранить документ {docType} (ID: {id})");
                    return BadRequest(new { success = false, error = "Не удалось сохранить документ" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"API: Ошибка при сохранении документа {docType} (ID: {id})");
                return StatusCode(500, new { error = "Внутренняя ошибка сервера", details = ex.Message });
            }
        }

        /// <summary>
        /// Проверяет доступность API
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                status = "healthy",
                service = "DocumentEditAPI",
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Запрос на сохранение документа
    /// </summary>
    public class SaveDocumentRequest
    {
        /// <summary>
        /// Данные документа в формате JSON
        /// </summary>
        public object Data { get; set; }
    }
}
