using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TN_Doc.Models.Status;

namespace TN_Doc.Services.Status
{
    /// <summary>
    /// Интерфейс сервиса проверки статуса подключений к устройствам
    /// </summary>
    public interface IConnectionStatusService
    {
        /// <summary>
        /// Событие изменения статуса устройства
        /// </summary>
        event EventHandler<DeviceStatusChangedEventArgs> DeviceStatusChanged;

        /// <summary>
        /// Проверка подключения к конкретному устройству
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Статус подключения к устройству</returns>
        Task<ConnectionStatus> CheckDeviceConnectionAsync(int deviceId);

        /// <summary>
        /// Проверка подключения к SignalR Hub
        /// </summary>
        /// <returns>Статус SignalR подключения</returns>
        Task<ServiceStatus> CheckSignalRConnectionAsync();

        /// <summary>
        /// Получение статусов всех устройств
        /// </summary>
        /// <returns>Список статусов устройств</returns>
        Task<List<DeviceStatus>> GetAllDeviceStatusesAsync();

        /// <summary>
        /// Получение статуса конкретного устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Статус устройства</returns>
        Task<DeviceStatus?> GetDeviceStatusAsync(int deviceId);

        /// <summary>
        /// Получение конфигурации для клиента
        /// </summary>
        /// <returns>Конфигурация устройств и сервисов</returns>
        Task<StatusConfiguration> GetStatusConfigurationAsync();

        /// <summary>
        /// Принудительное обновление статуса устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        Task RefreshDeviceStatusAsync(int deviceId);

        /// <summary>
        /// Получение статистики подключений
        /// </summary>
        /// <returns>Статистика по всем подключениям</returns>
        Task<Dictionary<string, ConnectionStatistics>> GetConnectionStatisticsAsync();

        /// <summary>
        /// Очистка кэша статусов
        /// </summary>
        void ClearStatusCache();
    }

    /// <summary>
    /// Аргументы события изменения статуса устройства
    /// </summary>
    public class DeviceStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Идентификатор устройства
        /// </summary>
        public int DeviceId { get; set; }

        /// <summary>
        /// Предыдущий статус
        /// </summary>
        public bool PreviousStatus { get; set; }

        /// <summary>
        /// Текущий статус
        /// </summary>
        public bool CurrentStatus { get; set; }

        /// <summary>
        /// Время изменения
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Дополнительная информация
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Конфигурация статусов для клиента
    /// </summary>
    public class StatusConfiguration
    {
        /// <summary>
        /// Конфигурация устройств
        /// </summary>
        public List<DeviceConfiguration> Devices { get; set; } = new();

        /// <summary>
        /// Конфигурация OPC серверов
        /// </summary>
        public List<OpcServerConfiguration> OpcServers { get; set; } = new();

        /// <summary>
        /// Конфигурация ELIS
        /// </summary>
        public ElisConfiguration Elis { get; set; } = new();

        /// <summary>
        /// Настройки обновления статусов
        /// </summary>
        public StatusUpdateSettings UpdateSettings { get; set; } = new();
    }

    /// <summary>
    /// Конфигурация устройства для клиента
    /// </summary>
    public class DeviceConfiguration
    {
        /// <summary>
        /// Идентификатор устройства
        /// </summary>
        public int IdDevice { get; set; }

        /// <summary>
        /// Наименование устройства
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Описание устройства
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Активность устройства
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Конфигурация OPC сервера для клиента
    /// </summary>
    public class OpcServerConfiguration
    {
        /// <summary>
        /// Идентификатор OPC сервера
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Тип OPC сервера
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Наименование сервера
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Адрес сервера (endpoint/progId)
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// ProgId для OPC DA
        /// </summary>
        public string? ProgId { get; set; }
    }

    /// <summary>
    /// Конфигурация ELIS для клиента
    /// </summary>
    public class ElisConfiguration
    {
        /// <summary>
        /// Использование ELIS
        /// </summary>
        public bool Use { get; set; }

        /// <summary>
        /// URL ELIS API
        /// </summary>
        public string? ApiUrl { get; set; }

        /// <summary>
        /// Глобальные настройки
        /// </summary>
        public bool IsGlobalSettings { get; set; }
    }

    /// <summary>
    /// Настройки обновления статусов
    /// </summary>
    public class StatusUpdateSettings
    {
        /// <summary>
        /// Интервал обновления статусов устройств (секунды)
        /// </summary>
        public int DeviceUpdateInterval { get; set; } = 60;

        /// <summary>
        /// Интервал обновления статусов OPC (секунды)
        /// </summary>
        public int OpcUpdateInterval { get; set; } = 30;

        /// <summary>
        /// Интервал обновления статусов сервисов (секунды)
        /// </summary>
        public int ServiceUpdateInterval { get; set; } = 10;

        /// <summary>
        /// Время жизни кэша (секунды)
        /// </summary>
        public int CacheTtl { get; set; } = 10;
    }
}