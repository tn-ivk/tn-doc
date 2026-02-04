using System;
using TN_Doc.Models.DeviceConnectionDiagnostic;

namespace TN_Doc.Services;

/// <summary>
/// Сервис диагностики соединения для защиты БД от блокировки при неудачных попытках подключения.
/// При неправильном пароле MySQL блокирует хост после 100 неудачных попыток (max_connect_errors).
/// </summary>
public interface IDeviceConnectionDiagnosticService
{
    /// <summary>
    /// Проверяет, разрешено ли сейчас подключение к устройству
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    /// <returns>true если подключение разрешено, false если заблокировано</returns>
    bool ShouldAllowConnection(string deviceId);

    /// <summary>
    /// Регистрирует успешное подключение (полный сброс состояния)
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    void RecordSuccess(string deviceId);

    /// <summary>
    /// Регистрирует неудачное подключение с категоризацией ошибки
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    /// <param name="deviceName">Название устройства</param>
    /// <param name="exception">Исключение с информацией об ошибке</param>
    void RecordFailure(string deviceId, string deviceName, Exception exception);

    /// <summary>
    /// Сбрасывает состояние диагностики для устройства (ручной сброс)
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    /// <returns>true если сброс выполнен успешно</returns>
    bool ResetDevice(string deviceId);

    /// <summary>
    /// Получает информацию о состоянии диагностики соединения для устройства
    /// </summary>
    /// <param name="deviceId">ID устройства</param>
    /// <returns>Информация о диагностике или null если устройство не отслеживается</returns>
    DeviceConnectionDiagnosticInfo? GetDeviceConnectionDiagnosticInfo(string deviceId);

    /// <summary>
    /// Проверяет, есть ли заблокированные устройства
    /// </summary>
    bool HasBlockedDevices { get; }
}
