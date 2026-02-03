using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN_Doc.Models.CircuitBreaker;
using TN_DocGeneral.Services;

namespace TN_Doc.Services;

/// <summary>
/// Реализация Circuit Breaker для защиты БД от блокировки.
///
/// Логика работы:
/// - Auth ошибки (1045, 1044, 1698) → немедленная блокировка до ручного сброса
/// - Network ошибки → exponential backoff (60с → 1ч), после MaxRetryCount → блокировка
/// - При успешном подключении → полный сброс всех счётчиков
/// </summary>
public class CircuitBreakerService : ICircuitBreakerService
{
    private readonly IAppConfigService _appConfigService;
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly ConcurrentDictionary<string, DeviceCircuitState> _deviceStates = new();

    // MySQL коды ошибок аутентификации
    private static readonly int[] AuthErrorCodes = { 1045, 1044, 1698 };

    // MySQL коды сетевых ошибок
    private static readonly int[] NetworkErrorCodes = { 2003, 2002, 2006, 2013 };

    public CircuitBreakerService(IAppConfigService appConfigService, ILogger<CircuitBreakerService> logger)
    {
        _appConfigService = appConfigService ?? throw new ArgumentNullException(nameof(appConfigService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool HasBlockedDevices => _deviceStates.Values.Any(s => s.RequiresManualReset);

    /// <inheritdoc/>
    public bool ShouldAllowConnection(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return true;

        if (!_deviceStates.TryGetValue(deviceId, out var state))
            return true;

        // Если состояние Closed - всегда разрешаем
        if (state.State == CircuitState.Closed)
            return true;

        // Если требуется ручной сброс - запрещаем
        if (state.RequiresManualReset)
        {
            _logger.LogDebug("Device {DeviceId} blocked: requires manual reset (category: {Category})",
                deviceId, state.ErrorCategory);
            return false;
        }

        // Проверяем, истёк ли backoff
        if (state.NextAllowedAttempt.HasValue && DateTime.Now >= state.NextAllowedAttempt.Value)
        {
            // Переводим в HalfOpen для пробного подключения
            state.State = CircuitState.HalfOpen;
            _logger.LogDebug("Device {DeviceId} transitioning to HalfOpen state", deviceId);
            return true;
        }

        _logger.LogDebug("Device {DeviceId} blocked: backoff active until {NextAttempt}",
            deviceId, state.NextAllowedAttempt);
        return false;
    }

    /// <inheritdoc/>
    public void RecordSuccess(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return;

        if (_deviceStates.TryGetValue(deviceId, out var state))
        {
            var previousState = state.State;
            var previousFailures = state.FailureCount;

            // Полный сброс состояния
            state.State = CircuitState.Closed;
            state.ErrorCategory = ErrorCategory.None;
            state.LastError = null;
            state.FailureCount = 0;
            state.MaxBackoffRetryCount = 0;
            state.CurrentBackoffSeconds = 0;
            state.NextAllowedAttempt = null;
            state.LastFailureTime = null;
            state.RequiresManualReset = false;

            _logger.LogInformation(
                "Device {DeviceId} recovered: {PreviousState} → Closed (was {FailureCount} failures)",
                deviceId, previousState, previousFailures);
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(string deviceId, string deviceName, Exception exception)
    {
        if (string.IsNullOrEmpty(deviceId))
            return;

        var settings = GetSettings();
        var category = CategorizeError(exception);

        var state = _deviceStates.GetOrAdd(deviceId, _ => new DeviceCircuitState
        {
            DeviceId = deviceId,
            DeviceName = deviceName
        });

        state.DeviceName = deviceName;
        state.ErrorCategory = category;
        state.LastError = exception.Message;
        state.LastFailureTime = DateTime.Now;
        state.FailureCount++;

        switch (category)
        {
            case ErrorCategory.Authentication:
                HandleAuthenticationError(state, settings);
                break;

            case ErrorCategory.Network:
            case ErrorCategory.Other:
                HandleNetworkOrOtherError(state, settings);
                break;
        }

        _logger.LogWarning(
            "Device {DeviceId} ({DeviceName}) failure #{Count}: {Category} - {Error}. State: {State}, Backoff: {Backoff}s",
            deviceId, deviceName, state.FailureCount, category, exception.Message,
            state.State, state.CurrentBackoffSeconds);
    }

    /// <inheritdoc/>
    public bool ResetDevice(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return false;

        if (_deviceStates.TryGetValue(deviceId, out var state))
        {
            var previousState = state.State;
            var previousCategory = state.ErrorCategory;

            // Сброс для новой попытки (но сохраняем историю ошибки)
            state.State = CircuitState.HalfOpen;
            state.RequiresManualReset = false;
            state.NextAllowedAttempt = null;
            state.CurrentBackoffSeconds = 0;
            state.MaxBackoffRetryCount = 0;
            // FailureCount не сбрасываем - покажет историю

            _logger.LogInformation(
                "Device {DeviceId} manually reset: {PreviousState} ({Category}) → HalfOpen",
                deviceId, previousState, previousCategory);

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public CircuitBreakerInfo? GetCircuitBreakerInfo(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return null;

        if (!_deviceStates.TryGetValue(deviceId, out var state))
            return null;

        var info = new CircuitBreakerInfo
        {
            IsBlocked = state.RequiresManualReset,
            State = state.State.ToString(),
            ErrorCategory = state.ErrorCategory != ErrorCategory.None ? state.ErrorCategory.ToString() : null,
            LastError = state.LastError,
            FailureCount = state.FailureCount,
            MaxRetryReached = state.RequiresManualReset && state.ErrorCategory != ErrorCategory.Authentication,
            CurrentBackoffSeconds = state.CurrentBackoffSeconds
        };

        // Вычисляем время до следующей попытки
        if (!state.RequiresManualReset && state.NextAllowedAttempt.HasValue)
        {
            var secondsRemaining = (int)(state.NextAllowedAttempt.Value - DateTime.Now).TotalSeconds;
            info.SecondsUntilNextAttempt = Math.Max(0, secondsRemaining);
        }

        return info;
    }

    /// <summary>
    /// Категоризирует ошибку подключения
    /// </summary>
    private ErrorCategory CategorizeError(Exception exception)
    {
        // Проверяем MySqlException
        if (exception is MySqlException mysqlEx)
        {
            if (AuthErrorCodes.Contains(mysqlEx.Number))
                return ErrorCategory.Authentication;

            if (NetworkErrorCodes.Contains(mysqlEx.Number))
                return ErrorCategory.Network;

            return ErrorCategory.Other;
        }

        // Проверяем вложенную MySqlException
        if (exception.InnerException is MySqlException innerMysqlEx)
        {
            if (AuthErrorCodes.Contains(innerMysqlEx.Number))
                return ErrorCategory.Authentication;

            if (NetworkErrorCodes.Contains(innerMysqlEx.Number))
                return ErrorCategory.Network;
        }

        // Сетевые ошибки по типу исключения
        if (exception is SocketException ||
            exception is TimeoutException ||
            exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Unable to connect", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCategory.Network;
        }

        // Проверяем текст на auth-related сообщения
        if (exception.Message.Contains("Access denied", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase))
        {
            return ErrorCategory.Authentication;
        }

        return ErrorCategory.Other;
    }

    /// <summary>
    /// Обрабатывает ошибку аутентификации - немедленная блокировка
    /// </summary>
    private void HandleAuthenticationError(DeviceCircuitState state, TN.DocData.CircuitBreakerSettings settings)
    {
        state.State = CircuitState.Open;
        state.RequiresManualReset = true;
        state.NextAllowedAttempt = null; // Нет автоматических попыток

        _logger.LogError(
            "Device {DeviceId} ({DeviceName}) BLOCKED due to authentication error. Manual reset required.",
            state.DeviceId, state.DeviceName);
    }

    /// <summary>
    /// Обрабатывает сетевую или другую ошибку - exponential backoff
    /// </summary>
    private void HandleNetworkOrOtherError(DeviceCircuitState state, TN.DocData.CircuitBreakerSettings settings)
    {
        // Проверяем порог включения backoff
        if (state.FailureCount < settings.NetworkFailureThreshold)
        {
            // Ещё не достигли порога - остаёмся в Closed
            state.State = CircuitState.Closed;
            return;
        }

        // Включаем backoff
        state.State = CircuitState.Open;

        // Вычисляем новый backoff
        if (state.CurrentBackoffSeconds == 0)
        {
            state.CurrentBackoffSeconds = settings.InitialBackoffSeconds;
        }
        else if (state.CurrentBackoffSeconds < settings.MaxBackoffSeconds)
        {
            state.CurrentBackoffSeconds = Math.Min(
                (int)(state.CurrentBackoffSeconds * settings.BackoffMultiplier),
                settings.MaxBackoffSeconds);
        }

        // Проверяем достижение максимального backoff
        if (state.CurrentBackoffSeconds >= settings.MaxBackoffSeconds)
        {
            state.MaxBackoffRetryCount++;

            // Проверяем лимит попыток с максимальным backoff
            if (state.MaxBackoffRetryCount >= settings.MaxRetryCount)
            {
                state.RequiresManualReset = true;
                state.NextAllowedAttempt = null;

                _logger.LogError(
                    "Device {DeviceId} ({DeviceName}) BLOCKED: exceeded {MaxRetry} retries at max backoff",
                    state.DeviceId, state.DeviceName, settings.MaxRetryCount);
                return;
            }
        }

        state.NextAllowedAttempt = DateTime.Now.AddSeconds(state.CurrentBackoffSeconds);
    }

    /// <summary>
    /// Получает настройки Circuit Breaker из конфигурации
    /// </summary>
    private TN.DocData.CircuitBreakerSettings GetSettings()
    {
        var appConfig = _appConfigService.GetAppCfg();
        return appConfig?.CircuitBreaker ?? new TN.DocData.CircuitBreakerSettings();
    }
}
