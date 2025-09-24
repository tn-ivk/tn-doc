using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TN_Doc.Models.Status;

namespace TN_Doc.Services.Status
{
    /// <summary>
    /// Интерфейс сервиса проверки статуса OPC серверов
    /// </summary>
    public interface IOpcStatusService
    {
        /// <summary>
        /// Событие изменения статуса OPC сервера
        /// </summary>
        event EventHandler<OpcStatusChangedEventArgs> OpcStatusChanged;

        /// <summary>
        /// Проверка подключения к OPC серверу
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <returns>Статус подключения к OPC серверу</returns>
        Task<OpcServerStatus> CheckOpcServerStatusAsync(string opcId);

        /// <summary>
        /// Проверка подключения к OPC DA серверу
        /// </summary>
        /// <param name="progId">ProgID OPC DA сервера</param>
        /// <param name="serverName">Наименование сервера</param>
        /// <returns>Статус подключения к OPC DA серверу</returns>
        Task<OpcServerStatus> CheckOpcDaServerAsync(string progId, string serverName);

        /// <summary>
        /// Проверка подключения к OPC UA серверу
        /// </summary>
        /// <param name="endpoint">Endpoint OPC UA сервера</param>
        /// <param name="serverName">Наименование сервера</param>
        /// <returns>Статус подключения к OPC UA серверу</returns>
        Task<OpcServerStatus> CheckOpcUaServerAsync(string endpoint, string serverName);

        /// <summary>
        /// Получение статусов всех OPC серверов
        /// </summary>
        /// <returns>Список статусов OPC серверов</returns>
        Task<List<OpcServerStatus>> GetAllOpcServerStatusesAsync();

        /// <summary>
        /// Получение статуса конкретного OPC сервера
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <returns>Статус OPC сервера</returns>
        Task<OpcServerStatus?> GetOpcServerStatusAsync(string opcId);

        /// <summary>
        /// Тестовое чтение тега с OPC сервера
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <param name="tagName">Наименование тега</param>
        /// <returns>Результат чтения тега</returns>
        Task<OpcTagReadResult> TestTagReadAsync(string opcId, string tagName);

        /// <summary>
        /// Получение списка доступных OPC DA серверов на машине
        /// </summary>
        /// <param name="machineName">Имя машины (null для локальной)</param>
        /// <returns>Список ProgID доступных серверов</returns>
        Task<List<string>> GetAvailableOpcDaServersAsync(string? machineName = null);

        /// <summary>
        /// Discovery OPC UA серверов
        /// </summary>
        /// <param name="discoveryUrl">URL для discovery</param>
        /// <returns>Список найденных endpoints</returns>
        Task<List<string>> DiscoverOpcUaServersAsync(string discoveryUrl);

        /// <summary>
        /// Проверка валидности сертификата OPC UA
        /// </summary>
        /// <param name="endpoint">Endpoint OPC UA сервера</param>
        /// <returns>Статус сертификата</returns>
        Task<CertificateStatus> CheckOpcUaCertificateAsync(string endpoint);

        /// <summary>
        /// Получение информации о OPC сервере
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        /// <returns>Информация о сервере</returns>
        Task<OpcServerInfo?> GetOpcServerInfoAsync(string opcId);

        /// <summary>
        /// Принудительное обновление статуса OPC сервера
        /// </summary>
        /// <param name="opcId">Идентификатор OPC сервера</param>
        Task RefreshOpcServerStatusAsync(string opcId);

        /// <summary>
        /// Очистка кэша статусов OPC серверов
        /// </summary>
        void ClearOpcStatusCache();
    }

    /// <summary>
    /// Аргументы события изменения статуса OPC сервера
    /// </summary>
    public class OpcStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Идентификатор OPC сервера
        /// </summary>
        public string OpcId { get; set; } = string.Empty;

        /// <summary>
        /// Тип OPC сервера
        /// </summary>
        public OpcServerType ServerType { get; set; }

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

        /// <summary>
        /// Код ошибки OPC (если есть)
        /// </summary>
        public int? OpcErrorCode { get; set; }
    }

    /// <summary>
    /// Результат чтения тега с OPC сервера
    /// </summary>
    public class OpcTagReadResult
    {
        /// <summary>
        /// Успешность чтения
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Значение тега
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// Качество данных
        /// </summary>
        public string? Quality { get; set; }

        /// <summary>
        /// Временная метка
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Код ошибки OPC
        /// </summary>
        public int? OpcErrorCode { get; set; }

        /// <summary>
        /// Время выполнения операции (мс)
        /// </summary>
        public long ExecutionTime { get; set; }
    }

    /// <summary>
    /// Информация о OPC сервере
    /// </summary>
    public class OpcServerInfo
    {
        /// <summary>
        /// Идентификатор сервера
        /// </summary>
        public string ServerId { get; set; } = string.Empty;

        /// <summary>
        /// Наименование сервера
        /// </summary>
        public string ServerName { get; set; } = string.Empty;

        /// <summary>
        /// Версия сервера
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Vendor информация
        /// </summary>
        public string? VendorInfo { get; set; }

        /// <summary>
        /// Тип сервера
        /// </summary>
        public OpcServerType ServerType { get; set; }

        /// <summary>
        /// Адрес сервера
        /// </summary>
        public string ServerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Поддерживаемые группы/подписки
        /// </summary>
        public List<string>? SupportedGroups { get; set; }

        /// <summary>
        /// Доступные теги (ограниченный список)
        /// </summary>
        public List<string>? AvailableTags { get; set; }

        /// <summary>
        /// Статус соединения
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Время последнего обновления информации
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Дополнительные свойства сервера
        /// </summary>
        public Dictionary<string, object>? Properties { get; set; }
    }
}