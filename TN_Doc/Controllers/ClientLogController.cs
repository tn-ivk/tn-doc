using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TN_Doc.Models;
using System;

namespace TN_Doc.Controllers;

/// <summary>
/// Контроллер для приёма логов от клиентской части приложения
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ClientLogController : ControllerBase
{
    private readonly ILogger<ClientLogController> _logger;

    public ClientLogController(ILogger<ClientLogController> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Принимает сообщение лога от клиента и записывает в серверный лог
    /// </summary>
    /// <param name="log">Объект с уровнем и текстом сообщения</param>
    /// <returns>200 OK при успехе, 400 BadRequest при ошибке</returns>
    [HttpPost("logging")]
    public IActionResult LogClientMessage([FromBody] ClientLogMessage log)
    {
        if (log == null)
        {
            _logger.LogError("Ошибка лога клиентской части: объект log равен null");
            return BadRequest("Log object is null");
        }

        if (string.IsNullOrWhiteSpace(log.Level))
        {
            _logger.LogError("Ошибка лога клиентской части: не указан уровень логирования");
            return BadRequest("Log level is required");
        }

        if (string.IsNullOrWhiteSpace(log.Message))
        {
            _logger.LogError("Ошибка лога клиентской части: пустое сообщение");
            return BadRequest("Log message is required");
        }

        var level = log.Level.Trim().ToLowerInvariant();
        var msg = log.Message;

        switch (level)
        {
            case "trace":
                _logger.LogTrace(msg);
                break;
            case "debug":
                _logger.LogDebug(msg);
                break;
            case "info":
                _logger.LogInformation(msg);
                break;
            case "warn":
            case "warning":
                _logger.LogWarning(msg);
                break;
            case "error":
            case "fatal":
            case "critical":
                _logger.LogError(msg);
                break;
            default:
                _logger.LogError($"Неизвестный уровень логирования от клиента: '{log.Level}'. Сообщение: {msg}");
                return BadRequest($"Unknown log level: {log.Level}");
        }

        return Ok();
    }
}
