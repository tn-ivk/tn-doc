using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TN_Doc.Models.Status;

namespace TN_Doc.Services.Status
{
    /// <summary>
    /// Интерфейс сервиса проверки статуса ELIS (Единая Лабораторная Информационная Система)
    /// </summary>
    public interface IElisStatusService
    {
        /// <summary>
        /// Событие изменения статуса ELIS
        /// </summary>
        event EventHandler<ElisStatusChangedEventArgs> ElisStatusChanged;

        /// <summary>
        /// Проверка доступности ELIS API
        /// </summary>
        /// <returns>Статус ELIS подключения</returns>
        Task<ServiceStatus> CheckElisAvailabilityAsync();

        /// <summary>
        /// Проверка подключения к LabHub
        /// </summary>
        /// <returns>Статус LabHub подключения</returns>
        Task<ServiceStatus> CheckLabHubConnectionAsync();

        /// <summary>
        /// Проверка валидности токенов доступа к ELIS
        /// </summary>
        /// <returns>Результат проверки токенов</returns>
        Task<ElisTokenValidationResult> ValidateElisTokensAsync();

        /// <summary>
        /// Проверка статуса SSL сертификатов
        /// </summary>
        /// <returns>Статус SSL сертификатов</returns>
        Task<ElisSSLStatus> CheckElisSSLStatusAsync();

        /// <summary>
        /// Получение информации о ELIS системе
        /// </summary>
        /// <returns>Информация о ELIS</returns>
        Task<ElisSystemInfo?> GetElisSystemInfoAsync();

        /// <summary>
        /// Тестовый запрос к ELIS API
        /// </summary>
        /// <returns>Результат тестового запроса</returns>
        Task<ElisTestRequestResult> TestElisApiAsync();

        /// <summary>
        /// Получение статуса ELIS для конкретного устройства
        /// </summary>
        /// <param name="deviceId">Идентификатор устройства</param>
        /// <returns>Статус ELIS для устройства</returns>
        Task<ServiceStatus?> GetDeviceElisStatusAsync(int deviceId);

        /// <summary>
        /// Проверка доступности справочников ELIS
        /// </summary>
        /// <returns>Статус справочников</returns>
        Task<ElisDictionaryStatus> CheckElisDictionariesAsync();

        /// <summary>
        /// Получение статистики работы с ELIS
        /// </summary>
        /// <returns>Статистика ELIS</returns>
        Task<ElisStatistics> GetElisStatisticsAsync();

        /// <summary>
        /// Принудительное обновление статуса ELIS
        /// </summary>
        Task RefreshElisStatusAsync();

        /// <summary>
        /// Очистка кэша статусов ELIS
        /// </summary>
        void ClearElisStatusCache();

        /// <summary>
        /// Проверка настроек ELIS в конфигурации
        /// </summary>
        /// <returns>Результат проверки настроек</returns>
        Task<ElisConfigValidationResult> ValidateElisConfigurationAsync();
    }

    /// <summary>
    /// Аргументы события изменения статуса ELIS
    /// </summary>
    public class ElisStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Предыдущий статус доступности
        /// </summary>
        public bool PreviousAvailability { get; set; }

        /// <summary>
        /// Текущий статус доступности
        /// </summary>
        public bool CurrentAvailability { get; set; }

        /// <summary>
        /// Время изменения
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Дополнительная информация
        /// </summary>
        public string? AdditionalInfo { get; set; }

        /// <summary>
        /// Тип изменения статуса
        /// </summary>
        public ElisStatusChangeType ChangeType { get; set; }
    }

    /// <summary>
    /// Тип изменения статуса ELIS
    /// </summary>
    public enum ElisStatusChangeType
    {
        /// <summary>
        /// Общая доступность API
        /// </summary>
        ApiAvailability,

        /// <summary>
        /// Подключение к LabHub
        /// </summary>
        LabHubConnection,

        /// <summary>
        /// Валидность токенов
        /// </summary>
        TokenValidation,

        /// <summary>
        /// Статус SSL сертификатов
        /// </summary>
        SslCertificate,

        /// <summary>
        /// Доступность справочников
        /// </summary>
        DictionariesAvailability
    }

    /// <summary>
    /// Результат проверки токенов ELIS
    /// </summary>
    public class ElisTokenValidationResult
    {
        /// <summary>
        /// Все токены валидны
        /// </summary>
        public bool AllTokensValid { get; set; }

        /// <summary>
        /// Результаты проверки отдельных токенов
        /// </summary>
        public Dictionary<string, bool> TokenValidationResults { get; set; } = new();

        /// <summary>
        /// Дата истечения токенов
        /// </summary>
        public Dictionary<string, DateTime?> TokenExpiryDates { get; set; } = new();

        /// <summary>
        /// Сообщения об ошибках
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Время проверки
        /// </summary>
        public DateTime CheckTime { get; set; }
    }

    /// <summary>
    /// Статус SSL сертификатов ELIS
    /// </summary>
    public class ElisSSLStatus
    {
        /// <summary>
        /// Все сертификаты валидны
        /// </summary>
        public bool AllCertificatesValid { get; set; }

        /// <summary>
        /// Статусы отдельных сертификатов
        /// </summary>
        public List<CertificateInfo> Certificates { get; set; } = new();

        /// <summary>
        /// Сообщения об ошибках
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Время проверки
        /// </summary>
        public DateTime CheckTime { get; set; }
    }

    /// <summary>
    /// Информация о сертификате
    /// </summary>
    public class CertificateInfo
    {
        /// <summary>
        /// Имя файла сертификата
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Путь к сертификату
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Сертификат валиден
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Субъект сертификата
        /// </summary>
        public string? Subject { get; set; }

        /// <summary>
        /// Издатель сертификата
        /// </summary>
        public string? Issuer { get; set; }

        /// <summary>
        /// Дата истечения
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Ошибки валидации
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Информация о системе ELIS
    /// </summary>
    public class ElisSystemInfo
    {
        /// <summary>
        /// Версия ELIS API
        /// </summary>
        public string? ApiVersion { get; set; }

        /// <summary>
        /// URL базового API
        /// </summary>
        public string? ApiBaseUrl { get; set; }

        /// <summary>
        /// URL LabHub
        /// </summary>
        public string? LabHubUrl { get; set; }

        /// <summary>
        /// Статус системы
        /// </summary>
        public string? SystemStatus { get; set; }

        /// <summary>
        /// Время последнего обновления
        /// </summary>
        public DateTime? LastUpdateTime { get; set; }

        /// <summary>
        /// Доступные модули
        /// </summary>
        public List<string>? AvailableModules { get; set; }

        /// <summary>
        /// Конфигурационные параметры
        /// </summary>
        public Dictionary<string, object>? Configuration { get; set; }
    }

    /// <summary>
    /// Результат тестового запроса к ELIS API
    /// </summary>
    public class ElisTestRequestResult
    {
        /// <summary>
        /// Запрос успешен
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP статус код
        /// </summary>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Время отклика в миллисекундах
        /// </summary>
        public long ResponseTime { get; set; }

        /// <summary>
        /// Тело ответа
        /// </summary>
        public string? ResponseBody { get; set; }

        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Время выполнения запроса
        /// </summary>
        public DateTime RequestTime { get; set; }

        /// <summary>
        /// Заголовки ответа
        /// </summary>
        public Dictionary<string, string>? ResponseHeaders { get; set; }
    }

    /// <summary>
    /// Статус справочников ELIS
    /// </summary>
    public class ElisDictionaryStatus
    {
        /// <summary>
        /// Все справочники доступны
        /// </summary>
        public bool AllDictionariesAvailable { get; set; }

        /// <summary>
        /// Статусы отдельных справочников
        /// </summary>
        public Dictionary<string, bool> DictionaryStatuses { get; set; } = new();

        /// <summary>
        /// Время последнего обновления справочников
        /// </summary>
        public Dictionary<string, DateTime?> LastUpdateTimes { get; set; } = new();

        /// <summary>
        /// Сообщения об ошибках
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Время проверки
        /// </summary>
        public DateTime CheckTime { get; set; }
    }

    /// <summary>
    /// Статистика работы с ELIS
    /// </summary>
    public class ElisStatistics
    {
        /// <summary>
        /// Общее количество запросов к ELIS
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Количество успешных запросов
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// Количество неудачных запросов
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// Среднее время отклика
        /// </summary>
        public double AverageResponseTime { get; set; }

        /// <summary>
        /// Процент успешности
        /// </summary>
        public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;

        /// <summary>
        /// Время первого запроса
        /// </summary>
        public DateTime? FirstRequestTime { get; set; }

        /// <summary>
        /// Время последнего успешного запроса
        /// </summary>
        public DateTime? LastSuccessfulRequestTime { get; set; }

        /// <summary>
        /// Статистика по типам запросов
        /// </summary>
        public Dictionary<string, int>? RequestTypeStatistics { get; set; }
    }

    /// <summary>
    /// Результат проверки конфигурации ELIS
    /// </summary>
    public class ElisConfigValidationResult
    {
        /// <summary>
        /// Конфигурация валидна
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// ELIS включен в конфигурации
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Используются глобальные настройки
        /// </summary>
        public bool IsGlobalConfiguration { get; set; }

        /// <summary>
        /// Сообщения об ошибках конфигурации
        /// </summary>
        public List<string> ValidationErrors { get; set; } = new();

        /// <summary>
        /// Предупреждения о конфигурации
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Найденные настройки ELIS для устройств
        /// </summary>
        public List<int> DevicesWithElisSettings { get; set; } = new();

        /// <summary>
        /// Время проверки конфигурации
        /// </summary>
        public DateTime CheckTime { get; set; }
    }
}