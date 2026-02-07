using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using TN_Doc.Models.DeviceConnectionDiagnostic;
using TN_DocGeneral.Services;

namespace TN_Doc.Services;

/// <summary>
/// Реализация сервиса диагностики соединения для защиты БД от блокировки.
///
/// Логика работы:
/// - Auth ошибки (1045, 1044, 1698) → немедленная блокировка до ручного сброса
/// - Network ошибки → exponential polling (60с → 1ч), после MaxRetryCount → блокировка
/// - При успешном подключении → полный сброс всех счётчиков
/// </summary>
public class DeviceConnectionDiagnosticService : IDeviceConnectionDiagnosticService
{
    private readonly IAppConfigService _appConfigService;
    private readonly ILogger<DeviceConnectionDiagnosticService> _logger;
    private readonly ConcurrentDictionary<string, DeviceConnectionState> _deviceStates = new();

    // MySQL коды ошибок аутентификации
    private static readonly int[] AuthErrorCodes = { 1045, 1044, 1698 };

    // MySQL коды сетевых ошибок
    private static readonly int[] NetworkErrorCodes = { 2003, 2002, 2006, 2013 };

    public DeviceConnectionDiagnosticService(IAppConfigService appConfigService, ILogger<DeviceConnectionDiagnosticService> logger)
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

        // Если состояние Active - всегда разрешаем
        if (state.State == ConnectionState.Active)
            return true;

        // Если требуется ручной сброс - запрещаем
        if (state.RequiresManualReset)
        {
            _logger.LogDebug("Device {DeviceId} blocked: requires manual reset (category: {Category})",
                deviceId, state.ErrorCategory);
            return false;
        }

        // Проверяем, истёк ли интервал опроса
        if (state.NextAllowedAttempt.HasValue && DateTime.Now >= state.NextAllowedAttempt.Value)
        {
            // Переводим в Recovering для пробного подключения
            state.State = ConnectionState.Recovering;
            _logger.LogDebug("Device {DeviceId} transitioning to Recovering state", deviceId);
            return true;
        }

        _logger.LogDebug("Device {DeviceId} blocked: poll interval active until {NextAttempt}",
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
            state.State = ConnectionState.Active;
            state.ErrorCategory = ErrorCategory.None;
            state.LastError = null;
            state.FailureCount = 0;
            state.MaxPollRetryCount = 0;
            state.CurrentPollSeconds = 0;
            state.NextAllowedAttempt = null;
            state.LastFailureTime = null;
            state.RequiresManualReset = false;

            _logger.LogInformation(
                "Device {DeviceId} recovered: {PreviousState} → Active (was {FailureCount} failures)",
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

        var state = _deviceStates.GetOrAdd(deviceId, _ => new DeviceConnectionState
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
            "Device {DeviceId} ({DeviceName}) failure #{Count}: {Category} - {Error}. State: {State}, Poll: {Poll}s",
            deviceId, deviceName, state.FailureCount, category, exception.Message,
            state.State, state.CurrentPollSeconds);
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
            state.State = ConnectionState.Recovering;
            state.RequiresManualReset = false;
            state.NextAllowedAttempt = null;
            state.CurrentPollSeconds = 0;
            state.MaxPollRetryCount = 0;
            // FailureCount не сбрасываем - покажет историю

            _logger.LogInformation(
                "Device {DeviceId} manually reset: {PreviousState} ({Category}) → Recovering",
                deviceId, previousState, previousCategory);

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public DeviceConnectionDiagnosticInfo? GetDeviceConnectionDiagnosticInfo(string deviceId)
    {
        if (string.IsNullOrEmpty(deviceId))
            return null;

        if (!_deviceStates.TryGetValue(deviceId, out var state))
            return null;

        var info = new DeviceConnectionDiagnosticInfo
        {
            IsBlocked = state.RequiresManualReset,
            State = state.State.ToString(),
            ErrorCategory = state.ErrorCategory != ErrorCategory.None ? state.ErrorCategory.ToString() : null,
            LastError = state.LastError,
            FailureCount = state.FailureCount,
            MaxRetryReached = state.RequiresManualReset && state.ErrorCategory != ErrorCategory.Authentication,
            CurrentPollSeconds = state.CurrentPollSeconds
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
    private void HandleAuthenticationError(DeviceConnectionState state, TN.DocData.DeviceConnectionDiagnosticSettings settings)
    {
        state.State = ConnectionState.Blocked;
        state.RequiresManualReset = true;
        state.NextAllowedAttempt = null; // Нет автоматических попыток

        _logger.LogError(
            "Device {DeviceId} ({DeviceName}) BLOCKED due to authentication error. Manual reset required.",
            state.DeviceId, state.DeviceName);
    }

    /// <summary>
    /// Обрабатывает сетевую или другую ошибку - exponential polling
    /// </summary>
    private void HandleNetworkOrOtherError(DeviceConnectionState state, TN.DocData.DeviceConnectionDiagnosticSettings settings)
    {
        // Проверяем порог включения защиты
        if (state.FailureCount < settings.NetworkFailureThreshold)
        {
            // Ещё не достигли порога - остаёмся в Active
            state.State = ConnectionState.Active;
            return;
        }

        // Включаем защиту
        state.State = ConnectionState.Blocked;

        // Вычисляем новый интервал опроса
        if (state.CurrentPollSeconds == 0)
        {
            state.CurrentPollSeconds = settings.InitialPollSeconds;
        }
        else if (state.CurrentPollSeconds < settings.MaxPollSeconds)
        {
            state.CurrentPollSeconds = Math.Min(
                (int)(state.CurrentPollSeconds * settings.PollMultiplier),
                settings.MaxPollSeconds);
        }

        // Проверяем достижение максимального интервала
        if (state.CurrentPollSeconds >= settings.MaxPollSeconds)
        {
            state.MaxPollRetryCount++;

            // Проверяем лимит попыток с максимальным интервалом
            if (state.MaxPollRetryCount >= settings.MaxRetryCount)
            {
                state.RequiresManualReset = true;
                state.NextAllowedAttempt = null;

                _logger.LogError(
                    "Device {DeviceId} ({DeviceName}) BLOCKED: exceeded {MaxRetry} retries at max poll interval",
                    state.DeviceId, state.DeviceName, settings.MaxRetryCount);
                return;
            }
        }

        state.NextAllowedAttempt = DateTime.Now.AddSeconds(state.CurrentPollSeconds);
    }

    /// <summary>
    /// Получает настройки диагностики соединения из конфигурации
    /// </summary>
    private TN.DocData.DeviceConnectionDiagnosticSettings GetSettings()
    {
        var appConfig = _appConfigService.GetAppCfg();
        return appConfig?.DeviceConnectionDiagnostic ?? new TN.DocData.DeviceConnectionDiagnosticSettings();
    }
}
