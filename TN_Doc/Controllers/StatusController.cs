using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TN_Doc.Models.Status;
using TN_Doc.Services.Status;

namespace TN_Doc.Controllers
{
    /// <summary>
    /// API контроллер для работы со статусами подключений
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly IConnectionStatusService _connectionStatusService;
        private readonly IOpcStatusService? _opcStatusService;
        private readonly IElisStatusService? _elisStatusService;
        private readonly ILogger<StatusController> _logger;

        public StatusController(
            IConnectionStatusService connectionStatusService,
            ILogger<StatusController> logger,
            IOpcStatusService? opcStatusService = null,
            IElisStatusService? elisStatusService = null)
        {
            _connectionStatusService = connectionStatusService ?? throw new ArgumentNullException(nameof(connectionStatusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _opcStatusService = opcStatusService;
            _elisStatusService = elisStatusService;
        }

        /// <summary>
        /// Получение конфигурации статусов для клиента
        /// </summary>
        /// <returns>Конфигурация устройств и сервисов</returns>
        [HttpGet("config")]
        public async Task<ActionResult<StatusConfiguration>> GetStatusConfiguration()
        {
            try
            {
                var config = await _connectionStatusService.GetStatusConfigurationAsync();
                return Ok(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения конфигурации статусов");
                return StatusCode(500, new { error = "Ошибка получения конфигурации", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статусов всех устройств
        /// </summary>
        /// <returns>Список статусов устройств</returns>
        [HttpGet("devices")]
        public async Task<ActionResult<List<DeviceStatus>>> GetDeviceStatuses()
        {
            try
            {
                var statuses = await _connectionStatusService.GetAllDeviceStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статусов устройств");
                return StatusCode(500, new { error = "Ошибка получения статусов устройств", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статуса конкретного устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Статус устройства</returns>
        [HttpGet("device/{deviceId:int}")]
        public async Task<ActionResult<DeviceStatus>> GetDeviceStatus(int deviceId)
        {
            try
            {
                var status = await _connectionStatusService.GetDeviceStatusAsync(deviceId);

                if (status == null)
                {
                    return NotFound(new { error = "Устройство не найдено", deviceId });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статуса устройства {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Ошибка получения статуса устройства", details = ex.Message });
            }
        }

        /// <summary>
        /// Принудительное обновление статуса устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Обновленный статус устройства</returns>
        [HttpPost("device/{deviceId:int}/refresh")]
        public async Task<ActionResult<DeviceStatus>> RefreshDeviceStatus(int deviceId)
        {
            try
            {
                await _connectionStatusService.RefreshDeviceStatusAsync(deviceId);
                var status = await _connectionStatusService.GetDeviceStatusAsync(deviceId);

                if (status == null)
                {
                    return NotFound(new { error = "Устройство не найдено", deviceId });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса устройства {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Ошибка обновления статуса устройства", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статусов OPC серверов
        /// </summary>
        /// <returns>Список статусов OPC серверов</returns>
        [HttpGet("opc")]
        public async Task<ActionResult<List<OpcServerStatus>>> GetOpcServerStatuses()
        {
            try
            {
                if (_opcStatusService == null)
                {
                    return Ok(new List<OpcServerStatus>());
                }

                var statuses = await _opcStatusService.GetAllOpcServerStatusesAsync();
                return Ok(statuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статусов OPC серверов");
                return StatusCode(500, new { error = "Ошибка получения статусов OPC серверов", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статуса конкретного OPC сервера
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <returns>Статус OPC сервера</returns>
        [HttpGet("opc/{opcId}")]
        public async Task<ActionResult<OpcServerStatus>> GetOpcServerStatus(string opcId)
        {
            try
            {
                if (_opcStatusService == null)
                {
                    return NotFound(new { error = "OPC сервис не доступен" });
                }

                var status = await _opcStatusService.GetOpcServerStatusAsync(opcId);

                if (status == null)
                {
                    return NotFound(new { error = "OPC сервер не найден", opcId });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статуса OPC сервера {OpcId}", opcId);
                return StatusCode(500, new { error = "Ошибка получения статуса OPC сервера", details = ex.Message });
            }
        }

        /// <summary>
        /// Принудительное обновление статуса OPC сервера
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <returns>Обновленный статус OPC сервера</returns>
        [HttpPost("opc/{opcId}/refresh")]
        public async Task<ActionResult<OpcServerStatus>> RefreshOpcServerStatus(string opcId)
        {
            try
            {
                if (_opcStatusService == null)
                {
                    return NotFound(new { error = "OPC сервис не доступен" });
                }

                await _opcStatusService.RefreshOpcServerStatusAsync(opcId);
                var status = await _opcStatusService.GetOpcServerStatusAsync(opcId);

                if (status == null)
                {
                    return NotFound(new { error = "OPC сервер не найден", opcId });
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления статуса OPC сервера {OpcId}", opcId);
                return StatusCode(500, new { error = "Ошибка обновления статуса OPC сервера", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статусов внешних сервисов (SignalR, ELIS)
        /// </summary>
        /// <returns>Статусы сервисов</returns>
        [HttpGet("services")]
        public async Task<ActionResult<object>> GetServiceStatuses()
        {
            try
            {
                var result = new Dictionary<string, object>();

                // Проверяем SignalR
                var signalRStatus = await _connectionStatusService.CheckSignalRConnectionAsync();
                result["signalr"] = signalRStatus;

                // Проверяем ELIS если сервис доступен
                if (_elisStatusService != null)
                {
                    var elisStatus = await _elisStatusService.CheckElisAvailabilityAsync();
                    result["elis"] = elisStatus;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статусов сервисов");
                return StatusCode(500, new { error = "Ошибка получения статусов сервисов", details = ex.Message });
            }
        }

        /// <summary>
        /// Получение статистики подключений
        /// </summary>
        /// <returns>Статистика по всем подключениям</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<Dictionary<string, ConnectionStatistics>>> GetConnectionStatistics()
        {
            try
            {
                var statistics = await _connectionStatusService.GetConnectionStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения статистики подключений");
                return StatusCode(500, new { error = "Ошибка получения статистики", details = ex.Message });
            }
        }

        /// <summary>
        /// Принудительное обновление всех статусов
        /// </summary>
        /// <returns>Результат обновления</returns>
        [HttpPost("refresh-all")]
        public async Task<ActionResult<object>> RefreshAllStatuses()
        {
            try
            {
                // Очищаем кэш статусов
                _connectionStatusService.ClearStatusCache();

                if (_opcStatusService != null)
                {
                    _opcStatusService.ClearOpcStatusCache();
                }

                if (_elisStatusService != null)
                {
                    _elisStatusService.ClearElisStatusCache();
                }

                var result = new
                {
                    message = "Все статусы обновлены",
                    timestamp = DateTime.Now
                };

                _logger.LogInformation("Выполнено принудительное обновление всех статусов");

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка принудительного обновления статусов");
                return StatusCode(500, new { error = "Ошибка обновления статусов", details = ex.Message });
            }
        }

        /// <summary>
        /// Проверка работоспособности API статусов
        /// </summary>
        /// <returns>Информация о здоровье API</returns>
        [HttpGet("health")]
        public ActionResult<object> GetHealthStatus()
        {
            try
            {
                var health = new
                {
                    status = "healthy",
                    timestamp = DateTime.Now,
                    services = new
                    {
                        connectionStatusService = _connectionStatusService != null,
                        opcStatusService = _opcStatusService != null,
                        elisStatusService = _elisStatusService != null
                    },
                    version = "1.0.0"
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка проверки здоровья API статусов");
                return StatusCode(500, new { status = "unhealthy", error = ex.Message });
            }
        }

        /// <summary>
        /// Тестирование подключения к конкретному устройству
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Результат тестирования</returns>
        [HttpPost("test/device/{deviceId:int}")]
        public async Task<ActionResult<object>> TestDeviceConnection(int deviceId)
        {
            try
            {
                var connectionStatus = await _connectionStatusService.CheckDeviceConnectionAsync(deviceId);

                var result = new
                {
                    deviceId,
                    isConnected = connectionStatus.IsConnected,
                    responseTime = connectionStatus.ResponseTime,
                    connectionInfo = connectionStatus.ConnectionInfo,
                    errorMessage = connectionStatus.ErrorMessage,
                    testTime = connectionStatus.LastCheck
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка тестирования подключения к устройству {DeviceId}", deviceId);
                return StatusCode(500, new { error = "Ошибка тестирования подключения", details = ex.Message });
            }
        }

        /// <summary>
        /// Тестирование OPC тега
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <param name="tagName">Имя тега</param>
        /// <returns>Результат чтения тега</returns>
        [HttpPost("test/opc/{opcId}/tag")]
        public async Task<ActionResult<OpcTagReadResult>> TestOpcTag(string opcId, [FromBody] string tagName)
        {
            try
            {
                if (_opcStatusService == null)
                {
                    return NotFound(new { error = "OPC сервис не доступен" });
                }

                if (string.IsNullOrWhiteSpace(tagName))
                {
                    return BadRequest(new { error = "Имя тега не указано" });
                }

                var result = await _opcStatusService.TestTagReadAsync(opcId, tagName);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка тестирования OPC тега {TagName} на сервере {OpcId}", tagName, opcId);
                return StatusCode(500, new { error = "Ошибка тестирования OPC тега", details = ex.Message });
            }
        }
    }
}